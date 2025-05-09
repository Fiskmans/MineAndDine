using Godot;
using Godot.Collections;
using MineAndDine;
using MineAndDine.Code.Materials;
using MineAndDine.Materials;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Array = Godot.Collections.Array;

public partial class Chunk : Node3D
{
    public const float Size = 4;
    public const int Resolution = 4;
    public const byte SelfCompactingLimit = 15;
    private const Image.Format myColorFormat = Image.Format.Rgb8;

    private Vector3I _ChunkIndex;
    public Vector3I ChunkIndex
    {
        get
        {
            return _ChunkIndex;
        }
        set
        {
            _ChunkIndex = value;
            Position = PosFromIndex(value);
        }
    }

    class MeshInfo
    {
        public Vector3[] vertices;
        public Array arrays;
        public Array<Image> colors;
    }

    private bool myIsDirty = true;
    private MeshInfo myNewMesh = null;


    static MarchingCubes myMarchingCubes = new MarchingCubes(Vector3I.One * (Resolution + 1));
    ImageTexture3D myTexture = new ImageTexture3D();
    ArrayMesh myMesh = new ArrayMesh();
    ConcavePolygonShape3D myCollisionMesh;
    ShaderMaterial myMaterial;

    public struct NodeIndex
    {
        public Chunk chunk;
        public Vector3I index;

        public byte this[MaterialType aType]
        {
            get { return Get()[aType]; }
            set { Get()[aType] = value; }
        }

        public ref MaterialsList Get()
        {
            return ref chunk.myTerrainNodes[index.X, index.Y, index.Z];
        }

        public NodeIndex Offset(Vector3I aOffset)
        {
            return chunk.NodeAt(index + aOffset);
        }

        public bool InBounds()
        {
            return chunk != null;
        }
    }

    MaterialsList[,,] myTerrainNodes;

    public override string ToString()
    {
        return $"Chunk {ChunkIndex}";
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        myTerrainNodes = new MaterialsList[Resolution, Resolution, Resolution];

        myTexture.CreatePlaceholder();

        MeshInstance3D meshComp = GetNode<MeshInstance3D>("Mesh");

        myMaterial = meshComp.Mesh.SurfaceGetMaterial(0).Duplicate() as ShaderMaterial;

        myMaterial.SetShaderParameter("Size", Size);
        myMaterial.SetShaderParameter("Offset", ChunkIndex);
        myMaterial.SetShaderParameter("Color", myTexture);

        meshComp.Mesh = myMesh;

        myCollisionMesh = new ConcavePolygonShape3D();
        GetNode<CollisionShape3D>("Collision/CollisionMesh").Shape = myCollisionMesh;

        Generate();
    }

    private void Generate()
    {
        // TODO: Move this to a worker thread
        for (int x = 0; x < Resolution; x++)
        {
            for (int y = 0; y < Resolution; y++)
            {
                for (int z = 0; z < Resolution; z++)
                {
                    myTerrainNodes[x, y, z].ForeachSet(MaterialGroups.Generatable, (type, value, generator) =>
                    {
                        return (byte)Mathf.Clamp(generator.Generate(WorldPosFromNodePos(new Vector3I(x, y, z))), 0, 255);
                    });
                }
            }
        }

        Terrain.ourInstance.RegisterModification(this);
    }


    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (myNewMesh != null)
        {
            MeshInfo info = Interlocked.Exchange(ref myNewMesh, null);

            myMesh.ClearSurfaces();
            long flags = 0;

            flags |= (long)Mesh.ArrayCustomFormat.RgbFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift;


            if (info.vertices.Length > 0)
            {
                myMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, info.arrays, null, null, (Mesh.ArrayFormat)flags);
                myMesh.SurfaceSetMaterial(0, myMaterial);
            }

            myTexture.Create(myColorFormat, Resolution + 1, Resolution + 1, Resolution + 1, false, info.colors);
            myCollisionMesh.SetFaces(info.vertices);
        }
    }

    public static Vector3I IndexFromPos(Vector3 aPosition)
    {
        return new Vector3I(
            (int)(aPosition.X / Size),
            (int)(aPosition.Y / Size),
            (int)(aPosition.Z / Size));
    }

    private static Vector3 PosFromIndex(Vector3I aPosition)
    {
        return new Vector3
        {
            X = (int)(aPosition.X * Size),
            Y = (int)(aPosition.Y * Size),
            Z = (int)(aPosition.Z * Size)
        };
    }

    public void MarkDirty()
    {
        myIsDirty = true;
    }

    public void Update()
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();

        HashSet<Chunk> modifedChunks = new HashSet<Chunk>();

        foreach ((MaterialType type, LooseMaterial material) in MaterialGroups.ForEach(MaterialGroups.Loose))
        {
            foreach (Vector3I pos in Utils.Every(Vector3I.Zero, new Vector3I(Resolution - 1, Resolution - 1, Resolution - 1)))
            {
                material.SimulateOn(new NodeIndex { chunk = this, index = pos }, modifedChunks);
            }
        }

        foreach (Chunk chunk in modifedChunks)
        {
            Terrain.ourInstance.RegisterModification(chunk);
        }

        watch.Stop();

        if (watch.ElapsedMilliseconds > 50)
        {
            GD.Print("Update ", ChunkIndex, " took ", watch.ElapsedMilliseconds, "ms");
        }
    }

    public Vector3I NodePosFromWorldPos(Vector3 aPos)
    {
        return (Vector3I)((aPos - Position) / Size * Resolution);
    }

    public Vector3 WorldPosFromNodePos(Vector3I aVoxelPos)
    {
        return (Vector3)aVoxelPos * (float)Size / (float)Resolution + Position;
    }

    public IEnumerable<Vector3I> AffectedNodes(Aabb aArea)
    {
        Aabb affected = aArea.Intersection(new Aabb(Position, new Vector3(Size, Size, Size)));

        return Utils.Every(
            NodePosFromWorldPos(affected.Position),
            NodePosFromWorldPos(affected.End) - new Vector3I(1, 1, 1));
    }

    public static NodeIndex NodeAt(Vector3 aWorldPos)
    {
        Chunk chunk = Terrain.ourInstance.TryGetChunk(Vector3I.Zero); // TODO this is jank

        if (chunk == null)
        {
            return new NodeIndex { chunk = null };
        }

        return chunk.NodeAt(chunk.NodePosFromWorldPos(aWorldPos));
    }

    public NodeIndex NodeAt(Vector3I aIndex)
    {
        Vector3I chunkOffset = Utils.TruncatedDivision(aIndex, new Vector3I(Resolution, Resolution, Resolution));

        return new NodeIndex
        {
            chunk = chunkOffset.Equals(Vector3I.Zero) ? this : Terrain.ourInstance.TryGetChunk(ChunkIndex + chunkOffset),
            index = aIndex - chunkOffset * Resolution
        };
    }

    // Adaptation of https://paulbourke.net/geometry/polygonise/
    public async Task RegenerateMeshAsync()
    {
        if (!myIsDirty)
            return;

        Stopwatch watch = new Stopwatch();
        watch.Start();

        int[,,] values = new int[Resolution + 1, Resolution + 1, Resolution + 1];
        Vector3[,,] colors = new Vector3[Resolution + 1, Resolution + 1, Resolution + 1];

        for (int x = 0; x <= Resolution; x++)
        {
            for (int y = 0; y <= Resolution; y++)
            {
                for (int z = 0; z <= Resolution; z++)
                {
                    values[x, y, z] = 0;
                    colors[x, y, z] = Vector3.Zero;
                }
            }
        }

        List<Chunk> chunks = new List<Chunk>();

        foreach (Vector3I chunkOffset in Utils.Every(Vector3I.Zero, Vector3I.One))
        {
            Chunk c = Terrain.ourInstance.TryGetChunk(ChunkIndex + chunkOffset);

            if (c != null)
            {
                chunks.Add(c);
            }
        }

        foreach (Chunk c in chunks)
        {
            Vector3I offset = (c.ChunkIndex - ChunkIndex);

            Vector3I end = (Vector3I.One - offset) * (Resolution - 1);

            foreach (Vector3I pos in Utils.Every(Vector3I.Zero, end))
            {
                Vector3I at = pos + offset * Resolution;
                values[at.X, at.Y, at.Z] = MaterialInteractions.Total(ref c.myTerrainNodes[pos.X, pos.Y, pos.Z]);
                colors[at.X, at.Y, at.Z] = MaterialInteractions.Color(ref c.myTerrainNodes[pos.X, pos.Y, pos.Z]).RGB();
            }
        }

        float scale = (float)Size / (float)Resolution;

        Vector3[] vertices = await myMarchingCubes.Calculate(values, 16, scale);

        // Initialize the ArrayMesh.
        Array arrays = new Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = CalculateNormals(vertices);
        arrays[(int)Mesh.ArrayType.Custom0] = CalculateMaterials(vertices);

        Array<Image> imageLayers = new Array<Image>();

        imageLayers.Resize(Resolution + 1);

        // TODO this is likely flipped one way or the other
        for (int z = 0; z <= Resolution; z++)
        {
            const int width = Resolution + 1;
            const int height = Resolution + 1;

            byte[] imageData = new byte[width * height * 3];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    imageData[(y * width + x) * 3 + 0] = (byte)(colors[x, y, z].X * 255.0f);
                    imageData[(y * width + x) * 3 + 1] = (byte)(colors[x, y, z].Y * 255.0f);
                    imageData[(y * width + x) * 3 + 2] = (byte)(colors[x, y, z].Z * 255.0f);
                }
            }

            imageLayers[z] = Image.CreateFromData(Resolution + 1, Resolution + 1, false, myColorFormat, imageData);
        }


        Interlocked.Exchange(ref myNewMesh, new MeshInfo { vertices = vertices, arrays = arrays, colors = imageLayers });

        myIsDirty = false;

        watch.Stop();

        if (watch.ElapsedMilliseconds > 20)
        {
            GD.Print("Remesh ", ChunkIndex, " took ", watch.ElapsedMilliseconds, "ms ");
        }
    }

    Vector3[] CalculateNormals(Vector3[] aVerticies)
    {
        Vector3[] normals = new Vector3[aVerticies.Length];

        for (int i = 0; i < aVerticies.Length; i += 3)
        {
            Vector3 a = aVerticies[i + 0];
            Vector3 b = aVerticies[i + 1];
            Vector3 c = aVerticies[i + 2];

            Vector3 normal = (c - a).Cross(b - a).Normalized();

            normals[i] = normal;
            normals[i + 1] = normal;
            normals[i + 2] = normal;
        }

        return normals;
    }

    float[] CalculateMaterials(Vector3[] aVerticies)
    {
        float[] materials = new float[aVerticies.Length * 3];

        for (int i = 0; i < aVerticies.Length; i++)
        {
            Vector3 pos = aVerticies[i + 0];

            Vector3 Color = new Vector3(0.2f, 0.3f, 0.4f);

            materials[i + 0] = Color.X;
            materials[i + 1] = Color.Y;
            materials[i + 2] = Color.Z;
        }

        return materials;
    }
}

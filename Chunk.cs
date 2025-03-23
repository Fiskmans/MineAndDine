using Godot;
using Godot.Collections;
using MineAndDine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml.Linq;
using static Godot.TextServer;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Array = Godot.Collections.Array;

public partial class Chunk : Node3D
{
	[Export]
	public int myResolution { get; set; } = 16;

	public float mySize = 1;

	[Export]
	public Vector3I myChunkPos = new Vector3I(0, 0, 0);

	class MeshInfo
	{
		public List<Vector3> vertices;
		public Array arrays;
	}

	private bool myIsDirty = true;
	private MeshInfo myNewMesh = null;

	ArrayMesh myMesh = new ArrayMesh();
	ConcavePolygonShape3D myCollisionMesh;
	ShaderMaterial myMaterial;

	public const float mySurfaceValue = 0.5f;

	public struct NodeIndex
	{
		public Chunk chunk;
		public Vector3I index;

		public ref MaterialsList Get()
		{
			return ref chunk.myTerrainNodes[index.X, index.Y, index.Z];
		}

		public bool InBounds()
		{
			return chunk != null;
		}
	}

	MaterialsList[,,] myTerrainNodes;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Randomize();

        myTerrainNodes = new MaterialsList[myResolution, myResolution, myResolution];

		for (int x = 0; x < myResolution; x++)
		{
			for (int y = 0; y < myResolution; y++)
			{
				for (int z = 0; z < myResolution; z++)
				{
					float amount = 0.0f;

					amount -= (myChunkPos.Y + y / (float)myResolution) * 1.5f;
					amount += myChunkPos.X * 0.05f;
					amount += myChunkPos.Z * -0.1f;
					amount += GD.Randf() * 0.03f;

                    myTerrainNodes[x, y, z][(int)MaterialType.Dirt] = amount * 0.9f;
					myTerrainNodes[x, y, z][(int)MaterialType.Coal] = amount * 0.1f;
				}
			}
		}

		MeshInstance3D meshComp = GetNode<MeshInstance3D>("Mesh");

		myMaterial = meshComp.Mesh.SurfaceGetMaterial(0).Duplicate() as ShaderMaterial;

		myMaterial.SetShaderParameter("Size", mySize);
		myMaterial.SetShaderParameter("Offset", myChunkPos);
		myMaterial.SetShaderParameter("LightColor", new Vector3(0.8f, 0.7f, 0.5f));
		myMaterial.SetShaderParameter("DarkColor", new Vector3(0.4f, 0.3f, 0.1f));

		meshComp.Mesh = myMesh;

		myCollisionMesh = new ConcavePolygonShape3D();
		GetNode<CollisionShape3D>("Collision/CollisionMesh").Shape = myCollisionMesh;

		Terrain.ourInstance.RegisterModification(this);
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        if (myNewMesh != null)
        {
			Stopwatch watch = new Stopwatch();
			watch.Start();
            MeshInfo info = Interlocked.Exchange(ref myNewMesh, null);

            myMesh.ClearSurfaces();
            long flags = 0;

            flags |= (long)Mesh.ArrayCustomFormat.RgbFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift;


            if (info.vertices.Count > 0)
            {
                myMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, info.arrays, null, null, (Mesh.ArrayFormat)flags);
                myMesh.SurfaceSetMaterial(0, myMaterial);
            }

            myCollisionMesh.SetFaces(info.vertices.ToArray());

			GD.Print("Mesh application took ", watch.ElapsedMilliseconds, "ms");
        }
    }

    public void MarkDirty()
	{
		myIsDirty = true;
	}

	private bool Simulate(Vector3I aNodePos)
	{
		bool modified = false;

		NodeIndex node = NodeAt(aNodePos);
		NodeIndex below = NodeAt(aNodePos + Vector3I.Down);
		
		if (below.InBounds())
		{
			if (MaterialInteractions.MoveLoose(ref node.Get(), ref below.Get(), 1))
				modified = true;
		}

		return modified;
	}

	public void Update()
	{
		bool modified = false;

		Stopwatch watch = new Stopwatch();
		watch.Start();

		foreach (Vector3I pos in Utils.Every(Vector3I.Zero, new Vector3I(myResolution - 1, myResolution - 1, myResolution - 1)))
		{
			if (Simulate(pos))
				modified = true;
		}

		if (modified)
			Terrain.ourInstance.RegisterModification(this);

		GD.Print("Update ", myChunkPos, " took ", watch.ElapsedMilliseconds, "ms");
	}

	public Vector3I NodePosFromWorldPos(Vector3 aPos)
	{
		return (Vector3I)((aPos - Position) / mySize * myResolution);
	}

	public Vector3 WorldPosFromVoxelPos(Vector3I aVoxelPos)
	{
		return (Vector3)aVoxelPos * mySize / myResolution + Position;
	}

    public IEnumerable<Vector3I> AffectedNodes(Aabb aArea)
    {
        Aabb affected = aArea.Intersection(new Aabb(Position, new Vector3(mySize, mySize, mySize)));

		return Utils.Every(
			NodePosFromWorldPos(affected.Position),
			NodePosFromWorldPos(affected.End) - new Vector3I(1, 1, 1));
    }

	public NodeIndex NodeAt(Vector3I aPos)
    {
        Vector3I chunkOffset = Utils.TruncatedDivision(aPos, new Vector3I(myResolution, myResolution, myResolution));

		return new NodeIndex
		{
			chunk = chunkOffset.Equals(Vector3I.Zero) ? this : Terrain.ourInstance.TryGetChunk(myChunkPos + chunkOffset),
			index = aPos - chunkOffset * myResolution
        };
    }

    // Adaptation of https://paulbourke.net/geometry/polygonise/
    public void RegenerateMesh()
	{
		if (!myIsDirty)
			return;

		Stopwatch watch = new Stopwatch();
		watch.Start();

		float[,,] values = new float[myResolution + 1, myResolution + 1, myResolution + 1];

		for (int x = 0;	x <= myResolution; x++)
		{
			for (int y = 0;	y <= myResolution; y++)
			{
				for (int z = 0;	z <= myResolution; z++)
				{
					values[x, y, z] = 0.0f;
				}
			}
		}

		foreach(Vector3I chunkOffset in Utils.Every(Vector3I.Zero, Vector3I.One))
		{
			Chunk c = Terrain.ourInstance.TryGetChunk(myChunkPos + chunkOffset);

			if (c == null)
			{
				continue;
			}

			Vector3I end = (Vector3I.One - chunkOffset) * (myResolution - 1);

            foreach (Vector3I pos in Utils.Every(Vector3I.Zero, end))
            {
				Vector3I at = pos + chunkOffset * myResolution;
                values[at.X,at.Y,at.Z] = MaterialInteractions.Total(ref c.myTerrainNodes[pos.X, pos.Y, pos.Z]);
            }
        }
		
		MarchingCubes marching = new MarchingCubes(values);

		List<Vector3> vertices = marching.Calculate();
		List<Vector3> normals = CalculateNormals(vertices);
		List<float> materials = CalculateMaterials(vertices);

		// Initialize the ArrayMesh.
		var arrays = new Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		arrays[(int)Mesh.ArrayType.Custom0] = materials.ToArray();

		Interlocked.Exchange(ref myNewMesh, new MeshInfo { vertices = vertices, arrays = arrays });

		myIsDirty = false;

		GD.Print("Remesh ", myChunkPos, " took ", watch.ElapsedMilliseconds, "ms ");
	}

	List<Vector3> CalculateNormals(List<Vector3> aVerticies)
	{
		List<Vector3> normals = new List<Vector3>(aVerticies.Count);

		for (int i = 0; i < aVerticies.Count; i += 3)
		{
			Vector3 a = aVerticies[i + 0];
			Vector3 b = aVerticies[i + 1];
			Vector3 c = aVerticies[i + 2];

			Vector3 normal = (c - a).Cross(b - a).Normalized();
			
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
		}

		return normals;
	}

	List<float> CalculateMaterials(List<Vector3> aVerticies)
	{
		List<float> materials = new List<float>(aVerticies.Count * 3);

        for (int i = 0; i < aVerticies.Count; i++)
        {
            Vector3 pos = aVerticies[i + 0];

			Vector3 Color = new Vector3(0.2f, 0.3f, 0.4f);

			materials.Add(Color.X);
			materials.Add(Color.Y);
			materials.Add(Color.Z);
        }

        return materials;
	}
}

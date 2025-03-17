using Godot;
using MineAndDine;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using static Godot.TextServer;
using static System.Runtime.InteropServices.JavaScript.JSType;

public partial class Chunk : Node3D
{
	[Export]
	public int Resolution { get; set; } = 16;

	public float Size = 1;

	[Export]
	public Vector3I ChunkPos = new Vector3I(0, 0, 0);

	public TerrainGenerator OwningTerrain = null;

	ArrayMesh myMesh = new ArrayMesh();
	ConcavePolygonShape3D myCollisionMesh;
	ShaderMaterial myMaterial;

	public const float FullValue = 1.0f;
	public const float SurfaceValue = 0.5f;

	public ref struct NodeMapping
	{
		public Vector3I Position;
		public ref MaterialsList Node;
	}

    struct Colors
	{
		public static readonly Vector3 Dirt = new Vector3(0.8f, 0.7f, 0.4f);
		public static readonly Vector3 Coal = new Vector3(0.1f, 0.1f, 0.1f);
	}

	// blame the source for weird order
	static readonly Vector3I[] Corners =
	{
		new Vector3I(0, 0, 0),
		new Vector3I(1, 0, 0),
		new Vector3I(1, 1, 0),
		new Vector3I(0, 1, 0),
		new Vector3I(0, 0, 1),
		new Vector3I(1, 0, 1),
		new Vector3I(1, 1, 1),
		new Vector3I(0, 1, 1),
	};

	MaterialsList[,,] myTerrainNodes;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Randomize();

        myTerrainNodes = new MaterialsList[Resolution, Resolution, Resolution];

		for (int x = 0; x < Resolution; x++)
		{
			for (int y = 0; y < Resolution; y++)
			{
				for (int z = 0; z < Resolution; z++)
				{
					float amount = 1.0f - y / (float)Resolution;

					amount -= ChunkPos.Y * 1.0f;
					amount += ChunkPos.X * 0.05f;
					amount += ChunkPos.Z * -0.1f;
					amount += GD.Randf() * 0.03f;

                    myTerrainNodes[x, y, z][(int)MaterialType.Dirt] = amount * 0.9f;
					myTerrainNodes[x, y, z][(int)MaterialType.Coal] = amount * 0.1f;
				}
			}
		}

		MeshInstance3D meshComp = GetNode<MeshInstance3D>("Mesh");

		myMaterial = meshComp.Mesh.SurfaceGetMaterial(0).Duplicate() as ShaderMaterial;

		myMaterial.SetShaderParameter("Size", Size);
		myMaterial.SetShaderParameter("Offset", ChunkPos);
		myMaterial.SetShaderParameter("LightColor", new Vector3(0.8f, 0.7f, 0.5f));
		myMaterial.SetShaderParameter("DarkColor", new Vector3(0.4f, 0.3f, 0.1f));

		meshComp.Mesh = myMesh;

		myCollisionMesh = new ConcavePolygonShape3D();
		GetNode<CollisionShape3D>("Collision/CollisionMesh").Shape = myCollisionMesh;

		OwningTerrain.RegisterModification(this);
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private bool Simulate(Vector3I aNodePos)
	{
		bool modified = false;

		ref MaterialsList node = ref NodeAt(aNodePos);
		ref MaterialsList below = ref NodeAt(aNodePos + Vector3I.Down);
		
		if (!Unsafe.IsNullRef(ref below))
		{
			if (MaterialInteractions.MoveLoose(ref node, ref below, 1))
				modified = true;
		}

		return modified;
	}

	public void Update()
	{
		bool modified = false;

		foreach (Vector3I pos in Utils.Every(Vector3I.Zero, new Vector3I(Resolution - 1, Resolution - 1, Resolution - 1)))
		{
			if (Simulate(pos))
				modified = true;
		}

		if (modified)
			OwningTerrain.RegisterModification(this);
	}

	public Vector3I NodePosFromWorldPos(Vector3 aPos)
	{
		return (Vector3I)((aPos - Position) / Size * Resolution);
	}

	public Vector3 WorldPosFromVoxelPos(Vector3I aVoxelPos)
	{
		return (Vector3)aVoxelPos * Size / Resolution + Position;
	}

    public IEnumerable<Vector3I> AffectedNodes(Aabb aArea)
    {
        Aabb affected = aArea.Intersection(new Aabb(Position, new Vector3(Size, Size, Size)));

		return Utils.Every(
			NodePosFromWorldPos(affected.Position),
			NodePosFromWorldPos(affected.End) - new Vector3I(1, 1, 1));
    }

public ref MaterialsList NodeAt(Vector3I aPos)
{
	Vector3I offset = Utils.TruncatedDivision(aPos, new Vector3I(Resolution, Resolution, Resolution));
	Vector3I chunkPos = ChunkPos + offset;
	Vector3I pos = aPos - offset * Resolution;

	Chunk chunk = offset.Equals(Vector3I.Zero) ?
					this : OwningTerrain.TryGetChunk(chunkPos);

	if (chunk == null)
		return ref Unsafe.NullRef<MaterialsList>();

	return ref chunk.myTerrainNodes[pos.X, pos.Y, pos.Z];
}

	// Adaptation of https://paulbourke.net/geometry/polygonise/
	public void RegenerateMesh()
	{
		// TODO: this looks very computeshader'y
		float unit = 1.0f / Resolution;

		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<float> materials = new List<float>();

		for (int x = 0; x < Resolution; x++)
		{
			for (int y = 0; y < Resolution; y++)
			{
				for (int z = 0; z < Resolution; z++)
				{
					GenerateFragment(new Vector3I(x,y,z), vertices, normals, materials);
				}
			}
		}

		// Initialize the ArrayMesh.
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		arrays[(int)Mesh.ArrayType.Custom0] = materials.ToArray();

		myMesh.ClearSurfaces();
		long flags = 0;

		flags |= (long)Mesh.ArrayCustomFormat.RgbFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift;


		if (vertices.Count > 0)
		{
			myMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null, (Mesh.ArrayFormat)flags);
			myMesh.SurfaceSetMaterial(0, myMaterial);
		}

		myCollisionMesh.SetFaces(vertices.ToArray());
	}

	struct PointInfo
	{
		public Vector3 Position;
		public Vector3 Material;
	}

	private PointInfo PointOnEdge(ref MaterialsList aFirstNode, ref MaterialsList aSecondNode, int firstCorner, int secondCorner)
	{

		float weight = Mathf.InverseLerp(
			MaterialInteractions.Total(ref aFirstNode),  
			MaterialInteractions.Total(ref aSecondNode), 
			SurfaceValue);

		PointInfo info;

		info.Position = Utils.Lerp(Corners[firstCorner], Corners[secondCorner], weight);

		info.Material = Vector3.Zero;


		if (Unsafe.IsNullRef(ref aFirstNode) || Unsafe.IsNullRef(ref aSecondNode))
			return info;


        float colorTotal = 0;
		foreach (MaterialGroups.ColorMapping mapping in MaterialGroups.Colored)
		{
			float amount = Mathf.Lerp(aFirstNode[mapping.Index], aSecondNode[mapping.Index], weight);

            info.Material += mapping.Color * amount;
			colorTotal += amount;
        }

		info.Material /= colorTotal;

		return info;
	}

	private void GenerateFragment(Vector3I aSubPosition, List<Vector3> aVertexSink, List<Vector3> aNormalSink, List<float> aMaterialSink)
	{
		float unit = 1.0f / Resolution * Size;

		Vector3 orig = (Vector3)aSubPosition * unit;

		ref MaterialsList corner0 = ref NodeAt(aSubPosition + Corners[0]);
		ref MaterialsList corner1 = ref NodeAt(aSubPosition + Corners[1]);
		ref MaterialsList corner2 = ref NodeAt(aSubPosition + Corners[2]);
		ref MaterialsList corner3 = ref NodeAt(aSubPosition + Corners[3]);
		ref MaterialsList corner4 = ref NodeAt(aSubPosition + Corners[4]);
		ref MaterialsList corner5 = ref NodeAt(aSubPosition + Corners[5]);
		ref MaterialsList corner6 = ref NodeAt(aSubPosition + Corners[6]);
		ref MaterialsList corner7 = ref NodeAt(aSubPosition + Corners[7]);


		int index = 0;
		if (!MaterialInteractions.Solid(ref corner0, SurfaceValue)) index += 1;
		if (!MaterialInteractions.Solid(ref corner1, SurfaceValue)) index += 2;
		if (!MaterialInteractions.Solid(ref corner2, SurfaceValue)) index += 4;
		if (!MaterialInteractions.Solid(ref corner3, SurfaceValue)) index += 8;
		if (!MaterialInteractions.Solid(ref corner4, SurfaceValue)) index += 16;
		if (!MaterialInteractions.Solid(ref corner5, SurfaceValue)) index += 32;
		if (!MaterialInteractions.Solid(ref corner6, SurfaceValue)) index += 64;
		if (!MaterialInteractions.Solid(ref corner7, SurfaceValue)) index += 128;

		PointInfo[] edges =
		{
			PointOnEdge(ref corner0, ref corner1, 0, 1),
			PointOnEdge(ref corner1, ref corner2, 1, 2),
			PointOnEdge(ref corner2, ref corner3, 2, 3),
			PointOnEdge(ref corner3, ref corner0, 3, 0),
			PointOnEdge(ref corner4, ref corner5, 4, 5),
			PointOnEdge(ref corner5, ref corner6, 5, 6),
			PointOnEdge(ref corner6, ref corner7, 6, 7),
			PointOnEdge(ref corner7, ref corner4, 7, 4),
			PointOnEdge(ref corner0, ref corner4, 0, 4),
			PointOnEdge(ref corner1, ref corner5, 1, 5),
			PointOnEdge(ref corner2, ref corner6, 2, 6),
			PointOnEdge(ref corner3, ref corner7, 3, 7)
		};
				
		for (int i = 0; TriTable[index, i] != -1; i += 3)
		{
			PointInfo a = edges[TriTable[index, i + 0]];
			PointInfo b = edges[TriTable[index, i + 1]];
			PointInfo c = edges[TriTable[index, i + 2]];


			Vector3 aPos = orig + a.Position * unit;
			Vector3 bPos = orig + b.Position * unit;
			Vector3 cPos = orig + c.Position * unit;

			Vector3 normal = (bPos - aPos).Cross(cPos - aPos).Normalized();

			aVertexSink.Add(aPos);
			aNormalSink.Add(normal);
			aMaterialSink.Add(a.Material.X);
			aMaterialSink.Add(a.Material.Y);
			aMaterialSink.Add(a.Material.Z);

			aVertexSink.Add(cPos);
			aNormalSink.Add(normal);
            aMaterialSink.Add(c.Material.X);
            aMaterialSink.Add(c.Material.Y);
            aMaterialSink.Add(c.Material.Z);

            aVertexSink.Add(bPos);
			aNormalSink.Add(normal);
            aMaterialSink.Add(b.Material.X);
            aMaterialSink.Add(b.Material.Y);
            aMaterialSink.Add(b.Material.Z);
        }
    }


	static int[,] TriTable = new int[256, 16] {
		{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  0,  8,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  0,  1,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  8,  3,  9,  8,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  8,  3,  1,  2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  2, 10,  0,  2,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  2,  8,  3,  2, 10,  8, 10,  9,  8, -1, -1, -1, -1, -1, -1, -1 }, 
		{  3, 11,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  0, 11,  2,  8, 11,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  9,  0,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1, 11,  2,  1,  9, 11,  9,  8, 11, -1, -1, -1, -1, -1, -1, -1 }, 
		{  3, 10,  1, 11, 10,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  0, 10,  1,  0,  8, 10,  8, 11, 10, -1, -1, -1, -1, -1, -1, -1 }, 
		{  3,  9,  0,  3, 11,  9, 11, 10,  9, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  8, 10, 10,  8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },  // 0x10
		{  4,  7,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  4,  3,  0,  7,  3,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  0,  1,  9,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  4,  1,  9,  4,  7,  1,  7,  3,  1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  2, 10,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  3,  4,  7,  3,  0,  4,  1,  2, 10, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  2, 10,  9,  0,  2,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1 }, 
		{  2, 10,  9,  2,  9,  7,  2,  7,  3,  7,  9,  4, -1, -1, -1, -1 }, 
		{  8,  4,  7,  3, 11,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{ 11,  4,  7, 11,  2,  4,  2,  0,  4, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  0,  1,  8,  4,  7,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1 }, 
		{  4,  7, 11,  9,  4, 11,  9, 11,  2,  9,  2,  1, -1, -1, -1, -1 }, 
		{  3, 10,  1,  3, 11, 10,  7,  8,  4, -1, -1, -1, -1, -1, -1, -1 },
		{  1, 11, 10,  1,  4, 11,  1,  0,  4,  7, 11,  4, -1, -1, -1, -1 }, 
		{  4,  7,  8,  9,  0, 11,  9, 11, 10, 11,  0,  3, -1, -1, -1, -1 }, 
		{  4,  7, 11,  4, 11,  9,  9, 11, 10, -1, -1, -1, -1, -1, -1, -1 },  // 0x20
		{  9,  5,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  9,  5,  4,  0,  8,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  0,  5,  4,  1,  5,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  8,  5,  4,  8,  3,  5,  3,  1,  5, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  2, 10,  9,  5,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  3,  0,  8,  1,  2, 10,  4,  9,  5, -1, -1, -1, -1, -1, -1, -1 }, 
		{  5,  2, 10,  5,  4,  2,  4,  0,  2, -1, -1, -1, -1, -1, -1, -1 }, 
		{  2, 10,  5,  3,  2,  5,  3,  5,  4,  3,  4,  8, -1, -1, -1, -1 }, 
		{  9,  5,  4,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  0, 11,  2,  0,  8, 11,  4,  9,  5, -1, -1, -1, -1, -1, -1, -1 }, 
		{  0,  5,  4,  0,  1,  5,  2,  3, 11, -1, -1, -1, -1, -1, -1, -1 }, 
		{  2,  1,  5,  2,  5,  8,  2,  8, 11,  4,  8,  5, -1, -1, -1, -1 }, 
		{ 10,  3, 11, 10,  1,  3,  9,  5,  4, -1, -1, -1, -1, -1, -1, -1 },
		{  4,  9,  5,  0,  8,  1,  8, 10,  1,  8, 11, 10, -1, -1, -1, -1 }, 
		{  5,  4,  0,  5,  0, 11,  5, 11, 10, 11,  0,  3, -1, -1, -1, -1 }, 
		{  5,  4,  8,  5,  8, 10, 10,  8, 11, -1, -1, -1, -1, -1, -1, -1 },  // 0x30
		{  9,  7,  8,  5,  7,  9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  9,  3,  0,  9,  5,  3,  5,  7,  3, -1, -1, -1, -1, -1, -1, -1 }, 
		{  0,  7,  8,  0,  1,  7,  1,  5,  7, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  5,  3,  3,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  7,  8,  9,  5,  7, 10,  1,  2, -1, -1, -1, -1, -1, -1, -1 },
		{ 10,  1,  2,  9,  5,  0,  5,  3,  0,  5,  7,  3, -1, -1, -1, -1 }, 
		{  8,  0,  2,  8,  2,  5,  8,  5,  7, 10,  5,  2, -1, -1, -1, -1 }, 
		{  2, 10,  5,  2,  5,  3,  3,  5,  7, -1, -1, -1, -1, -1, -1, -1 }, 
		{  7,  9,  5,  7,  8,  9,  3, 11,  2, -1, -1, -1, -1, -1, -1, -1 },
		{  9,  5,  7,  9,  7,  2,  9,  2,  0,  2,  7, 11, -1, -1, -1, -1 }, 
		{  2,  3, 11,  0,  1,  8,  1,  7,  8,  1,  5,  7, -1, -1, -1, -1 }, 
		{ 11,  2,  1, 11,  1,  7,  7,  1,  5, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  5,  8,  8,  5,  7, 10,  1,  3, 10,  3, 11, -1, -1, -1, -1 },
		{  5,  7,  0,  5,  0,  9,  7, 11,  0,  1,  0, 10, 11, 10,  0, -1 }, 
		{ 11, 10,  0, 11,  0,  3, 10,  5,  0,  8,  0,  7,  5,  7,  0, -1 }, 
		{ 11, 10,  5,  7, 11,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },  // 0x40
		{ 10,  6,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  8,  3,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  0,  1,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  8,  3,  1,  9,  8,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  6,  5,  2,  6,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  1,  6,  5,  1,  2,  6,  3,  0,  8, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  6,  5,  9,  0,  6,  0,  2,  6, -1, -1, -1, -1, -1, -1, -1 }, 
		{  5,  9,  8,  5,  8,  2,  5,  2,  6,  3,  2,  8, -1, -1, -1, -1 }, 
		{  2,  3, 11, 10,  6,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{ 11,  0,  8, 11,  2,  0, 10,  6,  5, -1, -1, -1, -1, -1, -1, -1 }, 
		{  0,  1,  9,  2,  3, 11,  5, 10,  6, -1, -1, -1, -1, -1, -1, -1 }, 
		{  5, 10,  6,  1,  9,  2,  9, 11,  2,  9,  8, 11, -1, -1, -1, -1 }, 
		{  6,  3, 11,  6,  5,  3,  5,  1,  3, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  8, 11,  0, 11,  5,  0,  5,  1,  5, 11,  6, -1, -1, -1, -1 }, 
		{  3, 11,  6,  0,  3,  6,  0,  6,  5,  0,  5,  9, -1, -1, -1, -1 }, 
		{  6,  5,  9,  6,  9, 11, 11,  9,  8, -1, -1, -1, -1, -1, -1, -1 },  // 0x50
		{  5, 10,  6,  4,  7,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  4,  3,  0,  4,  7,  3,  6,  5, 10, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  9,  0,  5, 10,  6,  8,  4,  7, -1, -1, -1, -1, -1, -1, -1 }, 
		{ 10,  6,  5,  1,  9,  7,  1,  7,  3,  7,  9,  4, -1, -1, -1, -1 }, 
		{  6,  1,  2,  6,  5,  1,  4,  7,  8, -1, -1, -1, -1, -1, -1, -1 },
		{  1,  2,  5,  5,  2,  6,  3,  0,  4,  3,  4,  7, -1, -1, -1, -1 }, 
		{  8,  4,  7,  9,  0,  5,  0,  6,  5,  0,  2,  6, -1, -1, -1, -1 }, 
		{  7,  3,  9,  7,  9,  4,  3,  2,  9,  5,  9,  6,  2,  6,  9, -1 }, 
		{  3, 11,  2,  7,  8,  4, 10,  6,  5, -1, -1, -1, -1, -1, -1, -1 },
		{  5, 10,  6,  4,  7,  2,  4,  2,  0,  2,  7, 11, -1, -1, -1, -1 }, 
		{  0,  1,  9,  4,  7,  8,  2,  3, 11,  5, 10,  6, -1, -1, -1, -1 }, 
		{  9,  2,  1,  9, 11,  2,  9,  4, 11,  7, 11,  4,  5, 10,  6, -1 }, 
		{  8,  4,  7,  3, 11,  5,  3,  5,  1,  5, 11,  6, -1, -1, -1, -1 },
		{  5,  1, 11,  5, 11,  6,  1,  0, 11,  7, 11,  4,  0,  4, 11, -1 }, 
		{  0,  5,  9,  0,  6,  5,  0,  3,  6, 11,  6,  3,  8,  4,  7, -1 }, 
		{  6,  5,  9,  6,  9, 11,  4,  7,  9,  7, 11,  9, -1, -1, -1, -1 },  // 0x60
		{ 10,  4,  9,  6,  4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  4, 10,  6,  4,  9, 10,  0,  8,  3, -1, -1, -1, -1, -1, -1, -1 }, 
		{ 10,  0,  1, 10,  6,  0,  6,  4,  0, -1, -1, -1, -1, -1, -1, -1 }, 
		{  8,  3,  1,  8,  1,  6,  8,  6,  4,  6,  1, 10, -1, -1, -1, -1 }, 
		{  1,  4,  9,  1,  2,  4,  2,  6,  4, -1, -1, -1, -1, -1, -1, -1 },
		{  3,  0,  8,  1,  2,  9,  2,  4,  9,  2,  6,  4, -1, -1, -1, -1 }, 
		{  0,  2,  4,  4,  2,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  8,  3,  2,  8,  2,  4,  4,  2,  6, -1, -1, -1, -1, -1, -1, -1 }, 
		{ 10,  4,  9, 10,  6,  4, 11,  2,  3, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  8,  2,  2,  8, 11,  4,  9, 10,  4, 10,  6, -1, -1, -1, -1 }, 
		{  3, 11,  2,  0,  1,  6,  0,  6,  4,  6,  1, 10, -1, -1, -1, -1 }, 
		{  6,  4,  1,  6,  1, 10,  4,  8,  1,  2,  1, 11,  8, 11,  1, -1 }, 
		{  9,  6,  4,  9,  3,  6,  9,  1,  3, 11,  6,  3, -1, -1, -1, -1 },
		{  8, 11,  1,  8,  1,  0, 11,  6,  1,  9,  1,  4,  6,  4,  1, -1 }, 
		{  3, 11,  6,  3,  6,  0,  0,  6,  4, -1, -1, -1, -1, -1, -1, -1 }, 
		{  6,  4,  8, 11,  6,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },  // 0x70
		{  7, 10,  6,  7,  8, 10,  8,  9, 10, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  7,  3,  0, 10,  7,  0,  9, 10,  6,  7, 10, -1, -1, -1, -1 }, 
		{ 10,  6,  7,  1, 10,  7,  1,  7,  8,  1,  8,  0, -1, -1, -1, -1 }, 
		{ 10,  6,  7, 10,  7,  1,  1,  7,  3, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  2,  6,  1,  6,  8,  1,  8,  9,  8,  6,  7, -1, -1, -1, -1 },
		{  2,  6,  9,  2,  9,  1,  6,  7,  9,  0,  9,  3,  7,  3,  9, -1 }, 
		{  7,  8,  0,  7,  0,  6,  6,  0,  2, -1, -1, -1, -1, -1, -1, -1 }, 
		{  7,  3,  2,  6,  7,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  2,  3, 11, 10,  6,  8, 10,  8,  9,  8,  6,  7, -1, -1, -1, -1 },
		{  2,  0,  7,  2,  7, 11,  0,  9,  7,  6,  7, 10,  9, 10,  7, -1 }, 
		{  1,  8,  0,  1,  7,  8,  1, 10,  7,  6,  7, 10,  2,  3, 11, -1 }, 
		{ 11,  2,  1, 11,  1,  7, 10,  6,  1,  6,  7,  1, -1, -1, -1, -1 }, 
		{  8,  9,  6,  8,  6,  7,  9,  1,  6, 11,  6,  3,  1,  3,  6, -1 },
		{  0,  9,  1, 11,  6,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  7,  8,  0,  7,  0,  6,  3, 11,  0, 11,  6,  0, -1, -1, -1, -1 }, 
		{  7, 11,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },  // 0x80
		{  7,  6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  3,  0,  8, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  0,  1,  9, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  8,  1,  9,  8,  3,  1, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1 }, 
		{ 10,  1,  2,  6, 11,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  1,  2, 10,  3,  0,  8,  6, 11,  7, -1, -1, -1, -1, -1, -1, -1 }, 
		{  2,  9,  0,  2, 10,  9,  6, 11,  7, -1, -1, -1, -1, -1, -1, -1 }, 
		{  6, 11,  7,  2, 10,  3, 10,  8,  3, 10,  9,  8, -1, -1, -1, -1 }, 
		{  7,  2,  3,  6,  2,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  7,  0,  8,  7,  6,  0,  6,  2,  0, -1, -1, -1, -1, -1, -1, -1 }, 
		{  2,  7,  6,  2,  3,  7,  0,  1,  9, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  6,  2,  1,  8,  6,  1,  9,  8,  8,  7,  6, -1, -1, -1, -1 }, 
		{ 10,  7,  6, 10,  1,  7,  1,  3,  7, -1, -1, -1, -1, -1, -1, -1 },
		{ 10,  7,  6,  1,  7, 10,  1,  8,  7,  1,  0,  8, -1, -1, -1, -1 }, 
		{  0,  3,  7,  0,  7, 10,  0, 10,  9,  6, 10,  7, -1, -1, -1, -1 }, 
		{  7,  6, 10,  7, 10,  8,  8, 10,  9, -1, -1, -1, -1, -1, -1, -1 },  // 0x90
		{  6,  8,  4, 11,  8,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  3,  6, 11,  3,  0,  6,  0,  4,  6, -1, -1, -1, -1, -1, -1, -1 }, 
		{  8,  6, 11,  8,  4,  6,  9,  0,  1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  4,  6,  9,  6,  3,  9,  3,  1, 11,  3,  6, -1, -1, -1, -1 }, 
		{  6,  8,  4,  6, 11,  8,  2, 10,  1, -1, -1, -1, -1, -1, -1, -1 },
		{  1,  2, 10,  3,  0, 11,  0,  6, 11,  0,  4,  6, -1, -1, -1, -1 }, 
		{  4, 11,  8,  4,  6, 11,  0,  2,  9,  2, 10,  9, -1, -1, -1, -1 }, 
		{ 10,  9,  3, 10,  3,  2,  9,  4,  3, 11,  3,  6,  4,  6,  3, -1 }, 
		{  8,  2,  3,  8,  4,  2,  4,  6,  2, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  4,  2,  4,  6,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, // this one
		{  1,  9,  0,  2,  3,  4,  2,  4,  6,  4,  3,  8, -1, -1, -1, -1 }, 
		{  1,  9,  4,  1,  4,  2,  2,  4,  6, -1, -1, -1, -1, -1, -1, -1 }, 
		{  8,  1,  3,  8,  6,  1,  8,  4,  6,  6, 10,  1, -1, -1, -1, -1 },
		{ 10,  1,  0, 10,  0,  6,  6,  0,  4, -1, -1, -1, -1, -1, -1, -1 }, 
		{  4,  6,  3,  4,  3,  8,  6, 10,  3,  0,  3,  9, 10,  9,  3, -1 }, 
		{ 10,  9,  4,  6, 10,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },  // 0xA0
		{  4,  9,  5,  7,  6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  8,  3,  4,  9,  5, 11,  7,  6, -1, -1, -1, -1, -1, -1, -1 }, 
		{  5,  0,  1,  5,  4,  0,  7,  6, 11, -1, -1, -1, -1, -1, -1, -1 }, 
		{ 11,  7,  6,  8,  3,  4,  3,  5,  4,  3,  1,  5, -1, -1, -1, -1 }, 
		{  9,  5,  4, 10,  1,  2,  7,  6, 11, -1, -1, -1, -1, -1, -1, -1 },
		{  6, 11,  7,  1,  2, 10,  0,  8,  3,  4,  9,  5, -1, -1, -1, -1 }, 
		{  7,  6, 11,  5,  4, 10,  4,  2, 10,  4,  0,  2, -1, -1, -1, -1 }, 
		{  3,  4,  8,  3,  5,  4,  3,  2,  5, 10,  5,  2, 11,  7,  6, -1 }, 
		{  7,  2,  3,  7,  6,  2,  5,  4,  9, -1, -1, -1, -1, -1, -1, -1 },
		{  9,  5,  4,  0,  8,  6,  0,  6,  2,  6,  8,  7, -1, -1, -1, -1 }, 
		{  3,  6,  2,  3,  7,  6,  1,  5,  0,  5,  4,  0, -1, -1, -1, -1 }, 
		{  6,  2,  8,  6,  8,  7,  2,  1,  8,  4,  8,  5,  1,  5,  8, -1 }, 
		{  9,  5,  4, 10,  1,  6,  1,  7,  6,  1,  3,  7, -1, -1, -1, -1 },
		{  1,  6, 10,  1,  7,  6,  1,  0,  7,  8,  7,  0,  9,  5,  4, -1 }, 
		{  4,  0, 10,  4, 10,  5,  0,  3, 10,  6, 10,  7,  3,  7, 10, -1 }, 
		{  7,  6, 10,  7, 10,  8,  5,  4, 10,  4,  8, 10, -1, -1, -1, -1 },  // 0xB0
		{  6,  9,  5,  6, 11,  9, 11,  8,  9, -1, -1, -1, -1, -1, -1, -1 },
		{  3,  6, 11,  0,  6,  3,  0,  5,  6,  0,  9,  5, -1, -1, -1, -1 }, 
		{  0, 11,  8,  0,  5, 11,  0,  1,  5,  5,  6, 11, -1, -1, -1, -1 }, 
		{  6, 11,  3,  6,  3,  5,  5,  3,  1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  2, 10,  9,  5, 11,  9, 11,  8, 11,  5,  6, -1, -1, -1, -1 },
		{  0, 11,  3,  0,  6, 11,  0,  9,  6,  5,  6,  9,  1,  2, 10, -1 }, 
		{ 11,  8,  5, 11,  5,  6,  8,  0,  5, 10,  5,  2,  0,  2,  5, -1 }, 
		{  6, 11,  3,  6,  3,  5,  2, 10,  3, 10,  5,  3, -1, -1, -1, -1 }, 
		{  5,  8,  9,  5,  2,  8,  5,  6,  2,  3,  8,  2, -1, -1, -1, -1 },
		{  9,  5,  6,  9,  6,  0,  0,  6,  2, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  5,  8,  1,  8,  0,  5,  6,  8,  3,  8,  2,  6,  2,  8, -1 }, 
		{  1,  5,  6,  2,  1,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  1,  3,  6,  1,  6, 10,  3,  8,  6,  5,  6,  9,  8,  9,  6, -1 },
		{ 10,  1,  0, 10,  0,  6,  9,  5,  0,  5,  6,  0, -1, -1, -1, -1 }, 
		{  0,  3,  8,  5,  6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{ 10,  5,  6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },  // 0xC0
		{ 11,  5, 10,  7,  5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{ 11,  5, 10, 11,  7,  5,  8,  3,  0, -1, -1, -1, -1, -1, -1, -1 },
		{  5, 11,  7,  5, 10, 11,  1,  9,  0, -1, -1, -1, -1, -1, -1, -1 }, 
		{ 10,  7,  5, 10, 11,  7,  9,  8,  1,  8,  3,  1, -1, -1, -1, -1 }, 
		{ 11,  1,  2, 11,  7,  1,  7,  5,  1, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  8,  3,  1,  2,  7,  1,  7,  5,  7,  2, 11, -1, -1, -1, -1 }, 
		{  9,  7,  5,  9,  2,  7,  9,  0,  2,  2, 11,  7, -1, -1, -1, -1 }, 
		{  7,  5,  2,  7,  2, 11,  5,  9,  2,  3,  2,  8,  9,  8,  2, -1 }, 
		{  2,  5, 10,  2,  3,  5,  3,  7,  5, -1, -1, -1, -1, -1, -1, -1 },
		{  8,  2,  0,  8,  5,  2,  8,  7,  5, 10,  2,  5, -1, -1, -1, -1 }, 
		{  9,  0,  1,  5, 10,  3,  5,  3,  7,  3, 10,  2, -1, -1, -1, -1 }, 
		{  9,  8,  2,  9,  2,  1,  8,  7,  2, 10,  2,  5,  7,  5,  2, -1 }, 
		{  1,  3,  5,  3,  7,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  8,  7,  0,  7,  1,  1,  7,  5, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  0,  3,  9,  3,  5,  5,  3,  7, -1, -1, -1, -1, -1, -1, -1 }, 
		{  9,  8,  7,  5,  9,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },  // 0xD0
		{  5,  8,  4,  5, 10,  8, 10, 11,  8, -1, -1, -1, -1, -1, -1, -1 },
		{  5,  0,  4,  5, 11,  0,  5, 10, 11, 11,  3,  0, -1, -1, -1, -1 },
		{  0,  1,  9,  8,  4, 10,  8, 10, 11, 10,  4,  5, -1, -1, -1, -1 }, 
		{ 10, 11,  4, 10,  4,  5, 11,  3,  4,  9,  4,  1,  3,  1,  4, -1 }, 
		{  2,  5,  1,  2,  8,  5,  2, 11,  8,  4,  5,  8, -1, -1, -1, -1 },
		{  0,  4, 11,  0, 11,  3,  4,  5, 11,  2, 11,  1,  5,  1, 11, -1 }, 
		{  0,  2,  5,  0,  5,  9,  2, 11,  5,  4,  5,  8, 11,  8,  5, -1 }, 
		{  9,  4,  5,  2, 11,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  2,  5, 10,  3,  5,  2,  3,  4,  5,  3,  8,  4, -1, -1, -1, -1 },
		{  5, 10,  2,  5,  2,  4,  4,  2,  0, -1, -1, -1, -1, -1, -1, -1 }, 
		{  3, 10,  2,  3,  5, 10,  3,  8,  5,  4,  5,  8,  0,  1,  9, -1 }, 
		{  5, 10,  2,  5,  2,  4,  1,  9,  2,  9,  4,  2, -1, -1, -1, -1 }, 
		{  8,  4,  5,  8,  5,  3,  3,  5,  1, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  4,  5,  1,  0,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  8,  4,  5,  8,  5,  3,  9,  0,  5,  0,  3,  5, -1, -1, -1, -1 }, 
		{  9,  4,  5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },  // 0xE0
		{  4, 11,  7,  4,  9, 11,  9, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  8,  3,  4,  9,  7,  9, 11,  7,  9, 10, 11, -1, -1, -1, -1 },
		{  1, 10, 11,  1, 11,  4,  1,  4,  0,  7,  4, 11, -1, -1, -1, -1 }, 
		{  3,  1,  4,  3,  4,  8,  1, 10,  4,  7,  4, 11, 10, 11,  4, -1 }, 
		{  4, 11,  7,  9, 11,  4,  9,  2, 11,  9,  1,  2, -1, -1, -1, -1 },
		{  9,  7,  4,  9, 11,  7,  9,  1, 11,  2, 11,  1,  0,  8,  3, -1 }, 
		{ 11,  7,  4, 11,  4,  2,  2,  4,  0, -1, -1, -1, -1, -1, -1, -1 }, 
		{ 11,  7,  4, 11,  4,  2,  8,  3,  4,  3,  2,  4, -1, -1, -1, -1 }, 
		{  2,  9, 10,  2,  7,  9,  2,  3,  7,  7,  4,  9, -1, -1, -1, -1 },
		{  9, 10,  7,  9,  7,  4, 10,  2,  7,  8,  7,  0,  2,  0,  7, -1 }, 
		{  3,  7, 10,  3, 10,  2,  7,  4, 10,  1, 10,  0,  4,  0, 10, -1 }, 
		{  1, 10,  2,  8,  7,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  4,  9,  1,  4,  1,  7,  7,  1,  3, -1, -1, -1, -1, -1, -1, -1 },
		{  4,  9,  1,  4,  1,  7,  0,  8,  1,  8,  7,  1, -1, -1, -1, -1 }, 
		{  4,  0,  3,  7,  4,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 
		{  4,  8,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },  // 0xF0
		{  9, 10,  8, 10, 11,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  3,  0,  9,  3,  9, 11, 11,  9, 10, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  1, 10,  0, 10,  8,  8, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
		{  3,  1, 10, 11,  3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  1,  2, 11,  1, 11,  9,  9, 11,  8, -1, -1, -1, -1, -1, -1, -1 },
		{  3,  0,  9,  3,  9, 11,  1,  2,  9,  2, 11,  9, -1, -1, -1, -1 },
		{  0,  2, 11,  8,  0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  3,  2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  2,  3,  8,  2,  8, 10, 10,  8,  9, -1, -1, -1, -1, -1, -1, -1 },
		{  9, 10,  2,  0,  9,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  2,  3,  8,  2,  8, 10,  0,  1,  8,  1, 10,  8, -1, -1, -1, -1 },
		{  1, 10,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  1,  3,  8,  9,  1,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  9,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{  0,  3,  8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
		{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }
	};
}

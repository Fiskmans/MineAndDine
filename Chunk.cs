using Godot;
using System;
using System.Collections.Generic;

public partial class Chunk : Node3D
{
	[Export]
	public int Resolution { get; set; } = 16;

	public struct Pos
	{
		public int x; 
		public int y; 
		public int z;
	}

	public struct Voxel
	{
		public float Density;
	}

	Voxel[,,] myVoxels;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        GD.Randomize();
        
		myVoxels = new Voxel[Resolution, Resolution, Resolution];

		for (int x = 0; x < Resolution; x++)
		{
			for (int y = 0; y < Resolution; y++)
			{
				for (int z = 0; z < Resolution; z++)
				{
					myVoxels[x, y, z] = new Voxel { Density = GD.Randf() };
				}
			}
		}

		RegenerateMesh();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void RegenerateMesh()
    {
		// TODO: this looks very computeshader'y
		float unit = 1.0f / Resolution;

		List<Vector3> vertices = new List<Vector3>();

        for (int x = 0; x < Resolution; x++)
        {
            for (int y = 0; y < Resolution; y++)
            {
                for (int z = 0; z < Resolution; z++)
                {
					GenerateFragment(new Pos { x=x, y=y, z=z }, vertices);
                }
            }
        }

        // Initialize the ArrayMesh.
        var arrMesh = new ArrayMesh();
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();

        // Create the Mesh.
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

		GetNode<MeshInstance3D>("Mesh").Mesh = arrMesh;
	}

	private void GenerateFragment(Pos aSubPosition, List<Vector3> aVertexSink)
    {
        float unit = 1.0f / Resolution;

		Vector3 orig = new Vector3(aSubPosition.x * unit, aSubPosition.y * unit, aSubPosition.z * unit);

		Voxel v = myVoxels[aSubPosition.x, aSubPosition.y, aSubPosition.z];

        aVertexSink.Add(orig + new Vector3(0, unit * v.Density, 0));
        aVertexSink.Add(orig + new Vector3(unit * v.Density, 0, 0));
        aVertexSink.Add(orig + new Vector3(0, 0, unit * v.Density));
    }
}

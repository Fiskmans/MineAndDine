using Godot;
using MineAndDine;
using MineAndDine.Code.Materials;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Drawing;
using Color = Godot.Color;
using System.Threading;
using System.Linq;
using MineAndDine.Code.Extensions;

public partial class GenerationVisualizer : MeshInstance3D
{
    [Export]
    public MaterialType myMaterial;

    [Export]
    public int mySize = 64;

    [Export]
    public float myScale = 10.0f;

    [Export]
    public float mySurface = 0.5f;

    struct Layer
    {
        public Vector3[] Verts;
        public Color Color;
    }

    ConcurrentQueue<Layer> myNewLayers = new ConcurrentQueue<Layer>();

    List<MeshInstance3D> myLayers = new List<MeshInstance3D>();

    Vector3 myOffset;


    public override void _Ready()
    {
        MaterialGroups.GenerateTables();

        base._Ready();
        Mesh = new ArrayMesh();
        myOffset = GlobalPosition;

        new Thread(GenerateLayers).Start();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Layer layer;

        while (myNewLayers.TryDequeue(out layer))
        {
            if (layer.Verts.Length == 0)
            {
                break;
            }
;

            (Mesh as ArrayMesh).AddSurfaceFromVerticies(layer.Verts.Select(v => v - Vector3.One * mySize / 2).ToArray());

            StandardMaterial3D material = new StandardMaterial3D();

            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            material.AlbedoColor = layer.Color;
            
            Mesh.SurfaceSetMaterial(Mesh.GetSurfaceCount() - 1, material);
        }
    }

    private void GenerateLayers()
    {
        float[,,] data = new float[mySize, mySize, mySize];

        MineAndDine.Noise.Noise generator = MaterialGroups.Generatable[myMaterial];
        Color color = MaterialGroups.Color[myMaterial];

        foreach (Vector3I pos in Utils.EveryIndex(Vector3I.Zero, Vector3I.One * mySize))
        {
            data[pos.X, pos.Y, pos.Z] = generator.Generate((Vector3)pos - Vector3.One * mySize / 2 + myOffset);
        }

        for (int i = 0; i < 256; i += 10)
        {
            MarchingCubes cubes = new MarchingCubes(data, mySurface);

            myNewLayers.Enqueue(new Layer { Color = new Color(color) { A = 0.3f * (float)i/256 }, Verts = cubes.Calculate() });
        }
    }
}

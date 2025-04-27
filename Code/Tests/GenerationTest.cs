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
using MineAndDine.Code.Tests;
using System.Threading.Tasks;

public partial class GenerationTest : Test
{
    [Export]
    public MaterialType myMaterial;

    [Export]
    public int mySize = 64;

    [Export]
    public float myScale = 8.0f;


    struct Layer
    {
        public Vector3[] Verts;
        public Color Color;
        public bool Final;
    }

    ConcurrentQueue<Layer> myNewLayers = new ConcurrentQueue<Layer>();
    ArrayMesh Mesh;

    Vector3 myOffset;
    int myLayers = 0;


    public override void _Ready()
    {
        MaterialGroups.GenerateTables();

        base._Ready();
        MeshInstance3D child = new MeshInstance3D();
        child.Position = Vector3.One * -0.5f;

        Mesh = new ArrayMesh();
        child.Mesh = Mesh;
        AddChild(child);
        myOffset = GlobalPosition;

        myDescription = $"Generation of {myMaterial.ToString()}";

        SetStatus("Starting");

        new Thread(GenerateLayers).Start();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Result != ResultType.Running)
        {
            return;
        }

        Layer layer;

        while (myNewLayers.TryDequeue(out layer))
        {
            if (layer.Final)
            {
                Expect(Mesh.GetSurfaceCount() > 1);
                Passed();
                break;
            }

            myLayers++;
            SetStatus($"Layer {myLayers}: {layer.Verts.Length / 3} tris");

            if (layer.Verts.Length == 0)
            {
                break;
            }

            Mesh.AddSurfaceFromVerticies(layer.Verts);

            StandardMaterial3D material = new StandardMaterial3D();

            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            material.AlbedoColor = layer.Color;
            material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
            
            Mesh.SurfaceSetMaterial(Mesh.GetSurfaceCount() - 1, material);
        }
    }

    private void GenerateLayers()
    {
        int[,,] data = new int[mySize, mySize, mySize];

        MineAndDine.Noise.Noise generator = MaterialGroups.Generatable[myMaterial];
        Color color = MaterialGroups.Color[myMaterial];

        foreach (Vector3I pos in Utils.EveryIndex(Vector3I.Zero, Vector3I.One * mySize))
        {
            data[pos.X, pos.Y, pos.Z] = (int)generator.Generate((Vector3)pos - Vector3.One * mySize / 2 + myOffset);
        }

        MarchingCubes cubes = new MarchingCubes(Vector3I.One * mySize);

        List<Task<Vector3[]>> tasks = new List<Task<Vector3[]>>();

        for (uint i = 0; i < 256; i += 10)
        {
            tasks.Add(cubes.Calculate(data, i, 1f / mySize));
        }

        while (tasks.Count > 0)
        {
            int index = Task.WaitAny(tasks.ToArray());

            myNewLayers.Enqueue(new Layer { Color = new Color(color) { A = 0.5f }, Verts = tasks[index].Result });
            tasks.RemoveAt(index);
        }

        myNewLayers.Enqueue(new Layer { Final = true });
    }
}

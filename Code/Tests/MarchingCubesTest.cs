using Godot;
using MineAndDine;
using MineAndDine.Code.Extensions;
using MineAndDine.Code.Tests;
using System;

public partial class MarchingCubesTest : Test
{
    MeshInstance3D myCubeMeshNode = new MeshInstance3D();
    ArrayMesh myMesh = new ArrayMesh();
    float myValue = 0;


    public override void _Ready()
    {
        base._Ready();
        myCubeMeshNode.Mesh = myMesh;

        AddChild(myCubeMeshNode);
        myCubeMeshNode.Position = Vector3.One * -0.5f;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        myValue += (float)delta;
        if (myValue > 2.0f)
        {
            myValue -= 3.0f;
        }

        MarchingCubes cubes = new MarchingCubes(new float[,,] { { { 1.5f, 1 }, { 1, 0.5f } }, { { 1, 0.5f }, { 0.5f, 0 } } }, myValue);

        myMesh.ClearSurfaces();

        bool result = myMesh.AddSurfaceFromVerticies(cubes.Calculate());

        Expect(result == (myValue > 0 && myValue < 1.5f));

        Setstatus(myValue.ToString().Substr(0, 5));
    }
}

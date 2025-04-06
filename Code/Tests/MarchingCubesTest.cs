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
    Vector3[] myLast = Array.Empty<Vector3>();


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

        if (Result != ResultType.Running)
        {
            return;
        }

        myValue += (float)delta;
        if (myValue > 2.0f)
        {
            Passed();
        }

        MarchingCubes cubes = new MarchingCubes(new float[,,] { { { 1.5f, 1 }, { 1, 0.5f } }, { { 1, 0.5f }, { 0.5f, 0 } } }, myValue);

        myMesh.ClearSurfaces();

        Vector3[] next = cubes.Calculate();

        bool tooFar = false;

        foreach (Vector3 v in next)
        {
            float closest = 100;
            if (myLast.Length == 0)
            {
                break;
            }
            foreach (Vector3 v2 in myLast)
            {
                closest = Mathf.Min(closest, v.DistanceTo(v2));
            }

            if (closest > delta * 4)
            {
                tooFar = true;
                break;
            }
        }

        myLast = next;

        Expect(!tooFar);

        bool result = myMesh.AddSurfaceFromVerticies(next);

        Expect(result == (myValue > 0 && myValue < 1.5f));

        Setstatus(myValue.ToString().Substr(0, 5));
    }
}

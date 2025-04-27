using Godot;
using MineAndDine;
using MineAndDine.Code.Extensions;
using MineAndDine.Code.Tests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[Test]
public partial class MarchingCubesTest : Test
{
    MarchingCubes myMarchingCubes = new MarchingCubes(Vector3I.One * 2);
    MeshInstance3D myCubeMeshNode = new MeshInstance3D();
    ArrayMesh myMesh = new ArrayMesh();
    uint myValue = 0;
    int myCurrent = 0;
    Vector3[] myLast = Array.Empty<Vector3>();
    Timer myTimer = new Timer();

    const int Cor = 50;
    const int Ins = 25;
    const int Out = 10;
    const int Far = 0;

    List<int[,,]> myTests = new List<int[,,]>
    {
        new int[,,] { { { Cor, Ins }, { Ins, Out } }, { { Ins, Out }, { Out, Far } } },
        new int[,,] { { { Cor, Cor }, { Ins, Ins } }, { { Out, Out }, { Far, Far } } },
        new int[,,] { { { Cor, Cor }, { Cor, Cor } }, { { Far, Far }, { Far, Far } } },
        new int[,,] { { { Cor, Cor }, { Cor, Cor } }, { { Cor, Far }, { Far, Far } } },
        new int[,,] { { { Cor, Cor }, { Cor, Cor } }, { { Far, Cor }, { Far, Far } } },
    };

    public override void _Ready()
    {
        myDescription = "Marching cubes";
        base._Ready();
        myCubeMeshNode.Mesh = myMesh;

        AddChild(myCubeMeshNode);
        myCubeMeshNode.Position = Vector3.One * -0.5f;

        myTimer.OneShot = true;
        myTimer.Autostart = true;
        myTimer.Timeout += () => 
        { 
            NextAsync().ContinueWith(
                (task) =>
                {
                    RenderingServer.CallOnRenderThread(Callable.From(() => myTimer.Start()));
                }); 
        };
        myTimer.WaitTime = 0.0001;

        AddChild(myTimer);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    private async Task NextAsync()
    {
        if (Result != ResultType.Running)
        {
            return;
        }

        myValue++;
        if (myValue > 40)
        {
            myCurrent++;
            myValue = 0;
            myLast = [];

            if (myCurrent == myTests.Count)
            {
                Passed();
                return;
            }
        }

        int[,,] data = myTests[myCurrent];

        Vector3[] next = await myMarchingCubes.Calculate(data, myValue);

        SetStatus($"{myValue} => {next.Length}");

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

            if (closest > 0.2f)
            {
                tooFar = true;
                break;
            }
        }

        myLast = next;

        Expect(!tooFar, "Too far");

        myMesh.ClearSurfaces();
        bool result = myMesh.AddSurfaceFromVerticies(next);

        Expect(result == (myValue > Far && myValue <= Cor), "Existance missmatch");
    }
}

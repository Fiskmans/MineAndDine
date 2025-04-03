using Godot;
using MineAndDine;
using System;

public partial class ArchimedianScrew : Node3D
{
    [Export]
    public int myRange = 10;

    Interactable myHandle;
    Node3D myDial;
    Node3D myScrew;
    Node3D myScrewRoot;

    float myTension = 0.0f;

    public override void _Ready()
    {
        base._Ready();

        myHandle = GetNode<Interactable>("Handle");
        myDial = GetNode<Node3D>("Handle/Dial");
        myScrewRoot = GetNode<Node3D>("ScrewRoot");
        myScrew = GetNode<Node3D>("ScrewRoot/Screw");

        myHandle.OnActivate += MyHandle_OnActivate;
    }

    private void MyHandle_OnActivate(Interactable aSender)
    {
        myTension += 1.0f;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (myTension > 0.01f)
        {
            float next = myTension / (float)Mathf.Pow(Mathf.E, delta / 5);
            Spin(myTension - next);
            myTension = next;
            myDial.Rotation = new Vector3(0, myTension, 0);
        }
    }

    public void Spin(float aAmount)
    {
        myScrew.RotateY(aAmount * 16);

        Chunk.NodeIndex at = Chunk.NodeAt(myScrewRoot.GlobalPosition);

        if (at.InBounds())
        {
            for (int i = 0; i < myRange; i++)
            {
                Chunk.NodeIndex next = at.Offset(Vector3I.Forward + Vector3I.Down);
                if (!next.InBounds())
                {
                    break;
                }

                if (MaterialInteractions.Move(MaterialGroups.Loose, ref next.Get(), ref at.Get(), Chunk.NodeVolume))
                {
                    Terrain.ourInstance.RegisterModification(next.chunk);
                    Terrain.ourInstance.RegisterModification(at.chunk);
                }

                at = next;
            }
        }
    }
}

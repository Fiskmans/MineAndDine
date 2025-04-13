using Godot;
using Godot.Collections;
using MineAndDine;
using System;

namespace MineandDine
{
    internal partial class Placable : PickupObject
    {
        [Export]
        PackedScene myGhost;
        [Export]
        PackedScene myPlaced;

        Node3D myPlacingGhost;

        public override void _Ready()
        {
            base._Ready();
            if (myGhost == null) { throw new Exception("Placeable without a ghost"); }
            if (myPlaced == null) { throw new Exception("Placeable without a result"); }

        }

        public override void _Process(double aDelta)
        {
            if (myPlacingGhost != null)
            {
                if (myHeldByPlayer == null)
                {
                    GetTree().Root.RemoveChild(myPlacingGhost);
                    myPlacingGhost = null;
                }
                else
                {
                    Dictionary collision = myHeldByPlayer.DoRayCast(1);
                    if (collision.Count == 0) { return; }

                    myPlacingGhost.Position = collision["position"].AsVector3().Snapped(Vector3.One);
                }
            }
        }

        public override void Use()
        {
            Node root = GetTree().Root;

            if (myPlacingGhost == null)
            {
                myPlacingGhost = myGhost.Instantiate() as Node3D;
                root.AddChild(myPlacingGhost);
                return;
            }

            {

                Node3D placed = myPlaced.Instantiate() as Node3D;
                placed.Transform = myPlacingGhost.Transform;
                root.AddChild(placed);

                root.RemoveChild(myPlacingGhost);
                myPlacingGhost = null;
            }
        }

    }
}

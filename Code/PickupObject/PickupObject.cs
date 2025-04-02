using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{
    public partial class PickupObject : RigidBody3D
    {
        [Export]
        public string myName { get; private set; }
        protected PlayerController myHeldByPlayer;
        protected Mesh myMesh;

        public override void _Ready() 
        {
            CollisionLayer = 2;
        }

        public void PickUp(PlayerController aPlayer)
        {
            GetParent()?.RemoveChild(this);

            aPlayer.Hold(this);

            myHeldByPlayer = aPlayer;

            Freeze = true;
            GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
            GlobalPosition = myHeldByPlayer.myHand.GlobalPosition;
        }

        public void Drop()
        {
            if (myHeldByPlayer == null)
            {
                return;
            }

            Vector3 pos = GlobalPosition;

            Node root = GetTree().Root;

            GetParent()?.RemoveChild(this);
            root.AddChild(this);
            
            GlobalPosition = pos;
            Freeze = false;
            GetNode<CollisionShape3D>("CollisionShape3D").Disabled = false;

            myHeldByPlayer = null;
        }
    }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{
    internal partial class PickupObject : RigidBody3D
    {
        protected PlayerController myHeldByPlayer;

        public override void _Ready()
        {
            CollisionLayer = 2;
        }

        public void PickUp(PlayerController aPlayer)
        {
            if(GetParent() != null)
            {
                GetParent().RemoveChild(this);
            }

            aPlayer.myHand.AddChild(this);
            myHeldByPlayer = aPlayer;

            Freeze = true;
            GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
            GlobalPosition = myHeldByPlayer.myHand.GlobalPosition;
        }

        public void Drop()
        {
            if (myHeldByPlayer != null)
            {
                Vector3 pos = GlobalPosition;

                myHeldByPlayer.myHand.RemoveChild(this);
                myHeldByPlayer.AddSibling(this);

                GlobalPosition = pos;
                Freeze = false;
                GetNode<CollisionShape3D>("CollisionShape3D").Disabled = false;

                myHeldByPlayer = null;
            }
        }
    }
}

using Godot;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MineAndDine.Code.Extensions;

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

        public virtual void Use()
        {

        }

        public override void _Process(double aDelta)
        {
            if (GlobalPosition.Y < -1000) // Out of bounds
            {
                GD.Print($"{this} has gone out of bounds");

                GlobalPosition = Vector3.Zero;
                LinearVelocity = Vector3.Zero;
                AngularVelocity = Vector3.Zero;
            }
        }

        public void PickUp(PlayerController aPlayer)
        {
            GetParent()?.RemoveChild(this);

            aPlayer.Hold(this);

            myHeldByPlayer = aPlayer;

            Freeze = true;
            this.RemoveLayer(Code.Constants.CollisionLayer.Collision | Code.Constants.CollisionLayer.Interaction);
            GlobalPosition = myHeldByPlayer.myHand.GlobalPosition;
            Rotation = Vector3.Zero;
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

            this.AddLayer(Code.Constants.CollisionLayer.Collision | Code.Constants.CollisionLayer.Interaction);

            myHeldByPlayer = null;
        }
    }
}

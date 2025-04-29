using Godot;
using Godot.Collections;
using MineAndDine.Code.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{
    internal partial class Tool : PickupObject
    {
        public Container myContainer = null;

        public override void _Ready()
        {
            myContainer = (FindChild("Content") as Container) ?? new Container { myCapacity = 1 };
        }

        public override void Use()
        {
            if (!IsInsideTree())
            {
                throw new Exception("Tool used while not in the world");
            }

            Dictionary intersection = myHeldByPlayer.DoRayCast(2);

            if (intersection.Count == 0)
            {
                return;
            }

            GD.Print("Used: ", this);

            Variant collider;
            if (!intersection.TryGetValue("collider", out collider))
            {
                return;
            }

            GodotObject obj = collider.AsGodotObject();

            GD.Print("  On: ", obj);

            if (obj is Node3D)
            {
                InteractWith(obj as Node3D, intersection["position"].AsVector3());
            }
        }

        public virtual bool InteractWith(Node3D aNode, Vector3 aAt)
        {

            bool interacted = false;

            if (myContainer.Empty)
            {
                interacted |= (aNode as Tool)?.myContainer.TakeFrom(myContainer, MaterialGroups.All) ?? false;
                interacted |= (aNode as Container)?.TakeFrom(myContainer, MaterialGroups.All) ?? false;
            }
            else
            {
                interacted |= (aNode as Tool)?.myContainer.GiveTo(myContainer, MaterialGroups.All) ?? false;
                interacted |= (aNode as Container)?.GiveTo(myContainer, MaterialGroups.All) ?? false;
            }

            return interacted;
        }
        public void DepositAt(Vector3 aPosition)
        {
            Chunk.NodeIndex node = Chunk.NodeAt(aPosition);

            for (int i = 0; i < 7; i++)
            {
                if (!node.InBounds())
                {
                    break;
                }

                if (myContainer.Empty)
                {
                    break;
                }

                if (myContainer.GiveTo(ref node.Get(), MaterialGroups.All))
                {
                    Terrain.ourInstance.RegisterModification(node.chunk);
                }

                node = node.Offset(Vector3I.Up);
            }
        }
    }
}

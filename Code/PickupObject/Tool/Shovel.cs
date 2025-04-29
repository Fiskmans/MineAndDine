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
    internal partial class Shovel : Tool
    {
        [Export]
        public int myCapacity { get; private set; }
        [Export]
        public float myMiningRadius { get; private set; } = 5;
        [Export]
        public float myMiningPower { get; private set; } = 1;

        public override string ToString()
        {
            return $"Shovel {myContainer}";
        }

        public override bool InteractWith(Node3D aNode, Vector3 aAt)
        {
            if (base.InteractWith(aNode, aAt))
            {
                return true;
            }

            if (myContainer.Empty)
            {
                DigAt(aAt);
            }
            else
            {
                DepositAt(aAt);
            }

            return true;
        }

        public void DigAt(Vector3 aPosition)
        {

            Chunk.NodeIndex node = Chunk.NodeAt(aPosition);

            if (!node.InBounds())
            {
                return;
            }

            myContainer.TakeFrom(ref node.Get(), MaterialGroups.Loose);
            Terrain.ourInstance.RegisterModification(node.chunk);

            foreach (Vector3I offset in Utils.CardinalDirections)
            {
                Chunk.NodeIndex next = node.Offset(offset);

                if (!next.InBounds())
                {
                    continue;
                }

                myContainer.TakeFrom(ref next.Get(), MaterialGroups.Loose);
                Terrain.ourInstance.RegisterModification(next.chunk);
            }
        }
    }
}

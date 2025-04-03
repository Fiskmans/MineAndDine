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

        public override void Use()
        {
            ArgumentNullException.ThrowIfNull(myHeldByPlayer);

            GD.Print(MaterialInteractions.Total(ref myContent));

            Dictionary intersection = myHeldByPlayer.DoRayCast(2);

            if (intersection.Count == 0)
            {
                return;
            }

            Variant collider;
            if (intersection.TryGetValue("collider", out collider) && collider.AsGodotObject() is Tool)
            {
                GD.Print(collider);

                (collider.AsGodotObject() as Bucket)?.Fill(ref myContent);
            }
            else if (MaterialInteractions.Total(ref myContent) > 0.0f)
            {
                Deposit(intersection);
            }
            else
            {
                Dig(intersection);
            }
        }

        public void Dig(Dictionary anIntersection)
        {
            if (anIntersection.Count == 0)
            {
                return;
            }

            Vector3 pos = ((Vector3)anIntersection["position"]);

            Aabb area = new Aabb(pos - new Vector3(myMiningRadius, myMiningRadius, myMiningRadius), new Vector3(myMiningRadius, myMiningRadius, myMiningRadius) * 2);

            Terrain.ourInstance.Touch(area);

            foreach (Chunk chunk in Terrain.ourInstance.AffectedChunks(area))
            {
                foreach (Vector3I nodePos in chunk.AffectedNodes(area))
                {
                    float dist = pos.DistanceTo(chunk.WorldPosFromNodePos(nodePos));

                    if (dist >= myMiningRadius)
                    {
                        continue;
                    }

                    Chunk.NodeIndex node = chunk.NodeAt(nodePos);

                    MaterialInteractions.Move(MaterialGroups.Loose, ref node.Get(), ref myContent, myCapacity);
                }

                chunk.Update();
                Terrain.ourInstance.RegisterModification(chunk);
            }
        }

        private void Deposit(Dictionary anIntersection)
        {
            if (anIntersection.Count == 0)
            {
                return;
            }

            Vector3 pos = ((Vector3)anIntersection["position"]);
            Chunk.NodeIndex node = Chunk.NodeAt(pos);

            for (int i = 0; i < 7; i++)
            {
                if (!node.InBounds())
                {
                    break;
                }

                if (MaterialInteractions.Total(ref myContent) == 0)
                {
                    break;
                }

                MaterialInteractions.Move(MaterialGroups.Loose, ref myContent, ref node.Get(), Chunk.NodeCapacity);
                Terrain.ourInstance.RegisterModification(node.chunk);

                node = node.Offset(Vector3I.Up);

            }

            GD.Print("After deposit: ", MaterialInteractions.Total(ref myContent));
        }
    }
}

using Godot;
using Godot.Collections;
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
        public float myCapacity;
        [Export]
        public float myMiningRadius { get; set; } = 5;
        [Export]
        public float myMiningPower { get; set; } = 1;

        MaterialsList myContent;

        public override void Use()
        {
            ArgumentNullException.ThrowIfNull(myHeldByPlayer);

            if (MaterialInteractions.Total(ref myContent) > 0.0f )
            {
                Deposit(myHeldByPlayer.DoRayCast(2));
            }
            else
            {
                Dig(myHeldByPlayer.DoRayCast(2));
            }
        }

        public void Dig(Dictionary anIntersection)
        {
            if (anIntersection.Count == 0)
                return;

            Vector3 pos = ((Vector3)anIntersection["position"]);

            Aabb area = new Aabb(pos - new Vector3(myMiningRadius, myMiningRadius, myMiningRadius), new Vector3(myMiningRadius, myMiningRadius, myMiningRadius) * 2);

            Terrain.ourInstance.Touch(area);

            foreach (Chunk chunk in Terrain.ourInstance.AffectedChunks(area))
            {
                foreach (Vector3I nodePos in chunk.AffectedNodes(area))
                {
                    float dist = pos.DistanceTo(chunk.WorldPosFromVoxelPos(nodePos));

                    if (dist >= myMiningRadius)
                        continue;

                    Chunk.NodeIndex node = chunk.NodeAt(nodePos);

                    float amount = Mathf.Min(myMiningPower * (1.0f - dist / myMiningRadius), node.Get()[(int)MaterialType.Dirt]);

                    node.Get()[(int)MaterialType.Dirt] -= amount;
                }

                chunk.Update();
                Terrain.ourInstance.RegisterModification(chunk);
            }
        }

        private void Deposit(Dictionary anIntersection)
        {
            if (anIntersection.Count == 0)
                return;

            Vector3 pos = ((Vector3)anIntersection["position"]);

            Chunk chunk = Terrain.ourInstance.ChunkAt(Chunk.IndexFromPos(pos));
            Chunk.NodeIndex node = chunk.NodeAt(chunk.NodePosFromWorldPos(pos));

            if (node.InBounds())
                MaterialInteractions.MoveLoose(ref myContent, ref node.Get(), 1.0f);

            Terrain.ourInstance.RegisterModification(chunk);
        }
    }
}

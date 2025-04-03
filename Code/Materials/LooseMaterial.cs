using Godot;
using MineAndDine.Code.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Materials
{
    public class LooseMaterial
    {
        const float myMinimumMoveAmount = 0.01f;

        MaterialType myType;

        float myDeltaHeigthBelow;
        float myDeltaHeightBelowSide;
        float myDeltaHeightSide;

        public LooseMaterial(MaterialType aType, float aDeltaBelow, float aDeltaBelowSide, float aDeltaSide)
        {
            myType = aType;

            myDeltaHeigthBelow = aDeltaBelow;
            myDeltaHeightBelowSide = aDeltaBelowSide;
            myDeltaHeightSide = aDeltaSide;
        }

        public float SimulateOn(Chunk.NodeIndex aNode, HashSet<Chunk> aInOutModifiedChunks)
        {
            if (!aNode.InBounds())
            {
                return 0;
            }

            float total = 0.0f;

            total += Flow(aNode, aNode.Offset(Vector3I.Down), myDeltaHeigthBelow, aInOutModifiedChunks);

            total += Flow(aNode, aNode.Offset(new Vector3I( 1,-1, 0)), myDeltaHeightBelowSide, aInOutModifiedChunks);
            total += Flow(aNode, aNode.Offset(new Vector3I(-1,-1, 0)), myDeltaHeightBelowSide, aInOutModifiedChunks);
            total += Flow(aNode, aNode.Offset(new Vector3I( 0,-1, 1)), myDeltaHeightBelowSide, aInOutModifiedChunks);
            total += Flow(aNode, aNode.Offset(new Vector3I( 0,-1, 1)), myDeltaHeightBelowSide, aInOutModifiedChunks);

            total += Flow(aNode, aNode.Offset(new Vector3I( 1, 0, 0)), myDeltaHeightSide, aInOutModifiedChunks);
            total += Flow(aNode, aNode.Offset(new Vector3I(-1, 0, 0)), myDeltaHeightSide, aInOutModifiedChunks);
            total += Flow(aNode, aNode.Offset(new Vector3I( 0, 0, 1)), myDeltaHeightSide, aInOutModifiedChunks);
            total += Flow(aNode, aNode.Offset(new Vector3I( 0, 0, 1)), myDeltaHeightSide, aInOutModifiedChunks);

            // Wake up chunks that could possibly flow into this node
            if (total >= myMinimumMoveAmount)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Chunk.NodeIndex above = aNode.Offset(new Vector3I(x, 1, z));

                        if (above.InBounds())
                        {
                            aInOutModifiedChunks.Add(above.chunk);
                        }
                    }
                }
            }

            return total;
        }

        private float Flow(Chunk.NodeIndex aFrom, Chunk.NodeIndex aTo, float aTargetDelta, HashSet<Chunk> modifiedChunks)
        {
            if (!aTo.InBounds())
            {
                return 0;
            }

            float available = aFrom[myType];
            if (available < myMinimumMoveAmount)
            {
                return 0;
            }

            float space = Chunk.NodeVolume - MaterialInteractions.Total(ref aTo.Get());

            if (space < myMinimumMoveAmount)
            {
                return 0;
            }

            float delta = aFrom[myType] - aTo[myType];

            if (delta < aTargetDelta)
            {
                return 0;
            }

            float deltaToBalance = (delta - aTargetDelta) / 2.0f;

            float amount = Mathf.Min(Mathf.Min(deltaToBalance, available), space);

            if (amount < myMinimumMoveAmount)
            {
                return 0;
            }

            modifiedChunks.Add(aFrom.chunk);
            modifiedChunks.Add(aTo.chunk);

            aFrom[myType] -= amount;
            aTo[myType] += amount;

            return amount;
        }
    }
}

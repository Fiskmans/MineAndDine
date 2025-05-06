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
        const byte myMinimumMoveAmount = 1;

        MaterialType myType;

        int myDeltaHeigthBelow;
        int myDeltaHeightBelowSide;
        int myDeltaHeightSide;

        public LooseMaterial(MaterialType aType, int aDeltaBelow, int aDeltaBelowSide, int aDeltaSide)
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

        private float Flow(Chunk.NodeIndex aFrom, Chunk.NodeIndex aTo, int aTargetDelta, HashSet<Chunk> modifiedChunks)
        {
            if (!aTo.InBounds())
            {
                return 0;
            }

            int delta = aFrom[myType] - aTo[myType];

            int deltaToBalance = (delta - aTargetDelta) / 2;

            int amount = Utils.Min(deltaToBalance, aFrom[myType], Chunk.SelfCompactingLimit - MaterialInteractions.Total(ref aTo.Get()));

            if (amount < myMinimumMoveAmount)
            {
                return 0;
            }

            modifiedChunks.Add(aFrom.chunk);
            modifiedChunks.Add(aTo.chunk);

            aFrom[myType] -= (byte)amount;
            aTo[myType] += (byte)amount;

            return amount;
        }
    }
}

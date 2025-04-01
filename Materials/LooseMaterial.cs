using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Materials
{
    public class LooseMaterial
    {
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

        public void SimulateOn(Chunk.NodeIndex aNode, HashSet<Chunk> aInOutModifiedChunks)
        {
            if (!aNode.InBounds())
            {
                return;
            }

            Flow(aNode, aNode.Offset(Vector3I.Down), myDeltaHeigthBelow, aInOutModifiedChunks);

            Flow(aNode, aNode.Offset(new Vector3I( 1,-1, 0)), myDeltaHeightBelowSide, aInOutModifiedChunks);
            Flow(aNode, aNode.Offset(new Vector3I(-1,-1, 0)), myDeltaHeightBelowSide, aInOutModifiedChunks);
            Flow(aNode, aNode.Offset(new Vector3I( 0,-1, 1)), myDeltaHeightBelowSide, aInOutModifiedChunks);
            Flow(aNode, aNode.Offset(new Vector3I( 0,-1, 1)), myDeltaHeightBelowSide, aInOutModifiedChunks);

            Flow(aNode, aNode.Offset(new Vector3I( 1, 0, 0)), myDeltaHeightSide, aInOutModifiedChunks);
            Flow(aNode, aNode.Offset(new Vector3I(-1, 0, 0)), myDeltaHeightSide, aInOutModifiedChunks);
            Flow(aNode, aNode.Offset(new Vector3I( 0, 0, 1)), myDeltaHeightSide, aInOutModifiedChunks);
            Flow(aNode, aNode.Offset(new Vector3I( 0, 0, 1)), myDeltaHeightSide, aInOutModifiedChunks);
        }

        private void Flow(Chunk.NodeIndex aFrom, Chunk.NodeIndex aTo, float aTargetDelta, HashSet<Chunk> modifiedChunks)
        {
            if (!aTo.InBounds())
            {
                return;
            }

            float available = aFrom[myType];
            if (available <= float.Epsilon * available * 100)
            {
                return;
            }

            float space = Chunk.NodeVolume - MaterialInteractions.Total(ref aTo.Get());

            if (space <= float.Epsilon * space * 100)
            {
                return;
            }

            float delta = Mathf.Min(aFrom[myType] - aTo[myType], space);

            if (delta <= aTargetDelta + float.Epsilon * delta * 100)
            {
                return;
            }

            modifiedChunks.Add(aFrom.chunk);
            modifiedChunks.Add(aTo.chunk);

            float amount = Mathf.Max((delta - aTargetDelta) / 2.0f, available);

            aFrom[myType] -= amount;
            aTo[myType] += amount;
        }
    }
}

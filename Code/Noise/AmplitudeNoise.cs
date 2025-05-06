using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MineAndDine.Noise
{
    internal class AmplitudeNoise : Noise
    {
        Noise myNoise;
        float myScale;

        public AmplitudeNoise(Noise aInternal, float aScale)
        {
            myNoise = aInternal;
            myScale = aScale;
        }

        public override float Generate(Godot.Vector3 aPosition)
        {
            return myNoise.Generate(aPosition) * myScale;
        }
    }
}

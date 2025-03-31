using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Noise
{
    public class PerlinNoise : Noise
    {
        FastNoiseLite myInternal;

        float myScale = 1.0f;

        public PerlinNoise() 
        {
            myInternal = new FastNoiseLite();
            myInternal.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        }

        public PerlinNoise Scale(float aValue)
        {
            myScale = aValue;
            return this;
        }

        public override float Generate(Vector3 aPosition)
        {
            return myInternal.GetNoise3D(aPosition.X * myScale, aPosition.Y * myScale, aPosition.Z * myScale) / 2.0f + 0.5f;
        }
    }
}

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

        float myFrequency = 1.0f;
        Vector3 axles = Vector3.One;

        public PerlinNoise() 
        {
            myInternal = new FastNoiseLite();
            myInternal.Seed = (int)GD.Randi();
            myInternal.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        }

        public PerlinNoise Frequency(float aFrequency)
        {
            myFrequency = aFrequency;
            return this;
        }

        public PerlinNoise Axis(Vector3I aAxles)
        {
            axles = aAxles.Max(Vector3I.Zero).Min(Vector3I.One);
            return this;
        }

        public override float Generate(Vector3 aPosition)
        {
            Vector3 final = aPosition * myFrequency * axles;

            return (myInternal.GetNoise3D(final.X, final.Y, final.Z) / 2.0f + 0.5f);
        }
    }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Noise
{
    public abstract class Noise
    {
        public delegate float NoiseFunction(Vector3 aPosition);

        public abstract float Generate(Vector3 aPosition);

        public Noise Combine(Noise aOther, CompoundNoise.Mode aMode)
        {
            return new CompoundNoise(this, aOther, aMode);
        }
        public Noise Combine(Func<Vector3, float> aFunc, CompoundNoise.Mode aMode)
        {
            return new CompoundNoise(this, new LambdaNoise(aFunc), aMode);
        }
    }
}

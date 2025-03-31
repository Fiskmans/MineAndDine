using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Noise
{
    public class CompoundNoise : Noise
    {
        public enum Mode
        {
            Multiply,
            Max,
            Add,
            Subtract
        }

        Noise myLeft;
        Noise myRight;
        Mode myMode;

        public CompoundNoise(Noise aLeft, Noise aRight, Mode aMode) 
        {
            myLeft = aLeft;
            myRight = aRight;
            myMode = aMode;
        }

        public override float Generate(Godot.Vector3 aPosition)
        {
            float left = myLeft.Generate(aPosition);
            float right = myRight.Generate(aPosition);

            switch (myMode)
            {
                case Mode.Multiply:
                    return left * right;
                case Mode.Max:
                    return MathF.Max(left, right);
                case Mode.Add:
                    return left + right;
                case Mode.Subtract:
                    return left - right;
            }

            throw new NotImplementedException();
        }
    }
}

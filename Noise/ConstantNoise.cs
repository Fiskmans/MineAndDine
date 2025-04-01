using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Noise
{
    public class ConstantNoise : Noise
    {
        float myValue;

        public ConstantNoise(float aValue) 
        {
            myValue = aValue;
        }

        public override float Generate(Godot.Vector3 aPosition)
        {
            return myValue;
        }
    }
}

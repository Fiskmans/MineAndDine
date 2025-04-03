using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Noise
{
    public class LambdaNoise : Noise
    {
        readonly Func<Vector3, float> myFunction;
        public LambdaNoise(Func<Vector3, float> aFunction)
        {
            myFunction = aFunction;
        }

        public override float Generate(Vector3 aPosition)
        {
            return myFunction(aPosition);
        }
    }
}

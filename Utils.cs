using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{
    internal class Utils
    {
        public static IEnumerable<Vector3I> Every(Vector3I aFrom, Vector3I aTo)
        {
            for (int x = aFrom.X; x <= aTo.X; x++)
            {
                for (int y = aFrom.Y; y <= aTo.Y; y++)
                {
                    for (int z = aFrom.Z; z <= aTo.Z; z++)
                    {
                        yield return new Vector3I(x, y, z);
                    }
                }
            }
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float weight)
        {
            return new Vector3(
                    Mathf.Lerp(a.X,b.X,weight),
                    Mathf.Lerp(a.Y,b.Y, weight),
                    Mathf.Lerp(a.Z,b.Z,weight));
        }
    }
}

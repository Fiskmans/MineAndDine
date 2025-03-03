using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    }
}

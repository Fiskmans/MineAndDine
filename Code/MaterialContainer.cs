using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace MineAndDine
{

    public enum MaterialType : int
    {
        Dirt,
        Coal,
        CoalDust,

        Count
    }

    public class MaterialGroups
    {
        public static readonly int[] All = Enumerable.Range(0, ((int)MaterialType.Count)).ToArray();
        public static readonly int[] Loose = [(int)MaterialType.Dirt, (int)MaterialType.CoalDust];

        public struct ColorMapping
        {
            public int Index;
            public Vector3 Color;
        }

        public static readonly ColorMapping[] Colored = [
                new ColorMapping{ Index = (int)MaterialType.Dirt,       Color = new Vector3(0.8f, 0.6f, 0.2f) },
                new ColorMapping{ Index = (int)MaterialType.Coal,       Color = new Vector3(0.2f, 0.2f, 0.3f) },
                new ColorMapping{ Index = (int)MaterialType.CoalDust,   Color = new Vector3(0.4f, 0.4f, 0.5f) }
            ];
    }

    [InlineArray((int)MaterialType.Count)]
    public struct MaterialsList
    {
        private float InternalStorage;
    }

    public class MaterialInteractions
    {
        public const float epsilon = 0.001f;

        public static bool MoveLoose(ref MaterialsList aFrom, ref MaterialsList aTo, float aVolume)
        {
            float available = 0;

            foreach (int mat in MaterialGroups.Loose)
            {
                available += aFrom[mat];
            }

            if (available < epsilon)
            {
                return false;
            }


            float spaceAvailable = aVolume - Total(ref aTo);

            float fraction = spaceAvailable / available;

            if (fraction < epsilon)
            {
                return false;
            }

            if (fraction >= 1.0f)
            {
                foreach (int mat in MaterialGroups.Loose)
                {
                    aTo[mat] += aFrom[mat];
                    aFrom[mat] = 0;

                }
            }
            else
            {
                float fractionLeft = 1.0f - fraction;

                foreach (int mat in MaterialGroups.Loose)
                {
                    aTo[mat] += aFrom[mat] * fraction;
                    aFrom[mat] *= fractionLeft;
                }
            }

            return true;
        }

        public static bool Solid(ref MaterialsList aList, float aSurfaceValue)
        {
            return Total(ref aList) > aSurfaceValue;
        }

        public static float Total(ref MaterialsList aList)
        {
            float sum = 0;

            if (Unsafe.IsNullRef(ref aList))
            {
                return sum;
            }

            foreach (int mat in MaterialGroups.All)
            {
                sum += aList[mat];
            }

            return sum;
        }
    }
}

global using MaterialsList = MineAndDine.Code.Materials.MaterialsArray<float>;

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace MineAndDine.Code.Materials
{
    public enum MaterialType : int
    {
        Dirt,
        Coal,
        CoalDust,
        Gold,

        Count
    }

    [InlineArray((int)MaterialType.Count)]
    public struct MaterialsArray<T>
    {
        private T InternalStorage;
        public T this[MaterialType aMaterial]
        {
            get
            {
                return this[(int)aMaterial];
            }
            set
            {
                this[(int)aMaterial] = value;
            }
        }
    }

    public static class MaterialExtensions
    {
        public delegate void MaterialHandler<T>(MaterialType aType, float aAmount, T aObject);
        public delegate float MaterialModificationHandler<T>(MaterialType aType, float aAmount, T aObject);

        public static void Foreach<T>(this ref MaterialsList aMaterials, MaterialsArray<T> aGroup, MaterialHandler<T> aHandler)
        {
            foreach (MaterialType type in MaterialGroups.Indexes(aGroup))
            {
                aHandler(type, aMaterials[type], aGroup[type]);
            }
        }
        public static void ForeachSet<T>(this ref MaterialsList aMaterials, MaterialsArray<T> aGroup, MaterialModificationHandler<T> aHandler)
        {
            foreach (MaterialType type in MaterialGroups.Indexes(aGroup))
            {
                aMaterials[type] = aHandler(type, aMaterials[type], aGroup[type]);
            }
        }
    }

    public class MaterialInteractions
    {
        public const float epsilon = 0.001f;

        public static bool Move<T>(MaterialsArray<T> aGroup, ref MaterialsList aFrom, ref MaterialsList aTo, float aVolume)
        {
            float available = 0;

            aFrom.Foreach(aGroup, (type, value, angleOfCollapse) =>
            {
                available += value;
            });

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
                foreach (MaterialType type in MaterialGroups.Indexes(aGroup))
                {
                    aTo[type] += aFrom[type];
                    aFrom[type] = 0;
                }
            }
            else
            {
                float fractionLeft = 1.0f - fraction;

                foreach (MaterialType type in MaterialGroups.Indexes(aGroup))
                {
                    aTo[type] += aFrom[type] * fraction;
                    aFrom[type] *= fractionLeft;
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

            aList.Foreach(MaterialGroups.Color, (type, amount, color) =>
            {
                sum += amount;
            });

            return sum;
        }

        public static Vector3 Color(ref MaterialsList aList)
        {
            float sum = 0;
            Vector3 blend = Vector3.Zero;

            if (Unsafe.IsNullRef(ref aList))
            {
                return blend;
            }

            aList.Foreach(MaterialGroups.Color, (type, amount, color) =>
            {
                sum += amount;
                blend += color * amount;
            });

            return blend / sum;
        }
    }
}

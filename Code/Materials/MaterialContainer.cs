
global using MaterialsList = MineAndDine.Code.Materials.MaterialsArray<byte>;

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
        public delegate void MaterialHandler<T>(MaterialType aType, byte aAmount, T aObject);
        public delegate byte MaterialModificationHandler<T>(MaterialType aType, byte aAmount, T aObject);

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
        // TODO: this can probably be changed to allow different base-types to source and target so big, one of containers can use int, while nodes uses bytes
        public static bool Move<T>(MaterialsArray<T> aGroup, ref MaterialsList aFrom, ref MaterialsList aTo, int aCapacity)
        {
            byte available = 0;

            aFrom.Foreach(aGroup, (type, value, angleOfCollapse) =>
            {
                available += value;
            });

            if (available == 0)
            {
                return false;
            }


            float space = aCapacity - Total(ref aTo);

            if (space == 0)
            {
                return false;
            }

            if (available <= space)
            {
                foreach (MaterialType type in MaterialGroups.Indexes(aGroup))
                {
                    aTo[type] += aFrom[type];
                    aFrom[type] = 0;
                }
            }
            else
            {

                float fraction = (float)space / (float)available;
                float fractionLeft = 1.0f - fraction;

                // TODO: This leaves a slight amount of space left in the target, because of the rounding down, The final space should be filled by weighted random
                foreach (MaterialType type in MaterialGroups.Indexes(aGroup))
                {
                    byte amount = (byte)(aFrom[type] * fraction);

                    aTo[type] += amount;
                    aFrom[type] -= amount;
                }
            }

            return true;
        }

        public static bool Solid(ref MaterialsList aList, float aSurfaceValue)
        {
            return Total(ref aList) > aSurfaceValue;
        }

        public static byte Total(ref MaterialsList aList)
        {
            byte sum = 0;

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

        public static Color Color(ref MaterialsList aList)
        {
            float sum = 0;
            Color blend = new Color();

            blend.R = 0;
            blend.G = 0;
            blend.B = 0;
            blend.A = 0;

            aList.Foreach(MaterialGroups.Color, (type, amount, color) =>
            {
                sum += amount;
                blend += color * amount;
            });

            return blend / sum;
        }
    }
}


global using MaterialsList = MineAndDine.Code.Materials.MaterialsArray<byte>;

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Globalization;

namespace MineAndDine.Code.Materials
{
    public enum MaterialType : int
    {
        Dirt,
        Stone,
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
        public delegate void MaterialHandler<T, U>(MaterialType aType, U aAmount, T aObject);
        public delegate U MaterialModificationHandler<T, U>(MaterialType aType, U aAmount, T aObject);

        public static void Foreach<T, U>(this ref MaterialsArray<U> aMaterials, MaterialsArray<T> aGroup, MaterialHandler<T, U> aHandler)
        {
            foreach (MaterialType type in MaterialGroups.Indexes(aGroup))
            {
                aHandler(type, aMaterials[type], aGroup[type]);
            }
        }
        public static void ForeachSet<T, U>(this ref MaterialsArray<U> aMaterials, MaterialsArray<T> aGroup, MaterialModificationHandler<T, U> aHandler)
        {
            foreach (MaterialType type in MaterialGroups.Indexes(aGroup))
            {
                aMaterials[type] = aHandler(type, aMaterials[type], aGroup[type]);
            }
        }
    }

    public class MaterialInteractions
    {
        public static bool Move<T, U, V>(MaterialsArray<T> aGroup, ref MaterialsArray<U> aFrom, ref MaterialsArray<V> aTo, V aCapacity) 
            where U : INumber<U>
            where V : INumber<V>
        {
            U availableU = default;

            aFrom.Foreach(aGroup, (type, value, angleOfCollapse) =>
            {
                availableU += value;
            });

            if (availableU == default)
            {
                return false;
            }

            V available = V.CreateChecked(availableU);
            V space = aCapacity - Total(ref aTo);

            if (space == default)
            {
                return false;
            }

            if (available > space)
            {
                float fraction = float.CreateChecked(space) / float.CreateChecked(available);
                float fractionLeft = 1.0f - fraction;

                bool movedAny = false;

                // TODO: This leaves a slight amount of space left in the target, because of the rounding down, The final space should be filled by weighted random
                foreach (MaterialType type in MaterialGroups.Indexes(aGroup))
                {
                    V amount = V.CreateChecked(float.CreateChecked(aFrom[type]) * fraction);

                    movedAny |= amount != default;

                    aTo[type] += amount;
                    aFrom[type] -= U.CreateChecked(amount);
                }

                return movedAny;
            }
            else
            {
                foreach (MaterialType type in MaterialGroups.Indexes(aGroup))
                {
                    aTo[type] = aTo[type] + V.CreateChecked(aFrom[type]);
                    aFrom[type] = default;
                }
                return true;
            }
        }

        public static bool Solid(ref MaterialsList aList, float aSurfaceValue)
        {
            return Total(ref aList) > aSurfaceValue;
        }

        public static T Total<T>(ref MaterialsArray<T> aList) where T : INumber<T>
        {
            T sum = default;

            aList.Foreach(MaterialGroups.All, (type, amount, placeholder) =>
            {
                sum += amount;
            });

            return sum;
        }

        public static Color Color<T>(ref MaterialsArray<T> aList) where T : INumber<T>
        {
            float sum = default;
            Color blend = new Color();

            blend.R = 0;
            blend.G = 0;
            blend.B = 0;
            blend.A = 0;

            aList.Foreach(MaterialGroups.Color, (type, amount, color) =>
            {
                float fAmount = float.CreateChecked(amount);
                sum += fAmount;
                blend += color * fAmount;
            });

            return blend / sum;
        }
    }
}

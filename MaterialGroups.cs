using Godot;
using Microsoft.Win32;
using MineAndDine.Noise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{

    public class MaterialGroups
    {
        public static MaterialsArray<object>                    All;
        public static MaterialsArray<object>                    Loose;
        public static MaterialsArray<Noise.Noise>               Generatable;
        public static MaterialsArray<Vector3>                   Color;
        public static MaterialsArray<(MaterialType, float)[]>   CrushResult;

        private class PlaceHolder { };

        public static void GenerateTables()
        {
            foreach (ref object val in All) 
            {
                val = new PlaceHolder(); // we wanna put literally anything here so the filters work
            }

            Loose[MaterialType.Dirt]        = new PlaceHolder();
            Loose[MaterialType.CoalDust]    = new PlaceHolder();

            Color[MaterialType.Dirt]        = new Vector3(0.8f, 0.6f, 0.2f);
            Color[MaterialType.Coal]        = new Vector3(0.2f, 0.2f, 0.3f);
            Color[MaterialType.CoalDust]    = new Vector3(0.4f, 0.4f, 0.5f);

            Noise.Noise Ground = new LambdaNoise((pos) => Mathf.Max(Utils.Sigmoid((-10-pos.Y) / 5.0f), 0));

            Generatable[MaterialType.Dirt] = Ground;

            Generatable[MaterialType.Coal] = new PerlinNoise().Scale(1.0f).Combine(Ground, CompoundNoise.Mode.Multiply);
        }

        public static IEnumerable<MaterialType> Indexes<T>(MaterialsArray<T> aGroup)
        {
            for (int i = 0; i < (int)MaterialType.Count; i++)
            {
                T val = aGroup[i];
                if (val == null)
                {
                    continue;
                }

                yield return (MaterialType)i;
            }
        }
    }
}

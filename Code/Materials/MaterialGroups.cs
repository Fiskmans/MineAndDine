using Godot;
using Microsoft.Win32;
using MineAndDine.Materials;
using MineAndDine.Noise;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Code.Materials
{

    public class MaterialGroups
    {
        private static bool myTablesAreGenerated = false;

        public static MaterialsArray<object> All;
        public static MaterialsArray<LooseMaterial> Loose;
        public static MaterialsArray<Noise.Noise> Generatable;
        public static MaterialsArray<Color> Color;
        public static MaterialsArray<(MaterialType, float)[]> CrushResult;

        private class PlaceHolder { };

        public static void GenerateTables()
        {
            if (myTablesAreGenerated)
            {
                return;
            }

            foreach (ref object val in All)
            {
                val = new PlaceHolder(); // we wanna put literally anything here so the filters work
            }

            Loose[MaterialType.Dirt] = new LooseMaterial(MaterialType.Dirt, -100, 10, 30);

            Color[MaterialType.Dirt] = new Color(0.8f, 0.6f, 0.2f);
            Color[MaterialType.Coal] = new Color(0.01f, 0.01f, 0.02f);
            Color[MaterialType.CoalDust] = new Color(0.4f, 0.4f, 0.5f);
            Color[MaterialType.Gold] = new Color(0.8f, 0.0f, 0.0f);

            Noise.Noise height = new PerlinNoise().Axles(new Vector3I(1, 0, 1)).Scale(1.0f).Height(50.0f);

            Noise.Noise ground = new LambdaNoise((pos) => Utils.Sigmoid(-(pos.Y + height.Generate(pos) - 20.0f) * 3.0f) * 160);

            Generatable[MaterialType.Dirt] = ground;

            Generatable[MaterialType.Coal] = new PerlinNoise().Height(60);
            //Generatable[MaterialType.Gold] = new PerlinNoise().Scale(10.0f).Height(0.1f)
            //                                    .Combine(ground, CompoundNoise.Mode.Multiply);

            myTablesAreGenerated = true;
        }

        public static IEnumerable<(MaterialType, T)> ForEach<T>(MaterialsArray<T> aGroup)
        {

            for (int i = 0; i < (int)MaterialType.Count; i++)
            {
                T val = aGroup[i];
                if (val == null)
                {
                    continue;
                }

                yield return ((MaterialType)i, val);
            }
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

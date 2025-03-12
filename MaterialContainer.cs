using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MineAndDine.Attributes;
using System.Numerics;

namespace MineAndDine
{
    public abstract class MaterialContainer
    {
        public const float epsilon = 0.001f;

        public float Space { get { return Mathf.Max(0, GetVolume() - Total); } }

        public float Total { get { return Materials<MaterialTypeAttribute>().Select(f => (float)f.GetValue(this)).Sum(); } }

        [LooseMaterial]
        [Colored(0.1f, 0.1f, 0.3f)]
        public float Dirt;

        [LooseMaterial]
        [Colored(0.1f, 0.1f, 0.3f)]
        public float CoalDust;

        [StaticMaterial]
        [CrushesInto("CoalDust")]
        [Colored(0.1f, 0.1f, 0.3f)]
        public float Coal;

        public abstract float GetVolume();

        public bool Solid(float aSurface = 0.5f)
        {
            return Total > aSurface;
        }

        public bool Empty()
        {
            return Total < epsilon;
        }

        public void Clamp()
        {
            foreach (FieldInfo material in Materials<MaterialTypeAttribute>())
            {
                material.SetValue(this, Mathf.Max(0, (float)material.GetValue(this)));
            }
        }

        override public string ToString()
        {
            return string.Join(";", Materials<MaterialTypeAttribute>()
                .Select(f => $"{f.Name}: {f.GetValue(this)}"));
        }

        public bool Take<T>(MaterialContainer aFrom) where T : MaterialTypeAttribute
        {
            FieldInfo[] materials = Materials<T>();

            float available = materials.Sum((field) => (float)field.GetValue(aFrom));

            if (available < epsilon)
                return false;

            float fraction = Space / available;

            if (fraction < epsilon)
                return false;

            if (fraction >= 1.0f)
            {
                foreach (FieldInfo field in materials)
                {
                    float sum = (float)field.GetValue(aFrom) + (float)field.GetValue(this);
                    field.SetValue(this, sum);
                    field.SetValue(aFrom, 0);
                }
            }
            else
            {
                foreach (FieldInfo field in materials)
                {
                    float amount = (float)field.GetValue(aFrom) * fraction;

                    field.SetValue(this, (float)field.GetValue(this) + amount);
                    field.SetValue(aFrom, Mathf.Max(0, (float)field.GetValue(aFrom) - amount));
                }
            }

            return false;

        }

        static public FieldInfo[] Materials<T>() where T : Attribute
        {
            return typeof(MaterialContainer).GetFields()
                .Where(field => field.GetCustomAttributes<T>().Count() > 0)
                .ToArray();
        }
    }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Attributes
{

    [AttributeUsage(AttributeTargets.Field)]
    internal class ColoredAttribute : Attribute
    {
        public Vector3 Color;

        public ColoredAttribute(float red = 0, float green = 0, float blue = 0)
        {
            Color = new Vector3(red, green, blue);
        }
    }
}

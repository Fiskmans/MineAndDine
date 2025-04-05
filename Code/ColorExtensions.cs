using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{
    public static class ColorExtensions
    {
        public static Godot.Vector3 RGB(this Color color)
        {
            return new Godot.Vector3(color.R, color.G, color.B);
        }
    }
}

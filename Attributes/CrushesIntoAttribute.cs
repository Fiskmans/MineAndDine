using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class CrushesIntoAttribute : Attribute
    {
        public string TargetName = null;
        public float Ratio = 1.0f;

        public CrushesIntoAttribute(string aTargetName)
        {
            TargetName = aTargetName;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{

    [AttributeUsage(AttributeTargets.Property)]
    internal class VoxelMaterialAttribute : Attribute
    {
        public bool Solid;
    }
}

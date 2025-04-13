using Godot;
using MineAndDine.Code.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{
    internal partial class Bucket : Tool
    {
        public override bool InteractWith(Node3D aNode, Vector3 aAt)
        {
            if (myContainer.Empty)
            {
                return false;
            }

            DepositAt(aAt);
            return true;
        }
    }
}

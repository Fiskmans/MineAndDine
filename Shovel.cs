using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{
    internal partial class Shovel : Tool
    {

        public override void Use()
        {
            if(myHeldByPlayer != null)
            {
                myHeldByPlayer.Mine(myHeldByPlayer.DoRayCast(2));
            }
        }
    }
}

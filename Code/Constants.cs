using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Code
{
    public class Constants
    {
        [Flags]
        public enum CollisionLayer : uint
        {
            Collision = 1u << 0,
            Interaction = 1u << 1
        }
    }
}

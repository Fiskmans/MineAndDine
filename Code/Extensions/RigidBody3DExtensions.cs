using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Code.Extensions
{
    public static class RigidBody3DExtensions
    {
        public static void AddLayer(this RigidBody3D aSelf, Constants.CollisionLayer aLayer)
        {
            aSelf.CollisionLayer |= (uint)aLayer;
        }
        public static void RemoveLayer(this RigidBody3D aSelf, Constants.CollisionLayer aLayer)
        {
            aSelf.CollisionLayer &= ~(uint)aLayer;
        }
    }
}

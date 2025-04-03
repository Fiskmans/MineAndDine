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
        [Export]
        public byte myMaxCapacity { get; private set; } = 100;

        public override void Use()
        {
            if (MaterialInteractions.Total(ref myContent) > 0.0f)
            {
                Empty();
            }
        }

        public void Fill(ref MaterialsList someMaterials)
        {
            MaterialInteractions.Move(MaterialGroups.All, ref someMaterials, ref myContent, myMaxCapacity);
            GD.Print("Bucket: ", MaterialInteractions.Total(ref myContent), " Shovel: ", MaterialInteractions.Total(ref someMaterials));
        }

        public void Empty()
        {
            Chunk.NodeIndex node = Chunk.NodeAt(GlobalPosition);
            if (!node.InBounds())
            {
                return;
            }

            MaterialInteractions.Move(MaterialGroups.All, ref myContent, ref node.Get(), Chunk.NodeCapacity);
            Terrain.ourInstance.RegisterModification(node.chunk);
        }
    }
}

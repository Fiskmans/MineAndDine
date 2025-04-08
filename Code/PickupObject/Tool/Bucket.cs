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

        [Export]
        private MeshInstance3D myFillMesh;
        [Export]
        private Node3D myFillStartTransform;
        [Export]
        private Node3D myFillEndTransform;

        public override void _Ready()
        {
            myFillMesh.Visible = false;
        }

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

            GD.Print("Bucket: ", MaterialInteractions.Total(ref myContent), " (max: ", myMaxCapacity, ") Shovel: ", MaterialInteractions.Total(ref someMaterials));

            byte currentAmount = MaterialInteractions.Total(ref myContent);

            if (currentAmount > 0)
            {
                myFillMesh.Visible = true;
                myFillMesh.Scale = myFillStartTransform.Scale.Lerp(myFillEndTransform.Scale, (float)(currentAmount) / (float)(myMaxCapacity));
                myFillMesh.Position = myFillStartTransform.Position.Lerp(myFillEndTransform.Position, (float)(currentAmount) / (float)(myMaxCapacity));
            }

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

            myFillMesh.Visible = false;
        }
    }
}

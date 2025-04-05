using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Code.Tests
{
    public partial class Test : Node3D
    {
        [Export]
        public string mydescription = "This is a test";

        private MeshInstance3D myLabel = new MeshInstance3D();
        private TextMesh myLabelText = new TextMesh();
        private StandardMaterial3D myLabelMaterial = new StandardMaterial3D { AlbedoColor = new Color(1, 1, 0.7f) };

        public override void _Ready()
        {
            base._Ready();
            myLabel.Mesh = myLabelText;
            myLabel.Position = Vector3.Up * 2;
            myLabelText.Material = myLabelMaterial;

            AddChild(myLabel);

            Setstatus("");
        }

        public void Setstatus(string aStatus)
        {
            myLabelText.Text = mydescription + "\n" + aStatus;
        }

        public void Expect(bool aValue)
        {
            myLabelMaterial.AlbedoColor = aValue
                ? new Color(0.7f, 1, 0.7f)
                : new Color(1, 0.7f, 0.7f);
        }
    }
}

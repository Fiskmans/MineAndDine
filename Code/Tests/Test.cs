using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Code.Tests
{
    public partial class Test : Node3D
    {
        [Export]
        public string myDescription = "This is a test";

        private MeshInstance3D myLabel = new MeshInstance3D();
        private TextMesh myLabelText = new TextMesh();
        private StandardMaterial3D myLabelMaterial = new StandardMaterial3D { AlbedoColor = new Color(1, 1, 1) };

        public enum ResultType
        {
            NotStarted,
            Running,
            Passed,
            Failed
        }

        public ResultType Result { get; private set; } = ResultType.NotStarted;

        public override void _Ready()
        {
            base._Ready();
            myLabel.Mesh = myLabelText;
            myLabel.Position = Vector3.Up;
            myLabelText.Material = myLabelMaterial;

            AddChild(myLabel);

            Setstatus("");

            Result = ResultType.Running;
        }

        public void Setstatus(string aStatus)
        {
            myLabelText.Text = myDescription + "\n" + aStatus;
        }


        public void Expect(bool aValue)
        {
            if (aValue)
            {
                return;
            }

            myLabelMaterial.AlbedoColor = new Color(1, 0.7f, 0.7f);
            Result = ResultType.Failed;
        }

        public void Passed()
        {
            myLabelMaterial.AlbedoColor = new Color(0.7f, 1, 0.7f);
            Result = ResultType.Passed;
        }
    }
}

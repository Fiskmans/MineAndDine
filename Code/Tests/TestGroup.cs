using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Code.Tests
{
    public partial class TestGroup : Node3D
    {
        protected void AddTest(Test aTest)
        {
            aTest.Position = Vector3.Forward * GetChildren().Count;
            AddChild(aTest);
        }
    }
}

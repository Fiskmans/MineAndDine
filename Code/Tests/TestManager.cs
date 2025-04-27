using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Code.Tests
{
    internal partial class TestManager : Node3D
    {

        public override void _Ready()
        {
            base._Ready();

            int i = 0;
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.GetCustomAttributes(typeof(TestAttribute), true).Length > 0)
                {
                    Node3D test = (Node3D)Activator.CreateInstance(type);

                    test.Position = Vector3.Right * i;

                    AddChild(test);

                    i++;
                }
            }

        }
    }
}

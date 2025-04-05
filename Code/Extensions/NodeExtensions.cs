using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine.Code.Extensions
{
    public static class NodeExtensions
    {
        public static IEnumerable<Node> AllChildren<T>(this Node self)
        {
            foreach (Node child in self.GetChildren())
            {
                if (child is T)
                {
                    yield return child;
                }

                foreach (Node grandChild in child.AllChildren<T>())
                {
                    yield return grandChild;
                }
            }
        }
    }
}

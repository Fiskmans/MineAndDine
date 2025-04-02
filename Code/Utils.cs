using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{
    internal class Utils
    {
        public static bool AreTerrainPrintsEnabled()
        {
            return false;
        }

        public static IEnumerable<Vector3I> Every(Vector3I aFrom, Vector3I aTo)
        {
            for (int x = aFrom.X; x <= aTo.X; x++)
            {
                for (int y = aFrom.Y; y <= aTo.Y; y++)
                {
                    for (int z = aFrom.Z; z <= aTo.Z; z++)
                    {
                        yield return new Vector3I(x, y, z);
                    }
                }
            }
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float weight)
        {
            return new Vector3(
                    Mathf.Lerp(a.X,b.X,weight),
                    Mathf.Lerp(a.Y,b.Y, weight),
                    Mathf.Lerp(a.Z,b.Z,weight));
        }

        public static Vector3I TruncatedDivision(Vector3I aVector, Vector3I aValues)
        {
            return new Vector3I(
                (aVector.X < 0 ? aVector.X - aValues.X + 1 : aVector.X) / aValues.X,
                (aVector.Y < 0 ? aVector.Y - aValues.Y + 1 : aVector.Y) / aValues.Y,
                (aVector.Z < 0 ? aVector.Z - aValues.Z + 1 : aVector.Z) / aValues.Z);
        }

        public static IEnumerable<(PropertyInfo, T)> AllPropertiesWithAttribute<T>(object aObject) where T : Attribute
        {
            foreach(PropertyInfo propInfo in aObject.GetType().GetProperties())
            {
                foreach (T attribute in propInfo.GetCustomAttributes<T>())
                {
                    yield return (propInfo, attribute);
                }
            }
        }

        public static float Sigmoid(float aValue)
        {
            return aValue / (1 + Mathf.Abs(aValue));
        }
    }
}

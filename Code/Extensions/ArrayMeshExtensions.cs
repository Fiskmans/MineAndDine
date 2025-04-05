using Godot;

namespace MineAndDine.Code.Extensions
{
    public static class ArrayMeshExtensions
    {
        public static bool AddSurfaceFromVerticies(this ArrayMesh self, Vector3[] aVerticies)
        {
            if (aVerticies.Length == 0)
            {
                return false;
            }

            Godot.Collections.Array arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = aVerticies;

            Vector3[] normals = new Vector3[aVerticies.Length];

            for (int i = 0; i < aVerticies.Length; i += 3)
            {
                Vector3 d1 = aVerticies[i] - aVerticies[i+1];
                Vector3 d2 = aVerticies[i] - aVerticies[i+2];

                Vector3 normal = d1.Cross(d2).Normalized();

                normals[i] = normal;
                normals[i + 1] = normal;
                normals[i + 2] = normal;
            }

            arrays[(int)Mesh.ArrayType.Normal] = normals;

            self.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

            return true;
        }

    }
}

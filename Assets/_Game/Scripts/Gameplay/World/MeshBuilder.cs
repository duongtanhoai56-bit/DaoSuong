using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay
{
    /// <summary>Builds flat-shaded meshes with per-vertex biome colors.</summary>
    public sealed class MeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Color32> colors = new List<Color32>();
        private readonly List<int> triangles = new List<int>();

        public int TriangleCount => triangles.Count / 3;

        public static Color32 Encode(Color color)
        {
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                color = color.linear;
            return color;
        }

        /// <param name="insideReference">A point known to be inside the object, used to orient the face outward.</param>
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color32 color, Vector3 insideReference)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            Vector3 center = (a + b + c) / 3f;
            if (Vector3.Dot(normal, center - insideReference) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }

            if (normal.sqrMagnitude < 0.000001f)
                return;

            normal.Normalize();
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        public Mesh ToMesh(string meshName)
        {
            Mesh mesh = new Mesh { name = meshName };
            if (vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

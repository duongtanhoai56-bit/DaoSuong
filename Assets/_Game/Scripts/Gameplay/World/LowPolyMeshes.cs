using UnityEngine;

namespace Game.Gameplay
{
    public static class LowPolyMeshes
    {
        public static Mesh Tree()
        {
            MeshBuilder builder = new MeshBuilder();
            Color32 trunk = MeshBuilder.Encode(new Color(0.42f, 0.30f, 0.20f));
            Color32 leafA = MeshBuilder.Encode(new Color(0.18f, 0.42f, 0.20f));
            Color32 leafB = MeshBuilder.Encode(new Color(0.24f, 0.50f, 0.24f));
            Prism(builder, 0f, 1.8f, 0.22f, 6, trunk);
            Cone(builder, 1.0f, 2.2f, 1.35f, 7, leafA);
            Cone(builder, 2.3f, 1.9f, 1.0f, 7, leafB);
            return builder.ToMesh("Tree_LowPoly");
        }

        public static Mesh Rock(int seed)
        {
            DeterministicRandom random = new DeterministicRandom(seed);
            MeshBuilder builder = new MeshBuilder();
            Color32 top = MeshBuilder.Encode(new Color(0.52f, 0.52f, 0.55f));
            Color32 side = MeshBuilder.Encode(new Color(0.42f, 0.42f, 0.45f));
            const int sides = 6;
            Vector3[] ring = new Vector3[sides];

            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                float radius = random.Range(0.8f, 1.2f);
                float y = random.Range(-0.15f, 0.15f);
                ring[i] = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
            }

            Vector3 upper = new Vector3(0f, random.Range(0.75f, 1.05f), 0f);
            Vector3 lower = new Vector3(0f, -0.45f, 0f);
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                builder.Tri(upper, ring[i], ring[next], top, Vector3.zero);
                builder.Tri(lower, ring[next], ring[i], side, Vector3.zero);
            }

            return builder.ToMesh("Rock_LowPoly_" + seed);
        }

        public static Mesh Box(Vector3 size, Color color)
        {
            MeshBuilder builder = new MeshBuilder();
            Color32 vertexColor = MeshBuilder.Encode(color);
            Vector3 half = size * 0.5f;
            Vector3[] points = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                points[i] = new Vector3(
                    (i & 1) == 0 ? -half.x : half.x,
                    (i & 2) == 0 ? -half.y : half.y,
                    (i & 4) == 0 ? -half.z : half.z);
            }

            int[][] faces =
            {
                new[] { 0, 1, 3, 2 }, new[] { 4, 6, 7, 5 }, new[] { 0, 2, 6, 4 },
                new[] { 1, 5, 7, 3 }, new[] { 0, 4, 5, 1 }, new[] { 2, 3, 7, 6 }
            };

            foreach (int[] face in faces)
            {
                builder.Tri(points[face[0]], points[face[1]], points[face[2]], vertexColor, Vector3.zero);
                builder.Tri(points[face[0]], points[face[2]], points[face[3]], vertexColor, Vector3.zero);
            }

            return builder.ToMesh("LowPoly_Box");
        }

        private static void Cone(MeshBuilder builder, float y0, float height, float radius, int sides, Color32 color)
        {
            Vector3 apex = new Vector3(0f, y0 + height, 0f);
            Vector3 baseCenter = new Vector3(0f, y0, 0f);
            Vector3 inside = new Vector3(0f, y0 + height / 3f, 0f);

            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides;
                float a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * radius, y0, Mathf.Sin(a0) * radius);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, y0, Mathf.Sin(a1) * radius);
                builder.Tri(apex, p0, p1, color, inside);
                builder.Tri(baseCenter, p1, p0, color, inside);
            }
        }

        private static void Prism(MeshBuilder builder, float y0, float y1, float radius, int sides, Color32 color)
        {
            Vector3 inside = new Vector3(0f, (y0 + y1) * 0.5f, 0f);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides;
                float a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, y0, Mathf.Sin(a0) * radius);
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, y0, Mathf.Sin(a1) * radius);
                Vector3 t0 = new Vector3(b0.x, y1, b0.z);
                Vector3 t1 = new Vector3(b1.x, y1, b1.z);
                builder.Tri(b0, t0, t1, color, inside);
                builder.Tri(b0, t1, b1, color, inside);
            }
        }
    }
}

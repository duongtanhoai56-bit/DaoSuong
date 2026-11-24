using UnityEngine;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Island Settings", fileName = "IslandSettings")]
    public sealed class IslandSettings : ScriptableObject
    {
        [Header("Kích thước")]
        [Min(64f)] public float worldSize = 480f;
        [Min(1)] public int chunksPerSide = 4;
        [Min(4)] public int cellsPerChunk = 24;

        [Header("Hình dạng đảo")]
        [Range(0.2f, 0.8f)] public float islandCore = 0.45f;
        [Range(0.6f, 1.3f)] public float islandEdge = 0.95f;
        [Range(0f, 0.8f)] public float coastRoughness = 0.35f;
        public float seabedDepth = -8f;
        public float lowlandHeight = 2.5f;
        public float hillHeight = 10f;
        public float mountainHeight = 24f;
        [Range(0.1f, 0.4f)] public float mountainRadius = 0.22f;
        [Range(0.1f, 0.3f)] public float swampRadius = 0.17f;

        [Header("Vùng sinh thái")]
        public float beachMaxHeight = 1.6f;
        public float swampMaxHeight = 4.5f;
        [Range(0f, 1f)] public float swampMoisture = 0.62f;
        public float mountainStartHeight = 14f;
        public float snowStartHeight = 28f;
        [Range(0.1f, 1f)] public float rockSlope = 0.72f;

        [Header("Vật thể")]
        [Min(0)] public int maxTrees = 400;
        [Min(0)] public int maxRocks = 80;
        [Range(0, 3)] public int caveEntrances = 3;
        [Min(1f)] public float propGridStep = 8f;
        [Min(1f)] public float caveMinSpacing = 45f;

        [Header("Màu")]
        public Color seabed = new Color(0.50f, 0.46f, 0.36f);
        public Color sand = new Color(0.86f, 0.78f, 0.55f);
        public Color swamp = new Color(0.28f, 0.36f, 0.20f);
        public Color forest = new Color(0.27f, 0.52f, 0.24f);
        public Color rock = new Color(0.45f, 0.45f, 0.48f);
        public Color snow = new Color(0.93f, 0.95f, 0.98f);

        private void OnValidate()
        {
            worldSize = Mathf.Max(64f, worldSize);
            chunksPerSide = Mathf.Clamp(chunksPerSide, 1, 8);
            cellsPerChunk = Mathf.Clamp(cellsPerChunk, 4, 64);
            islandCore = Mathf.Clamp(islandCore, 0.2f, 0.8f);
            islandEdge = Mathf.Max(islandCore + 0.05f, islandEdge);
            maxTrees = Mathf.Max(0, maxTrees);
            maxRocks = Mathf.Max(0, maxRocks);
            caveEntrances = Mathf.Clamp(caveEntrances, 0, 3);
            propGridStep = Mathf.Max(1f, propGridStep);
            caveMinSpacing = Mathf.Max(1f, caveMinSpacing);
        }
    }
}

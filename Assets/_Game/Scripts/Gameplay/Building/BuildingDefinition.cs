using System;
using UnityEngine;

namespace Game.Gameplay
{
    [Serializable]
    public sealed class BuildCost
    {
        public ItemData item;
        [Min(1)] public int amount = 2;
    }

    [CreateAssetMenu(menuName = "Game/Building/Building Definition", fileName = "Building_New")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        public string id = "building";
        public string displayName = "Công trình";
        public PrimitiveType shape = PrimitiveType.Cube;
        public Vector3 size = new Vector3(2f, 2f, 2f);
        public Color color = new Color(0.45f, 0.35f, 0.25f);
        public BuildCost[] costs = Array.Empty<BuildCost>();
        [Min(1f)] public float maxHealth = 100f;
        public bool providesWarmth;
        [Min(0f)] public float warmthRadius = 8f;
        [Min(0f)] public float warmthCelsius = 8f;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name.ToLowerInvariant().Replace(' ', '_');
            if (string.IsNullOrWhiteSpace(displayName)) displayName = name;
            size.x = Safe(size.x, 2f, 0.2f, 20f);
            size.y = Safe(size.y, 2f, 0.2f, 20f);
            size.z = Safe(size.z, 2f, 0.2f, 20f);
            maxHealth = Safe(maxHealth, 100f, 1f, 100000f);
            warmthRadius = Safe(warmthRadius, 8f, 0f, 40f);
            warmthCelsius = Safe(warmthCelsius, 8f, 0f, 40f);
            if (costs == null) costs = Array.Empty<BuildCost>();
            for (int i = 0; i < costs.Length; i++)
                if (costs[i] != null) costs[i].amount = Mathf.Clamp(costs[i].amount, 1, 999);
        }

        private static float Safe(float value, float fallback, float min, float max)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }

    [CreateAssetMenu(menuName = "Game/Building/Building Catalog", fileName = "BuildingCatalog")]
    public sealed class BuildingCatalog : ScriptableObject
    {
        public BuildingDefinition[] pieces = Array.Empty<BuildingDefinition>();

        public BuildingDefinition Find(string id)
        {
            if (pieces == null || string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < pieces.Length; i++)
                if (pieces[i] != null && pieces[i].id == id) return pieces[i];
            return null;
        }
    }
}

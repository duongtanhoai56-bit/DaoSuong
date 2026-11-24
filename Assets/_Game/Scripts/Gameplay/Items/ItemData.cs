using UnityEngine;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Item", fileName = "Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Định danh")]
        public string id;
        public string displayName = "Vật phẩm";
        [Min(1)] public int maxStack = 50;
        [Min(0f)] public float weight = 0.1f;
        public Sprite icon;
        public Color color = Color.white;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = name.ToLowerInvariant().Replace(' ', '_');
            maxStack = Mathf.Max(1, maxStack);
            if (float.IsNaN(weight) || float.IsInfinity(weight)) weight = float.MaxValue;
            weight = Mathf.Max(0f, weight);
        }
    }
}

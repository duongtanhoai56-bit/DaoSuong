using UnityEngine;

namespace Game.Gameplay
{
    public enum ToolType { None, Axe, Pickaxe, FishingRod }

    [CreateAssetMenu(menuName = "Game/Tool", fileName = "Tool")]
    public sealed class ToolData : ItemData
    {
        [Header("Thông số công cụ")]
        public ToolType toolType = ToolType.Axe;
        [Min(1)] public int tier = 1;
        [Min(0.1f)] public float power = 1f;
        [Min(1)] public int maxDurability = 60;
        [Min(0.1f)] public float swingInterval = 0.6f;

        protected override void OnValidate()
        {
            base.OnValidate();
            maxStack = 1;
            maxDurability = Mathf.Max(1, maxDurability);
            tier = Mathf.Max(1, tier);
            if (float.IsNaN(power) || float.IsInfinity(power)) power = 1f;
            if (float.IsNaN(swingInterval) || float.IsInfinity(swingInterval)) swingInterval = 0.6f;
            power = Mathf.Max(0.1f, power);
            swingInterval = Mathf.Max(0.1f, swingInterval);
        }
    }
}

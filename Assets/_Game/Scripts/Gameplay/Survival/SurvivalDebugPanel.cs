using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Development-only survival diagnostics. Never included in non-development builds.</summary>
    public sealed class SurvivalDebugPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private SurvivalController survival;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private ConsumableData[] testConsumables;

        private void OnGUI()
        {
            if (survival == null) return;
            float scale = Mathf.Max(1f, Screen.height / 900f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(10f, 460f, 250f, 240f), GUI.skin.box);
            SurvivalVitals v = survival.Vitals;
            GUILayout.Label($"Sinh tồn HP {v.Health:0} | Đói {v.Hunger:0} | Khát {v.Thirst:0}");
            if (GUILayout.Button("Nhận đồ ăn / nước test") && inventory != null && testConsumables != null)
                foreach (ConsumableData item in testConsumables) if (item != null) inventory.Add(item, 3);
            if (GUILayout.Button("Test chảy máu 30 giây")) survival.ApplyBleeding(30f);
            if (GUILayout.Button("Test bệnh 60 giây")) survival.ApplySickness(60f, 1.8f);
            if (GUILayout.Button("Nhận 25 sát thương")) survival.ApplyDamage(25f);
            if (GUILayout.Button("Hồi toàn bộ chỉ số")) survival.RestoreForDebug();
            GUILayout.EndArea();
            GUI.matrix = previous;
        }
#endif
    }
}

using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>Development-only testing helpers; stripped from non-development release builds.</summary>
    public sealed class GatherDebugPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private GatherSettings settings;
        [SerializeField] private ResourceSpawner spawner;

        private void OnGUI()
        {
            if (inventory == null || settings == null) return;

            float scale = Mathf.Max(1f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            const float width = 250f;
            GUILayout.BeginArea(new Rect(Screen.width / scale - width - 10f, 90f, width, 440f), GUI.skin.box);

            GUILayout.Label("GATHER DEBUG · DEVELOPMENT");
            if (GUILayout.Button("Nhận công cụ test"))
            {
                if (settings.debugTools != null)
                {
                    foreach (ToolData tool in settings.debugTools)
                        if (tool != null) inventory.Add(tool, 1);
                }
            }

            if (GUILayout.Button("Công cụ còn 3 độ bền"))
            {
                bool changed = false;
                for (int i = 0; i < inventory.SlotCount; i++)
                {
                    ItemStack stack = inventory.GetSlot(i);
                    if (stack != null && stack.Tool != null)
                    {
                        stack.durability = Mathf.Min(stack.durability, 3);
                        changed = true;
                    }
                }
                if (changed) inventory.NotifyChanged();
            }

            if (spawner != null && GUILayout.Button("Hồi tài nguyên ngay"))
                spawner.RespawnAllNow();

            GUILayout.Space(8f);
            GUILayout.Label("INVENTORY");
            GUILayout.Label(inventory.Summary());
            GUILayout.EndArea();
            GUI.matrix = previous;
        }
#endif
    }
}

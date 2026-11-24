#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Gameplay;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Game.EditorTools
{
    public static class Phase4InventoryTools
    {
        [MenuItem("Đảo Sương/Giai đoạn 4/1 - Nâng cấp Inventory cho Scene đang mở")]
        public static void UpgradeOpenScene()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                EditorUtility.DisplayDialog("Đảo Sương", "Không tìm thấy GameObject Player trong Scene đang mở.", "OK");
                return;
            }

            IslandGenerator island = Object.FindFirstObjectByType<IslandGenerator>();
            GatherSettings gatherSettings = AssetDatabase.LoadAssetAtPath<GatherSettings>("Assets/_Game/Data/Gather/GatherSettings.asset");
            if (gatherSettings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:GatherSettings");
                if (guids.Length > 0)
                    gatherSettings = AssetDatabase.LoadAssetAtPath<GatherSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (island == null || gatherSettings == null)
            {
                string message = (island == null ? "Scene thiếu IslandGenerator.\n" : string.Empty) +
                                 (gatherSettings == null ? "Chưa có GatherSettings. Chạy Game → Giai đoạn 3 → Tạo dữ liệu mẫu trước." : string.Empty);
                EditorUtility.DisplayDialog("Đảo Sương", message, "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Nâng cấp Inventory",
                    "Thao tác này sẽ thêm PlayerInventory, cập nhật PlayerGatherer/ResourceSpawner, tạo HUD và rương thử, đồng thời gỡ SimpleBackpack tạm khỏi Player. Scene sẽ được lưu sau khi hoàn tất.",
                    "Nâng cấp", "Hủy"))
                return;

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) inventory = Undo.AddComponent<PlayerInventory>(player);

            ResourceSpawner spawner = Object.FindFirstObjectByType<ResourceSpawner>();
            if (spawner == null) spawner = Undo.AddComponent<ResourceSpawner>(island.gameObject);
            Material pickupMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Terrain.mat");
            Assign(spawner, "island", island);
            Assign(spawner, "settings", gatherSettings);
            if (pickupMaterial != null) Assign(spawner, "pickupMaterial", pickupMaterial);
            // Re-enable after assigning references so OnEnable subscribes with valid dependencies.
            spawner.enabled = false;
            spawner.enabled = true;

            PlayerGatherer gatherer = player.GetComponent<PlayerGatherer>();
            if (gatherer == null) gatherer = Undo.AddComponent<PlayerGatherer>(player);
            Assign(gatherer, "inventory", inventory);
            Assign(gatherer, "island", island);
            Assign(gatherer, "settings", gatherSettings);
            Camera mainCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (mainCamera != null) Assign(gatherer, "aimTransform", mainCamera.transform);

            // Remove only the known Phase 3 compatibility component; do not delete unrelated missing scripts.
            foreach (MonoBehaviour component in player.GetComponents<MonoBehaviour>())
            {
                if (component != null && component.GetType().Name == "SimpleBackpack")
                    Undo.DestroyObjectImmediate(component);
            }

            GatherDebugPanel debug = Object.FindFirstObjectByType<GatherDebugPanel>();
            if (debug == null) debug = Undo.AddComponent<GatherDebugPanel>(island.gameObject);
            Assign(debug, "inventory", inventory);
            Assign(debug, "settings", gatherSettings);
            Assign(debug, "spawner", spawner);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create inventory Canvas");
            }

            SafeAreaFitter.GetOrCreate(canvas.transform);
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            if (canvas.GetComponent<CanvasScaler>() == null)
            {
                CanvasScaler scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(eventObject, "Create Input System EventSystem");
            }
            else
            {
                StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (legacyModule != null) Undo.DestroyObjectImmediate(legacyModule);
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                    Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
            }

            InventoryHUD hud = canvas.GetComponent<InventoryHUD>();
            if (hud == null) hud = Undo.AddComponent<InventoryHUD>(canvas.gameObject);
            Assign(hud, "inventory", inventory);
            Assign(hud, "playerRoot", player.transform);

            // A small test chest is positioned beside the beach spawn when the island finishes generating.
            GameObject chestObject = GameObject.Find("StorageChest_Test");
            if (chestObject == null)
            {
                chestObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chestObject.name = "StorageChest_Test";
                chestObject.transform.localScale = new Vector3(1.25f, 1f, 0.9f);
                Undo.RegisterCreatedObjectUndo(chestObject, "Create sample storage chest");
            }
            StorageContainer chest = chestObject.GetComponent<StorageContainer>();
            if (chest == null) chest = Undo.AddComponent<StorageContainer>(chestObject);
            AssignString(chest, "displayName", "Rương thử nghiệm");
            AssignBool(chest, "placeNearPlayerSpawn", true);
            Assign(chest, "spawnIsland", island);
            Assign(chest, "spawnPlayer", player.transform);

            EditorUtility.SetDirty(inventory);
            EditorUtility.SetDirty(spawner);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(gatherer);
            EditorUtility.SetDirty(debug);
            EditorSceneManager.MarkSceneDirty(player.scene);
            EditorSceneManager.SaveScene(player.scene);

            if (pickupMaterial == null)
                Debug.LogWarning("[Phase 4] Không tìm thấy M_Terrain.mat. Pickup sẽ thiếu material cho đến khi tạo lại World Assets.");
            EditorUtility.DisplayDialog("Đảo Sương",
                "Đã gắn PlayerInventory, nối PlayerGatherer/ResourceSpawner/GatherDebugPanel và tạo InventoryHUD runtime.\n\n" +
                "Chạy Play, bấm TÚI. Đi tới rương thử và bấm RƯƠNG để chuyển đồ.", "OK");
        }

        [MenuItem("Đảo Sương/Giai đoạn 4/2 - Kiểm tra Inventory Scene")]
        public static void ValidateOpenScene()
        {
            List<string> issues = new List<string>();
            GameObject player = GameObject.Find("Player");
            if (player == null) issues.Add("Thiếu GameObject Player.");
            else
            {
                if (player.GetComponent<PlayerInventory>() == null) issues.Add("Player chưa có PlayerInventory.");
                PlayerGatherer gatherer = player.GetComponent<PlayerGatherer>();
                if (gatherer == null) issues.Add("Player chưa có PlayerGatherer.");
                else if (GetReference(gatherer, "inventory") == null) issues.Add("PlayerGatherer chưa nối PlayerInventory.");
                if (gatherer != null && GetReference(gatherer, "settings") == null) issues.Add("PlayerGatherer thiếu GatherSettings.");
            }
            IslandGenerator island = Object.FindFirstObjectByType<IslandGenerator>();
            if (island == null) issues.Add("Scene thiếu IslandGenerator.");

            ResourceSpawner spawner = Object.FindFirstObjectByType<ResourceSpawner>();
            if (spawner == null) issues.Add("Scene thiếu ResourceSpawner.");
            else
            {
                if (GetReference(spawner, "island") == null) issues.Add("ResourceSpawner chưa nối IslandGenerator.");
                if (GetReference(spawner, "settings") == null) issues.Add("ResourceSpawner thiếu GatherSettings.");
                if (GetReference(spawner, "pickupMaterial") == null) issues.Add("ResourceSpawner thiếu M_Terrain Pickup Material.");
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                issues.Add("Scene thiếu Canvas.");
            }
            else
            {
                Transform safeRoot = canvas.transform.Find("MobileSafeAreaRoot");
                Transform hudRoot = safeRoot != null ? safeRoot : canvas.transform;
                Transform gatherHud = hudRoot.Find("GatherHud");
                if (gatherHud == null) gatherHud = canvas.transform.Find("GatherHud");
                if (gatherHud == null || gatherHud.GetComponent<GatherHud>() == null)
                    issues.Add("Thiếu GatherHud. Chạy Đảo Sương → Giai đoạn 3 → 1 - Tạo Action Button + Gather HUD.");
                Transform actionButton = hudRoot.Find("ActionButton");
                if (actionButton == null) actionButton = canvas.transform.Find("ActionButton");
                if (actionButton == null || actionButton.GetComponent<ActionButton>() == null)
                    issues.Add("Thiếu ActionButton. Chạy Đảo Sương → Giai đoạn 3 → 1 - Tạo Action Button + Gather HUD.");

                InventoryHUD hud = canvas.GetComponent<InventoryHUD>();
                if (hud == null) issues.Add("Canvas thiếu InventoryHUD.");
                else
                {
                    if (GetReference(hud, "inventory") == null) issues.Add("InventoryHUD chưa nối PlayerInventory.");
                }
                if (canvas.GetComponent<GraphicRaycaster>() == null) issues.Add("Canvas thiếu GraphicRaycaster.");
            }

            GameObject testChest = GameObject.Find("StorageChest_Test");
            if (testChest == null) issues.Add("Chưa có StorageChest_Test (cần để test chuyển đồ).");
            else if (testChest.GetComponent<StorageContainer>() == null) issues.Add("StorageChest_Test chưa có StorageContainer.");
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null) issues.Add("Scene thiếu EventSystem.");
            else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                issues.Add("EventSystem chưa có InputSystemUIInputModule.");

            EditorUtility.DisplayDialog("Đảo Sương - Inventory", issues.Count == 0
                ? "Không phát hiện thiếu tham chiếu inventory cơ bản."
                : string.Join("\n", issues), "OK");
        }

        private static void Assign(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[Phase 4] Không tìm thấy serialized field '{propertyName}' trên {target.name}.");
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static void AssignString(Object target, string propertyName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.stringValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static void AssignBool(Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.boolValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static Object GetReference(Object target, string propertyName)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue : null;
        }
    }
}
#endif

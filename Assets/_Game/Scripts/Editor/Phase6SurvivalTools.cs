#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Gameplay;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
    public static class Phase6SurvivalTools
    {
        private const string DataFolder = "Assets/_Game/Data/Survival";
        private const string SettingsPath = DataFolder + "/SurvivalSettings.asset";

        [MenuItem("Đảo Sương/Giai đoạn 6/1 - Tạo dữ liệu sinh tồn")]
        public static void CreateData()
        {
            EnsureFolder(DataFolder);
            SurvivalSettings settings = LoadOrCreate<SurvivalSettings>(SettingsPath, "SurvivalSettings");
            EditorUtility.SetDirty(settings);

            ConsumableData driedFruit = MakeConsumable("DriedFruit", "dried_fruit", "Trái cây sấy", 0.2f,
                0f, 24f, 0f, 4f, 0f, 180f, 0.75f, 1f, false, false,
                new Color(0.72f, 0.38f, 0.18f));
            ConsumableData coconutWater = MakeConsumable("CoconutWater", "coconut_water", "Nước dừa", 0.4f,
                0f, 0f, 32f, 8f, 0f, 180f, 1f, 0.7f, false, false,
                new Color(0.35f, 0.78f, 0.64f));
            ConsumableData cookedFish = MakeConsumable("CookedFish", "cooked_fish", "Cá nướng", 0.35f,
                34f, 4f, 8f, 16f, 0f, 240f, 0.7f, 0.85f, false, false,
                new Color(0.72f, 0.58f, 0.35f));
            ConsumableData herbalBandage = MakeConsumable("HerbalBandage", "herbal_bandage", "Băng thuốc", 0.15f,
                0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, true, false,
                new Color(0.62f, 0.76f, 0.45f));
            ConsumableData herbalTea = MakeConsumable("HerbalTea", "herbal_tea", "Trà thảo mộc", 0.3f,
                0f, 18f, 4f, 0f, 1.2f, 0f, 1f, 1f, false, true,
                new Color(0.42f, 0.62f, 0.34f));
            ConsumableData fishSmall = MakeConsumable("FishSmallFood", "fish_small", "Cá nhỏ", 0.3f,
                0f, 14f, 0f, 2f, 0f, 0f, 1f, 1f, false, false,
                new Color(0.5f, 0.72f, 0.86f));
            ConsumableData fishBig = MakeConsumable("FishBigFood", "fish_big", "Cá lớn", 0.55f,
                2f, 26f, 0f, 6f, 0f, 120f, 0.85f, 1f, false, false,
                new Color(0.34f, 0.58f, 0.78f));
            ConsumableData eel = MakeConsumable("EelFood", "eel", "Lươn", 0.45f,
                0f, 20f, 0f, 4f, 0f, 60f, 0.9f, 1f, false, false,
                new Color(0.34f, 0.4f, 0.32f));

            UpdateFishingTable(fishSmall, fishBig, eel);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            EditorUtility.DisplayDialog("Đảo Sương - Sinh tồn",
                "Đã tạo SurvivalSettings và 8 vật phẩm tiêu thụ (đồ ăn, nước, băng thuốc và cá) tại " + DataFolder + ".", "OK");
        }

        [MenuItem("Đảo Sương/Giai đoạn 6/2 - Nâng cấp Scene sinh tồn")]
        public static void UpgradeScene()
        {
            if (EditorApplication.isPlaying) return;
            SurvivalSettings settings = AssetDatabase.LoadAssetAtPath<SurvivalSettings>(SettingsPath);
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Đảo Sương", "Hãy chạy bước 1 để tạo SurvivalSettings và vật phẩm trước.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Nâng cấp Scene sinh tồn",
                "Sẽ thêm SurvivalController, HUD chỉ số, nút chạy nhanh và bảng debug vào Scene đang mở. Scene sẽ được lưu.",
                "Nâng cấp", "Hủy")) return;

            GameObject player = GameObject.Find("Player");
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            IslandGenerator island = Object.FindFirstObjectByType<IslandGenerator>();
            DayNightCycle dayNight = Object.FindFirstObjectByType<DayNightCycle>();
            WeatherSystem weather = Object.FindFirstObjectByType<WeatherSystem>();
            MobileInput input = Object.FindFirstObjectByType<MobileInput>();
            if (player == null || canvas == null || island == null || input == null)
            {
                EditorUtility.DisplayDialog("Đảo Sương",
                    "Thiếu Player, Canvas, IslandGenerator hoặc MobileInput. Hãy hoàn tất Scene Phase 2–4 trước.", "OK");
                return;
            }

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                EditorUtility.DisplayDialog("Đảo Sương", "Player chưa có PlayerInventory. Hãy nâng cấp Inventory ở Giai đoạn 4 trước.", "OK");
                return;
            }

            SurvivalController survival = player.GetComponent<SurvivalController>();
            if (survival == null) survival = Undo.AddComponent<SurvivalController>(player);
            Assign(survival, "settings", settings);
            Assign(survival, "island", island);
            Assign(survival, "dayNight", dayNight);
            Assign(survival, "weather", weather);
            Assign(survival, "inventory", inventory);

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null) Assign(controller, "survival", survival);
            PlayerGatherer gatherer = player.GetComponent<PlayerGatherer>();
            if (gatherer != null) Assign(gatherer, "survival", survival);

            Transform safeRoot = SafeAreaFitter.GetOrCreate(canvas.transform);
            if (safeRoot == null) safeRoot = canvas.transform;
            ActionButton sprint = CreateSprintButton(safeRoot);
            Assign(input, "sprintButton", sprint);

            SurvivalHUD hud = canvas.GetComponent<SurvivalHUD>();
            if (hud == null) hud = Undo.AddComponent<SurvivalHUD>(canvas.gameObject);
            Assign(hud, "survival", survival);

            InventoryHUD inventoryHud = canvas.GetComponent<InventoryHUD>();
            if (inventoryHud != null) Assign(inventoryHud, "survivalController", survival);

            SurvivalDebugPanel debug = island.GetComponent<SurvivalDebugPanel>();
            if (debug == null) debug = Undo.AddComponent<SurvivalDebugPanel>(island.gameObject);
            Assign(debug, "survival", survival);
            Assign(debug, "inventory", inventory);
            AssignArray(debug, "testConsumables", new Object[] { LoadConsumable("DriedFruit"), LoadConsumable("CoconutWater"), LoadConsumable("CookedFish"), LoadConsumable("HerbalBandage"), LoadConsumable("HerbalTea") });

            EditorUtility.SetDirty(survival);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(input);
            EditorSceneManager.MarkSceneDirty(player.scene);
            EditorSceneManager.SaveScene(player.scene);
            EditorUtility.DisplayDialog("Đảo Sương",
                "Đã thêm HUD sinh tồn, chạy nhanh và debug. Chọn vật phẩm tiêu thụ trong Túi đồ rồi nhấn DÙNG.", "OK");
        }

        [MenuItem("Đảo Sương/Giai đoạn 6/3 - Kiểm tra Scene sinh tồn")]
        public static void ValidateScene()
        {
            List<string> issues = new List<string>();
            GameObject player = GameObject.Find("Player");
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            SurvivalSettings settings = AssetDatabase.LoadAssetAtPath<SurvivalSettings>(SettingsPath);
            if (settings == null) issues.Add("Thiếu SurvivalSettings.asset; chạy bước 1.");
            if (player == null) issues.Add("Không tìm thấy GameObject Player.");
            else
            {
                if (player.GetComponent<PlayerInventory>() == null) issues.Add("Player thiếu PlayerInventory.");
                SurvivalController survival = player.GetComponent<SurvivalController>();
                if (survival == null) issues.Add("Player thiếu SurvivalController.");
                else if (GetReference(survival, "settings") == null) issues.Add("SurvivalController chưa gán SurvivalSettings.");
                PlayerController movement = player.GetComponent<PlayerController>();
                if (movement == null) issues.Add("Player thiếu PlayerController.");
                else if (GetReference(movement, "survival") == null) issues.Add("PlayerController chưa gán SurvivalController.");
                PlayerGatherer gatherer = player.GetComponent<PlayerGatherer>();
                if (gatherer != null && GetReference(gatherer, "survival") == null)
                    issues.Add("PlayerGatherer chưa gán SurvivalController (sẽ tự tìm trong Awake nếu cùng GameObject).");
            }
            if (canvas == null) issues.Add("Thiếu Canvas.");
            else
            {
                if (canvas.GetComponent<SurvivalHUD>() == null) issues.Add("Canvas thiếu SurvivalHUD.");
                InventoryHUD inventoryHud = canvas.GetComponent<InventoryHUD>();
                if (inventoryHud == null) issues.Add("Canvas thiếu InventoryHUD.");
                else if (GetReference(inventoryHud, "survivalController") == null) issues.Add("InventoryHUD chưa gán SurvivalController (nút DÙNG không hoạt động).");
                Transform safeRoot = canvas.transform.Find("MobileSafeAreaRoot");
                Transform sprint = safeRoot != null ? safeRoot.Find("SprintButton") : null;
                if (sprint == null) sprint = canvas.transform.Find("SprintButton");
                if (sprint == null || sprint.GetComponent<ActionButton>() == null) issues.Add("Thiếu SprintButton / ActionButton.");
                MobileInput input = Object.FindFirstObjectByType<MobileInput>();
                if (input == null || GetReference(input, "sprintButton") == null) issues.Add("MobileInput chưa gán SprintButton.");
            }
            if (issues.Count == 0)
                EditorUtility.DisplayDialog("Đảo Sương - Survival", "Không phát hiện thiếu reference cốt lõi.", "OK");
            else
                EditorUtility.DisplayDialog("Đảo Sương - Survival", string.Join("\n", issues), "OK");
        }

        private static ActionButton CreateSprintButton(Transform canvas)
        {
            Transform existing = canvas.Find("SprintButton");
            GameObject go = existing != null ? existing.gameObject : new GameObject("SprintButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(go, "Create Sprint Button");
                go.transform.SetParent(canvas, false);
            }
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-470f, 455f);
            rect.sizeDelta = new Vector2(164f, 118f);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.16f, 0.42f, 0.3f, 0.9f);
            image.raycastTarget = true;

            ActionButton action = go.GetComponent<ActionButton>();
            if (action == null) action = Undo.AddComponent<ActionButton>(go);
            CanvasGroup group = go.GetComponent<CanvasGroup>();
            if (group == null) Undo.AddComponent<CanvasGroup>(go);
            HudElement element = go.GetComponent<HudElement>();
            if (element == null) element = Undo.AddComponent<HudElement>(go);
            SetString(element, "id", "sprint");
            SetBool(element, "customizable", true);

            Text label = go.transform.Find("Label") != null ? go.transform.Find("Label").GetComponent<Text>() : null;
            if (label == null)
            {
                GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textObject.transform.SetParent(go.transform, false);
                label = textObject.GetComponent<Text>();
            }
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "CHẠY NHANH";
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return action;
        }

        private static ConsumableData MakeConsumable(string file, string id, string label, float weight,
            float health, float hunger, float thirst, float stamina, float temperature,
            float buffDuration, float hungerMultiplier, float thirstMultiplier,
            bool clearsBleeding, bool curesSickness, Color color)
        {
            string path = DataFolder + "/" + file + ".asset";
            ConsumableData item = AssetDatabase.LoadAssetAtPath<ConsumableData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ConsumableData>();
                item.name = file;
                AssetDatabase.CreateAsset(item, path);
            }
            item.id = id; item.displayName = label; item.maxStack = 20; item.weight = weight; item.color = color;
            item.healthRestore = health; item.hungerRestore = hunger; item.thirstRestore = thirst; item.staminaRestore = stamina;
            item.temperatureDeltaC = temperature; item.buffDurationSeconds = buffDuration;
            item.hungerDrainMultiplier = hungerMultiplier; item.thirstDrainMultiplier = thirstMultiplier;
            item.clearsBleeding = clearsBleeding; item.curesSickness = curesSickness;
            EditorUtility.SetDirty(item);
            return item;
        }

        private static void UpdateFishingTable(ConsumableData fishSmall, ConsumableData fishBig, ConsumableData eel)
        {
            FishingTable table = null;
            string[] settingsGuids = AssetDatabase.FindAssets("t:GatherSettings", new[] { "Assets/_Game/Data" });
            foreach (string guid in settingsGuids)
            {
                string settingsPath = AssetDatabase.GUIDToAssetPath(guid);
                GatherSettings gatherSettings = AssetDatabase.LoadAssetAtPath<GatherSettings>(settingsPath);
                if (gatherSettings != null && gatherSettings.fishing != null)
                {
                    table = gatherSettings.fishing;
                    break;
                }
            }

            if (table == null)
            {
                string[] tableGuids = AssetDatabase.FindAssets("t:FishingTable", new[] { "Assets/_Game/Data" });
                if (tableGuids != null && tableGuids.Length > 0)
                    table = AssetDatabase.LoadAssetAtPath<FishingTable>(AssetDatabase.GUIDToAssetPath(tableGuids[0]));
            }

            if (table == null)
            {
                Debug.LogWarning("[Phase 6] Không tìm thấy FishingTable trong Assets/_Game/Data. Hãy chạy công cụ tạo dữ liệu Giai đoạn 3 trước.");
                return;
            }

            List<FishDrop> drops = table.drops != null ? new List<FishDrop>(table.drops) : new List<FishDrop>();
            UpsertFishDrop(drops, fishSmall, 60f, 1, 2);
            UpsertFishDrop(drops, fishBig, 25f, 1, 1);
            UpsertFishDrop(drops, eel, 15f, 1, 1);
            table.drops = drops.ToArray();
            EditorUtility.SetDirty(table);
        }

        private static void UpsertFishDrop(List<FishDrop> drops, ConsumableData consumable,
            float defaultWeight, int defaultMin, int defaultMax)
        {
            bool found = false;
            for (int i = 0; i < drops.Count; i++)
            {
                FishDrop entry = drops[i];
                if (entry == null || entry.item == null || entry.item.id != consumable.id) continue;
                entry.item = consumable;
                found = true;
            }
            if (!found)
                drops.Add(new FishDrop
                {
                    item = consumable,
                    weight = defaultWeight,
                    min = defaultMin,
                    max = defaultMax
                });
        }

        private static ConsumableData LoadConsumable(string name)
            => AssetDatabase.LoadAssetAtPath<ConsumableData>(DataFolder + "/" + name + ".asset");

        private static T LoadOrCreate<T>(string path, string assetName) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void Assign(Object target, string field, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static Object GetReference(Object target, string field)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null ? property.objectReferenceValue : null;
        }

        private static void SetString(Object target, string field, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property != null) { property.stringValue = value; serialized.ApplyModifiedProperties(); }
        }

        private static void SetBool(Object target, string field, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property != null) { property.boolValue = value; serialized.ApplyModifiedProperties(); }
        }

        private static void AssignArray(Object target, string field, Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null || !property.isArray) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedProperties();
        }
    }
}
#endif

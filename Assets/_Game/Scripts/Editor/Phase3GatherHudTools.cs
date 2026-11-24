#if UNITY_EDITOR
using System.Collections.Generic;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>Creates and wires the Phase 3 touch action button and gathering HUD.</summary>
    public static class Phase3GatherHudTools
    {
        [MenuItem("Đảo Sương/Giai đoạn 3/1 - Tạo Action Button + Gather HUD")]
        public static void CreateOrUpdateHud()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            MobileInput input = Object.FindFirstObjectByType<MobileInput>();
            if (canvas == null || input == null)
            {
                EditorUtility.DisplayDialog("Đảo Sương - Giai đoạn 3",
                    "Không tìm thấy Canvas hoặc MobileInput. Hãy tạo Scene bằng menu Phase 2 trước.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Nâng cấp HUD thu thập",
                    "Sẽ tạo/cập nhật nút TÁC ĐỘNG và GatherHud, đồng thời nối ActionButton vào MobileInput. Các HUD đã có cùng tên sẽ được tái sử dụng.",
                    "Tạo / cập nhật", "Hủy"))
                return;

            Transform canvasRoot = SafeAreaFitter.GetOrCreate(canvas.transform);
            if (canvasRoot == null) canvasRoot = canvas.transform;
            GameObject actionObject = FindDirectChild(canvasRoot, "ActionButton");
            if (actionObject == null)
            {
                actionObject = new GameObject("ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(actionObject, "Create Action Button");
                actionObject.transform.SetParent(canvasRoot, false);
            }
            else
            {
                Undo.RecordObject(actionObject.transform, "Update Action Button Layout");
            }

            RectTransform actionRect = actionObject.GetComponent<RectTransform>();
            SetRect(actionRect, Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(-470f, 250f), new Vector2(220f, 220f));
            Image actionImage = actionObject.GetComponent<Image>();
            if (actionImage == null) actionImage = Undo.AddComponent<Image>(actionObject);
            actionImage.color = new Color(0.1f, 0.34f, 0.43f, 0.9f);
            actionImage.raycastTarget = true;

            ActionButton action = actionObject.GetComponent<ActionButton>();
            if (action == null) action = Undo.AddComponent<ActionButton>(actionObject);
            HudElement actionHudElement = actionObject.GetComponent<HudElement>();
            if (actionHudElement == null) actionHudElement = Undo.AddComponent<HudElement>(actionObject);
            SetHudElement(actionHudElement, "action", true);

            Text actionLabel = GetOrCreateText("Label", actionObject.transform);
            actionLabel.text = "TÁC ĐỘNG";
            if (actionLabel.font == null) actionLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            actionLabel.fontSize = 26;
            actionLabel.alignment = TextAnchor.MiddleCenter;
            actionLabel.color = Color.white;
            actionLabel.raycastTarget = false;
            SetRect(actionLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            actionObject.transform.SetAsLastSibling();
            Assign(input, "actionButton", action);

            GameObject hudObject = FindDirectChild(canvasRoot, "GatherHud");
            if (hudObject == null)
            {
                hudObject = new GameObject("GatherHud", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(hudObject, "Create Gather HUD");
                hudObject.transform.SetParent(canvasRoot, false);
            }

            GatherHud hud = hudObject.GetComponent<GatherHud>();
            if (hud == null) hud = Undo.AddComponent<GatherHud>(hudObject);
            RectTransform hudRect = hudObject.GetComponent<RectTransform>();
            SetRect(hudRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Text prompt = GetOrCreateTextChild(hudObject.transform, "PromptText");
            prompt.text = "";
            if (prompt.font == null) prompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            prompt.fontSize = 36;
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.color = Color.white;
            prompt.raycastTarget = false;
            SetRect(prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 60f));
            if (prompt.GetComponent<Outline>() == null) Undo.AddComponent<Outline>(prompt.gameObject);

            GameObject progress = GetOrCreateImageChild(hudObject.transform, "ProgressBar", new Color(0.04f, 0.06f, 0.08f, 0.88f), false);
            SetRect(progress.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(400f, 20f));
            Image progressFill = GetOrCreateImageChild(progress.transform, "Fill", new Color(0.25f, 0.85f, 0.45f, 1f), false).GetComponent<Image>();
            SetRect(progressFill.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            progress.SetActive(false);

            GameObject toolRoot = GetOrCreateImageChild(hudObject.transform, "ToolInfo", new Color(0.04f, 0.06f, 0.08f, 0.88f), false);
            SetRect(toolRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(300f, 60f));
            Text toolName = GetOrCreateTextChild(toolRoot.transform, "Name");
            if (toolName.font == null) toolName.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            toolName.fontSize = 22;
            toolName.alignment = TextAnchor.MiddleCenter;
            toolName.raycastTarget = false;
            SetRect(toolName.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            GameObject toolBar = GetOrCreateImageChild(toolRoot.transform, "Bar", new Color(0.08f, 0.1f, 0.12f, 1f), false);
            SetRect(toolBar.GetComponent<RectTransform>(), new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.34f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image toolFill = GetOrCreateImageChild(toolBar.transform, "Fill", new Color(0.3f, 0.85f, 0.3f, 1f), false).GetComponent<Image>();
            SetRect(toolFill.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            toolRoot.SetActive(false);

            Text toast = GetOrCreateTextChild(hudObject.transform, "ToastText");
            if (toast.font == null) toast.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            toast.fontSize = 30;
            toast.alignment = TextAnchor.UpperRight;
            toast.color = Color.white;
            toast.raycastTarget = false;
            SetRect(toast.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-250f, 100f), new Vector2(400f, 200f));

            Assign(hud, "promptText", prompt);
            Assign(hud, "progressRoot", progress);
            Assign(hud, "progressFill", progressFill.rectTransform);
            Assign(hud, "toolRoot", toolRoot);
            Assign(hud, "toolText", toolName);
            Assign(hud, "toolFill", toolFill.rectTransform);
            Assign(hud, "toolFillImage", toolFill);
            Assign(hud, "toastText", toast);

            EditorUtility.SetDirty(action);
            EditorUtility.SetDirty(actionHudElement);
            EditorUtility.SetDirty(input);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
            EditorUtility.DisplayDialog("Đảo Sương", "Đã tạo/nối ActionButton và GatherHud. Hãy chạy menu Giai đoạn 4 để nâng cấp Inventory sau khi dữ liệu GatherSettings đã tồn tại.", "OK");
        }

        [MenuItem("Đảo Sương/Giai đoạn 3/2 - Kiểm tra Action + Gather HUD")]
        public static void ValidateHud()
        {
            List<string> issues = new List<string>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            MobileInput input = Object.FindFirstObjectByType<MobileInput>();
            if (canvas == null) issues.Add("Thiếu Canvas.");
            if (input == null) issues.Add("Thiếu MobileInput.");
            else if (GetReference(input, "actionButton") == null) issues.Add("MobileInput chưa được gán ActionButton.");
            if (canvas != null)
            {
                Transform safeRoot = canvas.transform.Find("MobileSafeAreaRoot");
                Transform hudRoot = safeRoot != null ? safeRoot : canvas.transform;
                Transform action = hudRoot.Find("ActionButton");
                if (action == null) action = canvas.transform.Find("ActionButton");
                if (action == null || action.GetComponent<ActionButton>() == null)
                    issues.Add("Thiếu ActionButton trong Canvas.");
                else if (action.GetComponent<Image>() == null || !action.GetComponent<Image>().raycastTarget)
                    issues.Add("Image của ActionButton phải bật Raycast Target.");

                Transform hud = hudRoot.Find("GatherHud");
                if (hud == null) hud = canvas.transform.Find("GatherHud");
                if (hud == null || hud.GetComponent<GatherHud>() == null)
                    issues.Add("Thiếu GatherHud trong Canvas.");
                else
                {
                    GatherHud gatherHud = hud.GetComponent<GatherHud>();
                    string[] fields = { "promptText", "progressRoot", "progressFill", "toolRoot", "toolText", "toolFill", "toolFillImage", "toastText" };
                    foreach (string field in fields)
                        if (GetReference(gatherHud, field) == null)
                            issues.Add("GatherHud thiếu reference: " + field);
                }
            }
            EditorUtility.DisplayDialog("Đảo Sương - Gather HUD",
                issues.Count == 0 ? "Không phát hiện thiếu cấu hình ActionButton/GatherHud." : string.Join("\n", issues), "OK");
        }

        private static GameObject FindDirectChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child.gameObject : null;
        }

        private static Text GetOrCreateTextChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Text existingText = existing.GetComponent<Text>();
                if (existingText == null) existingText = Undo.AddComponent<Text>(existing.gameObject);
                if (existingText.font == null) existingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                existingText.raycastTarget = false;
                return existingText;
            }
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Text GetOrCreateText(string name, Transform parent) => GetOrCreateTextChild(parent, name);

        private static GameObject GetOrCreateImageChild(Transform parent, string name, Color color, bool raycast)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : null;
            if (go == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                go.transform.SetParent(parent, false);
            }
            Image image = go.GetComponent<Image>();
            if (image == null) image = Undo.AddComponent<Image>(go);
            image.color = color;
            image.raycastTarget = raycast;
            return go;
        }


        private static void SetHudElement(HudElement element, string id, bool customizable)
        {
            SerializedObject serialized = new SerializedObject(element);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("customizable").boolValue = customizable;
            serialized.ApplyModifiedProperties();
        }

        private static void Assign(Object component, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static Object GetReference(Object component, string propertyName)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue : null;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
#endif

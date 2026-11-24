#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Game.Gameplay;
using Game.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>
    /// Creates the default Phase 2 assets and a runnable sample scene, wiring the Phase 1
    /// controls so the generated island can be tested without manually assigning every field.
    /// </summary>
    public static class Phase2WorldTools
    {
        private const string ArtRoot = "Assets/_Game/Art";
        private const string MaterialRoot = "Assets/_Game/Art/Materials";
        private const string SettingsRoot = "Assets/_Game/Data/World";
        private const string SceneRoot = "Assets/_Game/Scenes/World";
        private const string ScenePath = SceneRoot + "/SCN_IslandWorld.unity";

        private sealed class WorldAssets
        {
            public IslandSettings settings;
            public Material terrain;
            public Material water;
            public Material rain;
            public Material player;
        }

        [MenuItem("Đảo Sương/Phase 2/1 - Create World Assets")]
        public static void CreateWorldAssetsMenu()
        {
            WorldAssets assets = CreateWorldAssets();
            if (assets != null)
                EditorUtility.DisplayDialog("Đảo Sương", "Đã tạo/kiểm tra IslandSettings và 4 Material mặc định.", "OK");
        }

        [MenuItem("Đảo Sương/Phase 2/2 - Create Playable Island Scene")]
        public static void CreatePlayableIslandSceneMenu()
        {
            WorldAssets assets = CreateWorldAssets();
            if (assets == null)
                return;

            CreatePlayableIslandScene(assets);
            EditorUtility.DisplayDialog("Đảo Sương", "Đã tạo SCN_IslandWorld. Mở Scene và nhấn Play để sinh đảo.", "OK");
        }

        [MenuItem("Đảo Sương/Phase 2/3 - Validate Open Scene")]
        public static void ValidateOpenSceneMenu()
        {
            List<string> errors = ValidateOpenScene();
            if (errors.Count == 0)
                EditorUtility.DisplayDialog("Đảo Sương", "Không tìm thấy lỗi thiếu tham chiếu World/Island/Water/Weather trong Scene đang mở.", "OK");
            else
                EditorUtility.DisplayDialog("Đảo Sương - Cần sửa", string.Join("\n", errors), "OK");
        }

        private static WorldAssets CreateWorldAssets()
        {
            EnsureFolder("Assets/_Game/Art", "Materials");
            EnsureFolder("Assets/_Game/Data", "World");
            EnsureFolder("Assets/_Game/Scenes", "World");
            AssetDatabase.Refresh();

            WorldAssets assets = new WorldAssets();
            assets.settings = AssetDatabase.LoadAssetAtPath<IslandSettings>(SettingsRoot + "/IslandSettings.asset");
            if (assets.settings == null)
            {
                assets.settings = ScriptableObject.CreateInstance<IslandSettings>();
                AssetDatabase.CreateAsset(assets.settings, SettingsRoot + "/IslandSettings.asset");
            }

            assets.terrain = CreateOrUpdateMaterial("M_Terrain", "Game/LowPolyVertexColor", true);
            assets.water = CreateOrUpdateMaterial("M_Water", "Game/LowPolyWater", false);
            assets.rain = CreateOrUpdateMaterial("M_Rain", "Universal Render Pipeline/Particles/Unlit", false);
            assets.player = CreateOrUpdateMaterial("M_Player", "Universal Render Pipeline/Lit", false);

            if (assets.terrain == null || assets.water == null || assets.rain == null || assets.player == null)
            {
                Debug.LogError("[Phase 2] Không thể tạo một hoặc nhiều Material. Kiểm tra Console và URP package.");
                return null;
            }

            assets.terrain.enableInstancing = true;
            if (assets.player.HasProperty("_BaseColor"))
                assets.player.SetColor("_BaseColor", new Color(0.16f, 0.55f, 0.62f));
            if (assets.rain.HasProperty("_BaseColor"))
                assets.rain.SetColor("_BaseColor", new Color(0.82f, 0.88f, 1f, 0.55f));
            if (assets.rain.HasProperty("_Surface"))
                assets.rain.SetFloat("_Surface", 1f);
            if (assets.rain.HasProperty("_Blend"))
                assets.rain.SetFloat("_Blend", 0f);

            EditorUtility.SetDirty(assets.settings);
            EditorUtility.SetDirty(assets.terrain);
            EditorUtility.SetDirty(assets.water);
            EditorUtility.SetDirty(assets.rain);
            EditorUtility.SetDirty(assets.player);
            AssetDatabase.SaveAssets();
            return assets;
        }

        private static Material CreateOrUpdateMaterial(string materialName, string shaderName, bool enableInstancing)
        {
            string path = MaterialRoot + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[Phase 2] Không tìm thấy shader: " + shaderName);
                return null;
            }

            if (material == null)
            {
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.enableInstancing = enableInstancing;
            return material;
        }

        private static void CreatePlayableIslandScene(WorldAssets assets)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "Đảo Sương",
                    "SCN_IslandWorld đã tồn tại. Thay thế Scene cũ bằng Scene mẫu mới?",
                    "Thay thế", "Hủy"))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            int playerLayer = EnsurePlayerLayer();
            if (playerLayer < 0)
            {
                EditorUtility.DisplayDialog("Đảo Sương", "Không tạo được layer Player. Hãy tạo layer Player thủ công rồi chạy lại tool.", "OK");
                return;
            }

            // Directional light
            GameObject sunObject = new GameObject("Directional Light");
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Hard;
            sunObject.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.8f);
            RenderSettings.skybox = Resources.GetBuiltinResource<Material>("Default-Skybox.mat");
            RenderSettings.fog = true;

            // Player root follows the Phase 1 pivot-at-feet convention.
            GameObject playerObject = new GameObject("Player");
            playerObject.tag = "Player";
            SetLayerRecursively(playerObject, playerLayer);
            CharacterController characterController = playerObject.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.center = new Vector3(0f, 1f, 0f);
            characterController.slopeLimit = 48f;
            characterController.stepOffset = 0.35f;
            characterController.skinWidth = 0.08f;
            playerObject.AddComponent<PlayerController>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(playerObject.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);
            visual.GetComponent<Renderer>().sharedMaterial = assets.player;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            SetLayerRecursively(visual, playerLayer);

            // Camera
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.farClipPlane = 250f;
            camera.allowMSAA = false;
            ThirdPersonCamera thirdPersonCamera = cameraObject.AddComponent<ThirdPersonCamera>();
            SetObjectReference(thirdPersonCamera, "target", playerObject.transform);
            SetInteger(thirdPersonCamera, "collisionMask", ~(1 << playerLayer));

            // World systems stay together at origin. WaterSurface owns a separate following child.
            GameObject worldObject = new GameObject("World");
            IslandGenerator island = worldObject.AddComponent<IslandGenerator>();
            WaterSurface water = worldObject.AddComponent<WaterSurface>();
            DayNightCycle day = worldObject.AddComponent<DayNightCycle>();
            WeatherSystem weather = worldObject.AddComponent<WeatherSystem>();
            WorldDebugPanel debugPanel = worldObject.AddComponent<WorldDebugPanel>();

            SetObjectReference(island, "settings", assets.settings);
            SetInteger(island, "seed", 12345);
            SetObjectReference(island, "terrainMaterial", assets.terrain);
            SetObjectReference(island, "player", playerObject.transform);
            SetBoolean(island, "generateOnStart", true);

            SetObjectReference(water, "waterMaterial", assets.water);
            SetObjectReference(water, "follow", cameraObject.transform);
            SetFloat(water, "extent", 400f);

            SetObjectReference(day, "sun", sun);
            SetObjectReference(day, "weather", weather);
            SetFloat(day, "dayLengthSeconds", 720f);
            SetFloat(day, "startTime01", 0.28f);

            SetObjectReference(weather, "followTarget", cameraObject.transform);
            SetObjectReference(weather, "rainMaterial", assets.rain);
            SetObjectReference(weather, "island", island);
            SetObjectReference(weather, "player", playerObject.transform);

            SetObjectReference(debugPanel, "day", day);
            SetObjectReference(debugPanel, "weather", weather);
            SetObjectReference(debugPanel, "island", island);

            SetObjectReference(playerObject.GetComponent<PlayerController>(), "cameraTransform", cameraObject.transform);

            CreateMobileHud(playerObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool alreadyAdded = false;
            foreach (EditorBuildSettingsScene buildScene in buildScenes)
            {
                if (buildScene.path == ScenePath)
                {
                    alreadyAdded = true;
                    buildScene.enabled = true;
                    break;
                }
            }
            if (!alreadyAdded)
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            Selection.activeObject = worldObject;
            Debug.Log("[Phase 2] Created playable island scene: " + ScenePath);
        }

        private static void CreateMobileHud(GameObject playerObject)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>(); // default actions are assigned by OnEnable

            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform safeRoot = SafeAreaFitter.GetOrCreate(canvasObject.transform);
            if (safeRoot == null) safeRoot = canvasObject.transform;

            // Look zone covers the right half, and is mirrored by HudElement in left-hand mode.
            GameObject lookObject = CreateImageObject("LookZone", safeRoot, new Color(1f, 1f, 1f, 0f));
            RectTransform lookRect = lookObject.GetComponent<RectTransform>();
            lookRect.anchorMin = new Vector2(0.5f, 0f);
            lookRect.anchorMax = new Vector2(1f, 1f);
            lookRect.offsetMin = Vector2.zero;
            lookRect.offsetMax = Vector2.zero;
            lookObject.AddComponent<TouchLookZone>();
            ConfigureHudElement(lookObject.AddComponent<HudElement>(), "look", false);

            // Left thumb joystick.
            GameObject joystickObject = CreateImageObject("Joystick", safeRoot, new Color(0.12f, 0.18f, 0.22f, 0.42f));
            RectTransform joystickRect = joystickObject.GetComponent<RectTransform>();
            SetRect(joystickRect, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(260f, 260f), new Vector2(300f, 300f));
            GameObject handleObject = CreateImageObject("Handle", joystickObject.transform, new Color(0.85f, 0.93f, 0.95f, 0.85f));
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            SetRect(handleRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));
            handleObject.GetComponent<Image>().raycastTarget = false;
            VirtualJoystick virtualJoystick = joystickObject.AddComponent<VirtualJoystick>();
            SetObjectReference(virtualJoystick, "handle", handleRect);
            ConfigureHudElement(joystickObject.AddComponent<HudElement>(), "joystick", true);

            // Tap-to-jump; intentionally no Unity UI Button component.
            GameObject jumpObject = CreateImageObject("JumpButton", safeRoot, new Color(0.15f, 0.35f, 0.46f, 0.82f));
            RectTransform jumpRect = jumpObject.GetComponent<RectTransform>();
            SetRect(jumpRect, Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-200f, 260f), new Vector2(180f, 180f));
            CreateText("JumpLabel", jumpObject.transform, "JUMP", 28, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(160f, 70f));
            jumpObject.AddComponent<JumpButton>();
            ConfigureHudElement(jumpObject.AddComponent<HudElement>(), "jump", true);

            GameObject inputObject = new GameObject("Input");
            MobileInput mobileInput = inputObject.AddComponent<MobileInput>();
            SetObjectReference(mobileInput, "joystick", virtualJoystick);
            SetObjectReference(mobileInput, "lookZone", lookObject.GetComponent<TouchLookZone>());
            SetObjectReference(mobileInput, "jumpButton", jumpObject.GetComponent<JumpButton>());

            // HUD edit panel
            GameObject editController = new GameObject("EditController");
            editController.transform.SetParent(safeRoot, false);
            HudEditPanel editPanel = editController.AddComponent<HudEditPanel>();
            GameObject panelObject = CreateImageObject("Panel", editController.transform, new Color(0.035f, 0.06f, 0.075f, 0.96f));
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            SetRect(panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(520f, 390f));
            CreateText("Title", panelObject.transform, "CHỈNH GIAO DIỆN", 27, TextAnchor.MiddleCenter, new Vector2(0f, -32f), new Vector2(470f, 45f));

            Toggle leftToggle = CreateToggle(panelObject.transform, "LeftHandedToggle", "Tay trái", new Vector2(0f, -88f));
            Slider scaleSlider = CreateSlider(panelObject.transform, "ScaleSlider", "Kích cỡ nút", 0.5f, 2f, 1f, new Vector2(0f, -160f));
            Slider sensitivitySlider = CreateSlider(panelObject.transform, "SensitivitySlider", "Độ nhạy camera", 0.05f, 0.4f, 0.15f, new Vector2(0f, -232f));
            Button resetButton = CreateButton(panelObject.transform, "ResetButton", "ĐẶT LẠI", new Vector2(-125f, -330f), new Vector2(180f, 54f));
            Button doneButton = CreateButton(panelObject.transform, "DoneButton", "XONG", new Vector2(125f, -330f), new Vector2(180f, 54f));
            panelObject.SetActive(false);

            SetObjectReference(editPanel, "panel", panelObject);
            SetObjectReference(editPanel, "leftHandedToggle", leftToggle);
            SetObjectReference(editPanel, "scaleSlider", scaleSlider);
            SetObjectReference(editPanel, "sensitivitySlider", sensitivitySlider);
            UnityEventTools.AddPersistentListener(leftToggle.onValueChanged, editPanel.SetLeftHanded);
            UnityEventTools.AddPersistentListener(scaleSlider.onValueChanged, editPanel.SetScale);
            UnityEventTools.AddPersistentListener(sensitivitySlider.onValueChanged, editPanel.SetSensitivity);
            UnityEventTools.AddPersistentListener(resetButton.onClick, editPanel.ResetAll);
            UnityEventTools.AddPersistentListener(doneButton.onClick, editPanel.Done);

            // Gear button lives opposite the debug panel and mirrors with the handedness layout.
            Button gearButton = CreateButton(safeRoot, "HudEditButton", "HUD ⚙", new Vector2(-95f, -75f), new Vector2(150f, 62f));
            RectTransform gearRect = gearButton.GetComponent<RectTransform>();
            gearRect.anchorMin = Vector2.one;
            gearRect.anchorMax = Vector2.one;
            gearRect.pivot = new Vector2(0.5f, 0.5f);
            gearRect.anchoredPosition = new Vector2(-95f, -75f);
            HudElement gearElement = gearButton.gameObject.AddComponent<HudElement>();
            ConfigureHudElement(gearElement, "gear", false);
            UnityEventTools.AddPersistentListener(gearButton.onClick, editPanel.OpenEdit);

            // Ensure EventSystem and Canvas remain at the end of scene root for easy inspection.
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create Phase 2 EventSystem");
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Phase 2 Mobile HUD");
        }

        private static GameObject CreateImageObject(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return go;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label, Vector2 position)
        {
            GameObject go = CreateImageObject(name, parent, new Color(0.1f, 0.16f, 0.2f, 0.9f));
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), position, new Vector2(430f, 48f));
            Toggle toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = go.GetComponent<Image>();

            GameObject mark = CreateImageObject("Checkmark", go.transform, new Color(0.25f, 0.85f, 0.55f, 1f));
            RectTransform markRect = mark.GetComponent<RectTransform>();
            SetRect(markRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(26f, 26f));
            toggle.graphic = mark.GetComponent<Image>();
            mark.SetActive(false);
            UnityEventTools.AddPersistentListener(toggle.onValueChanged, mark.SetActive);
            CreateText("Label", go.transform, label, 22, TextAnchor.MiddleLeft, new Vector2(35f, 0f), new Vector2(350f, 44f));
            return toggle;
        }

        private static Slider CreateSlider(Transform parent, string name, string label, float min, float max, float value, Vector2 position)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetRect(rootRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), position, new Vector2(450f, 64f));
            CreateText("Label", root.transform, label, 20, TextAnchor.MiddleLeft, new Vector2(0f, 15f), new Vector2(420f, 28f));

            GameObject track = CreateImageObject("Track", root.transform, new Color(0.18f, 0.24f, 0.28f, 1f));
            RectTransform trackRect = track.GetComponent<RectTransform>();
            SetRect(trackRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(390f, 12f));

            GameObject fill = CreateImageObject("Fill", track.transform, new Color(0.2f, 0.75f, 0.85f, 1f));
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            SetRect(fillRect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            GameObject handle = CreateImageObject("Handle", track.transform, new Color(0.9f, 0.96f, 1f, 1f));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            SetRect(handleRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));

            Slider slider = root.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.ColorTint;
            return slider;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            GameObject go = CreateImageObject(name, parent, new Color(0.1f, 0.34f, 0.43f, 0.94f));
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), position, size);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            Text text = CreateText("Label", go.transform, label, 20, TextAnchor.MiddleCenter, Vector2.zero, size);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private static void ConfigureHudElement(HudElement element, string id, bool customizable)
        {
            SetString(element, "id", id);
            SetBoolean(element, "customizable", customizable);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[Phase 2] Serialized field '{propertyName}' was not found on {target.GetType().Name}.");
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInteger(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolean(Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static int EnsurePlayerLayer()
        {
            int existingLayer = LayerMask.NameToLayer("Player");
            if (existingLayer >= 0)
                return existingLayer;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[Phase 2] Không tìm thấy TagManager để tạo layer Player. Hãy tạo layer Player thủ công.");
                return -1;
            }

            SerializedObject manager = new SerializedObject(assets[0]);
            SerializedProperty layers = manager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = "Player";
                    manager.ApplyModifiedProperties();
                    EditorUtility.SetDirty(assets[0]);
                    AssetDatabase.SaveAssets();
                    return i;
                }
            }
            Debug.LogWarning("[Phase 2] Không còn slot layer trống để tạo Player. Hãy tạo layer thủ công.");
            return -1;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static List<string> ValidateOpenScene()
        {
            List<string> errors = new List<string>();
            GameObject worldObject = GameObject.Find("World");
            if (worldObject == null)
            {
                errors.Add("- Không tìm thấy GameObject World.");
                return errors;
            }

            IslandGenerator island = worldObject.GetComponent<IslandGenerator>();
            WaterSurface water = worldObject.GetComponent<WaterSurface>();
            DayNightCycle day = worldObject.GetComponent<DayNightCycle>();
            WeatherSystem weather = worldObject.GetComponent<WeatherSystem>();
            WorldDebugPanel debugPanel = worldObject.GetComponent<WorldDebugPanel>();
            if (island == null) errors.Add("- World thiếu IslandGenerator.");
            if (water == null) errors.Add("- World thiếu WaterSurface.");
            if (day == null) errors.Add("- World thiếu DayNightCycle.");
            if (weather == null) errors.Add("- World thiếu WeatherSystem.");
            if (debugPanel == null) errors.Add("- World thiếu WorldDebugPanel.");

            Camera mainCamera = Camera.main;
            if (mainCamera == null) errors.Add("- Không tìm thấy MainCamera có tag MainCamera.");
            if (GameObject.Find("Player") == null) errors.Add("- Không tìm thấy GameObject Player.");
            if (Object.FindObjectOfType<EventSystem>() == null) errors.Add("- Scene thiếu EventSystem.");
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null) errors.Add("- URP chưa được gán trong Project Settings > Graphics.");
            if (island != null && water != null && worldObject.transform.position != Vector3.zero)
                errors.Add("- World phải ở Position (0,0,0) để dùng cấu hình mặc định.");
            return errors;
        }
    }
}
#endif

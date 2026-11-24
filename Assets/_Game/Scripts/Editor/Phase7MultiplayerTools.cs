#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using Game.Network;
using Game.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class Phase7MultiplayerTools
{
    private const string NetworkPrefabFolder = "Assets/_Game/Prefabs/Network";
    private const string NetworkPrefabPath = NetworkPrefabFolder + "/NetworkPlayerAvatar.prefab";

    [MenuItem("Game/Giai đoạn 7/1 - Tạo Network Player Prefab")]
    public static void CreatePlayerPrefab()
    {
        EnsureFolders();
        GameObject prefab = CreateOrLoadNetworkPrefab();
        Selection.activeObject = prefab;
        Debug.Log("[Phase 7] Đã tạo NetworkPlayerAvatar prefab: " + NetworkPrefabPath);
    }

    [MenuItem("Game/Giai đoạn 7/2 - Nâng cấp Scene hiện tại")]
    public static void UpgradeCurrentScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EnsureFolders();
        GameObject prefab = CreateOrLoadNetworkPrefab();

        NetworkManager networkManager = Object.FindFirstObjectByType<NetworkManager>();
        if (networkManager == null)
        {
            var managerObject = new GameObject("NetworkManager");
            Undo.RegisterCreatedObjectUndo(managerObject, "Create NetworkManager");
            networkManager = Undo.AddComponent<NetworkManager>(managerObject);
        }

        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null) transport = Undo.AddComponent<UnityTransport>(networkManager.gameObject);
        Undo.RecordObject(networkManager, "Configure NGO NetworkManager");
        Undo.RecordObject(transport, "Configure Unity Transport");
        networkManager.NetworkConfig.NetworkTransport = transport;
        networkManager.NetworkConfig.PlayerPrefab = prefab;
        networkManager.NetworkConfig.TickRate = 20;
        networkManager.NetworkConfig.EnableSceneManagement = true;

        GameObject services = GameObject.Find("OnlineServices");
        if (services == null)
        {
            services = new GameObject("OnlineServices");
            Undo.RegisterCreatedObjectUndo(services, "Create Online Services");
        }
        if (services.GetComponent<NetworkSessionManager>() == null) Undo.AddComponent<NetworkSessionManager>(services);
        if (services.GetComponent<VivoxVoiceManager>() == null) Undo.AddComponent<VivoxVoiceManager>(services);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("OnlineCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Online Canvas");
        }
        if (canvas.GetComponent<OnlineSessionHUD>() == null) Undo.AddComponent<OnlineSessionHUD>(canvas.gameObject);

        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventObject, "Create Input System EventSystem");
        }
        else
        {
            var legacyInput = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyInput != null) Object.DestroyImmediate(legacyInput, true);
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
        }

        PlayerSettings.iOS.microphoneUsageDescription = "Đảo Sương sử dụng microphone để bạn trò chuyện bằng giọng nói với đồng đội trong phòng chơi.";
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EnsureActiveSceneInBuildSettings();
        Debug.Log("[Phase 7] Scene đã được nâng cấp và thêm vào Build Settings. Network tick=20 Hz, room max=4, voice=PUSH-TO-TALK.");
    }

    [MenuItem("Game/Giai đoạn 7/3 - Validate Multiplayer Scene")]
    public static void ValidateScene()
    {
        int errors = 0;
        NetworkManager manager = Object.FindFirstObjectByType<NetworkManager>();
        if (manager == null) { Debug.LogError("[Phase 7] Thiếu NetworkManager."); errors++; }
        else
        {
            if (manager.GetComponent<UnityTransport>() == null) { Debug.LogError("[Phase 7] Thiếu UnityTransport."); errors++; }
            if (manager.NetworkConfig.PlayerPrefab == null) { Debug.LogError("[Phase 7] Chưa gán PlayerPrefab."); errors++; }
            else if (manager.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>() == null || manager.NetworkConfig.PlayerPrefab.GetComponent<NetworkPlayerAvatar>() == null || manager.NetworkConfig.PlayerPrefab.GetComponent<NetworkTransform>() == null)
            { Debug.LogError("[Phase 7] Network Player Prefab cần NetworkObject, NetworkTransform và NetworkPlayerAvatar."); errors++; }
        }

        if (Object.FindFirstObjectByType<NetworkSessionManager>() == null) { Debug.LogError("[Phase 7] Thiếu NetworkSessionManager."); errors++; }
        if (Object.FindFirstObjectByType<VivoxVoiceManager>() == null) { Debug.LogError("[Phase 7] Thiếu VivoxVoiceManager."); errors++; }
        if (Object.FindFirstObjectByType<OnlineSessionHUD>() == null) { Debug.LogError("[Phase 7] Thiếu OnlineSessionHUD."); errors++; }
        Scene activeScene = SceneManager.GetActiveScene();
        bool sceneInBuild = !string.IsNullOrEmpty(activeScene.path) && Array.Exists(EditorBuildSettings.scenes, s => s.enabled && s.path == activeScene.path);
        if (!sceneInBuild) { Debug.LogError("[Phase 7] Scene hiện tại chưa được thêm vào Build Settings."); errors++; }
        if (GameObject.FindGameObjectWithTag("Player") == null) { Debug.LogError("[Phase 7] Không tìm thấy GameObject tag Player."); errors++; }

        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null || eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            Debug.LogError("[Phase 7] Cần EventSystem với InputSystemUIInputModule để dùng UI cảm ứng.");
            errors++;
        }

        string androidManifest = Path.Combine(Application.dataPath, "Plugins", "Android", "AndroidManifest.xml");
        if (!File.Exists(androidManifest) || !File.ReadAllText(androidManifest).Contains("android.permission.RECORD_AUDIO"))
        {
            Debug.LogError("[Phase 7] AndroidManifest thiếu RECORD_AUDIO.");
            errors++;
        }
        if (string.IsNullOrWhiteSpace(PlayerSettings.iOS.microphoneUsageDescription))
        {
            Debug.LogError("[Phase 7] iOS thiếu NSMicrophoneUsageDescription.");
            errors++;
        }

        if (errors == 0) Debug.Log("[Phase 7] Validate passed. Cần link Unity Dashboard, bật Authentication/Relay/Vivox và test 2 client thật.");
        else Debug.LogError($"[Phase 7] Validate có {errors} lỗi.");
    }


    private static void EnsureActiveSceneInBuildSettings()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            Debug.LogWarning("[Phase 7] Scene chưa có đường dẫn lưu; hãy Save Scene rồi chạy Validate lại.");
            return;
        }

        var entries = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int index = entries.FindIndex(entry => entry.path == scene.path);
        if (index >= 0)
        {
            if (!entries[index].enabled)
                entries[index] = new EditorBuildSettingsScene(scene.path, true);
        }
        else
        {
            entries.Add(new EditorBuildSettingsScene(scene.path, true));
        }
        EditorBuildSettings.scenes = entries.ToArray();
    }

    private static GameObject CreateOrLoadNetworkPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPrefabPath);
        if (existing != null) return existing;

        GameObject sourcePlayer = GameObject.FindGameObjectWithTag("Player");
        Material material = null;
        if (sourcePlayer != null)
        {
            var renderer = sourcePlayer.GetComponentInChildren<Renderer>();
            if (renderer != null) material = renderer.sharedMaterial;
        }

        GameObject root = new GameObject("NetworkPlayerAvatar");
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        if (material != null) visual.GetComponent<Renderer>().sharedMaterial = material;

        root.AddComponent<NetworkObject>();
        root.AddComponent<NetworkTransform>();
        root.AddComponent<NetworkPlayerAvatar>();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, NetworkPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return saved;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs")) AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");
        if (!AssetDatabase.IsValidFolder(NetworkPrefabFolder)) AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Network");
    }
}
#endif

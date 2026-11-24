#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.EditorTools;

/// <summary>
/// One place to assemble the cumulative Dao Suong phases into one local playable scene.
/// The existing phase tools remain the source of truth for creating data and wiring components.
/// </summary>
public sealed class DaoSuongFullGameSetupWizard : EditorWindow
{
    private Vector2 scroll;
    private const string MainScene = "Assets/_Game/Scenes/World/SCN_IslandWorld.unity";

    [MenuItem("Đảo Sương/00 - Trình tạo game tổng hợp (Phase 0–9)")]
    public static void Open()
    {
        DaoSuongFullGameSetupWizard window = GetWindow<DaoSuongFullGameSetupWizard>("Đảo Sương - Full Game");
        window.minSize = new Vector2(520f, 650f);
        window.Show();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ĐẢO SƯƠNG — TRÌNH TẠO GAME TỔNG HỢP", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Project hợp nhất Phase 0–9. Hãy chạy các bước theo thứ tự trên một scene mới hoặc scene đã sao lưu. " +
            "Các công cụ cũ sẽ hỏi xác nhận khi cần tạo/cập nhật HUD hoặc thay scene.", MessageType.Info);

        DrawProjectStatus();
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("A. NỀN TẢNG PROJECT (PHASE 0–1)", EditorStyles.boldLabel);
        if (GUILayout.Button("Tạo Bootstrap Scene trống (Phase 0, tùy chọn)", GUILayout.Height(28)))
            Run("Phase 0 Bootstrap", DaoSuong.Editor.Phase0ProjectTools.CreateBootstrapScene);
        if (GUILayout.Button("Kiểm tra cấu trúc assembly (Phase 0)", GUILayout.Height(28)))
            Run("Phase 0 Validate", DaoSuong.Editor.Phase0ProjectTools.ValidateProjectStructure);
        EditorGUILayout.HelpBox("Điều khiển nhân vật/mobile của Phase 1 được gắn trong Scene chơi do Phase 2 tạo ra.", MessageType.None);

        EditorGUILayout.LabelField("B. TẠO GAME SINH TỒN OFFLINE", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Đây là đường chạy chính để tạo scene chơi được: đảo, điều khiển, thu thập, túi đồ, chế tạo, sinh tồn, 99 ngày, boss, căn cứ, hang và mobile HUD.", MessageType.None);

        if (GUILayout.Button("1. Tạo thế giới và Scene chơi (Phase 2)", GUILayout.Height(32))) Run("Phase 2", () =>
        {
            Game.EditorTools.Phase2WorldTools.CreateWorldAssetsMenu();
            Game.EditorTools.Phase2WorldTools.CreatePlayableIslandSceneMenu();
        });
        if (GUILayout.Button("2. Tạo dữ liệu thu thập + HUD hành động (Phase 3)", GUILayout.Height(30))) Run("Phase 3", () =>
        {
            Stage3DataCreator.CreateSampleData();
            Game.EditorTools.Phase3GatherHudTools.CreateOrUpdateHud();
        });
        if (GUILayout.Button("3. Túi đồ, rương và chuyển vật phẩm (Phase 4)", GUILayout.Height(30))) Run("Phase 4", Phase4InventoryTools.UpgradeOpenScene);
        if (GUILayout.Button("4. Công thức và trạm chế tạo (Phase 5)", GUILayout.Height(30))) Run("Phase 5", () =>
        {
            Phase5CraftingTools.CreateSampleData();
            Phase5CraftingTools.UpgradeScene();
        });
        if (GUILayout.Button("5. Thanh sinh tồn và trạng thái người chơi (Phase 6)", GUILayout.Height(30))) Run("Phase 6", () =>
        {
            Game.EditorTools.Phase6SurvivalTools.CreateData();
            Game.EditorTools.Phase6SurvivalTools.UpgradeScene();
        });
        if (GUILayout.Button("6. Sáu đảo, quái, boss, chiến dịch 99 ngày, xây căn cứ (Phase 8)", GUILayout.Height(34))) Run("Phase 8", () =>
        {
            Game.EditorTools.Phase8CampaignTools.CreateCampaignContent();
            Game.EditorTools.Phase8CampaignTools.UpgradeCurrentScene();
        });
        if (GUILayout.Button("7. Hang động bí mật, vật phẩm, nhật ký và tối ưu mobile (Phase 9)", GUILayout.Height(34))) Run("Phase 9", () =>
        {
            Game.EditorTools.Phase9CaveTools.CreateCaveContent();
            Game.EditorTools.Phase9CaveTools.UpgradeScene();
        });

        EditorGUILayout.Space(8);
        if (GUILayout.Button("CHẠY TẤT CẢ BƯỚC OFFLINE THEO THỨ TỰ", GUILayout.Height(42)))
        {
            bool confirmed = EditorUtility.DisplayDialog("Tạo game Đảo Sương",
                "Chạy tuần tự Phase 2 → 3 → 4 → 5 → 6 → 8 → 9. Unity có thể hiển thị các hộp xác nhận tiếp theo. " +
                "Nếu scene SCN_IslandWorld đã tồn tại, chỉ chọn Thay thế khi bạn đã sao lưu scene đó. Tiếp tục?",
                "Bắt đầu", "Hủy");
            if (confirmed) RunAllOffline();
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("C. KIỂM TRA VÀ NGHIỆM THU", EditorStyles.boldLabel);
        if (GUILayout.Button("Kiểm tra toàn bộ reference và dữ liệu các Phase", GUILayout.Height(36))) ValidateAll();
        if (GUILayout.Button("Quét cấu trúc mã nguồn, asmdef, GUID và test count", GUILayout.Height(30))) AuditProjectFiles();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("D. ONLINE (TÙY CHỌN)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Phase 7 thêm tạo phòng Relay, avatar network và voice Vivox. Nó chưa đồng bộ gameplay authoritative: máu, túi đồ, boss, loot và công trình vẫn không đồng bộ giữa máy. " +
            "Chỉ cài khi đã link Unity Cloud Project và chuẩn bị cấu hình Authentication/Relay/Vivox.", MessageType.Warning);
        if (GUILayout.Button("Cài lớp kết nối phòng + voice (Phase 7)", GUILayout.Height(32)))
        {
            if (EditorUtility.DisplayDialog("Cài Phase 7 Online",
                "Lệnh này thêm NetworkManager, avatar proxy và voice UI vào scene đang mở. Cần cấu hình Unity Dashboard trước khi thử mạng. Tiếp tục?",
                "Cài Online", "Hủy"))
                Run("Phase 7 Online", () =>
                {
                    Phase7MultiplayerTools.CreatePlayerPrefab();
                    Phase7MultiplayerTools.UpgradeCurrentScene();
                });
        }
        if (GUILayout.Button("Kiểm tra cấu hình Online", GUILayout.Height(28))) Run("Validate Phase 7", Phase7MultiplayerTools.ValidateScene);

        EditorGUILayout.Space(12);
        EditorGUILayout.HelpBox(
            "Lưu ý: các bước trên sinh asset và Scene bên trong Unity Editor. ZIP source không chứa Scene/Prefab/ScriptableObject đã sinh sẵn. " +
            "Sau khi tạo xong, mở SCN_IslandWorld, nhấn Play, chạy EditMode Test Runner và build Android Development APK để nghiệm thu trên điện thoại thật.",
            MessageType.Warning);
        EditorGUILayout.EndScrollView();
    }

    private static void DrawProjectStatus()
    {
        bool hasScene = File.Exists(MainScene);
        bool hasManifest = File.Exists("Packages/manifest.json");
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Trạng thái project", EditorStyles.boldLabel);
        Status("Packages/manifest.json", hasManifest);
        Status("Scene SCN_IslandWorld", hasScene);
        Status("Unity Editor", Application.unityVersion.StartsWith("6000.3", StringComparison.Ordinal));
        EditorGUILayout.EndVertical();
    }

    private static void Status(string label, bool ok)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label);
        GUILayout.Label(ok ? "SẴN SÀNG" : "CHƯA TẠO / KIỂM TRA", GUILayout.Width(150f));
        EditorGUILayout.EndHorizontal();
    }

    private static void RunAllOffline()
    {
        if (!Run("Phase 2", () =>
        {
            Game.EditorTools.Phase2WorldTools.CreateWorldAssetsMenu();
            Game.EditorTools.Phase2WorldTools.CreatePlayableIslandSceneMenu();
        })) return;
        if (!File.Exists(MainScene) || !string.Equals(SceneManager.GetActiveScene().path, MainScene, StringComparison.Ordinal))
        { Stop("Phase 2 chưa mở được SCN_IslandWorld. Có thể bạn đã hủy thao tác thay Scene. Dừng tại đây để tránh nâng cấp nhầm Scene khác."); return; }
        if (!SceneHasComponent("PlayerController") || !SceneHasComponent("MobileInput") || !SceneHasComponent("IslandGenerator"))
        { Stop("Scene Phase 2 đang mở nhưng thiếu PlayerController, MobileInput hoặc IslandGenerator. Dừng để không nối các Phase tiếp theo vào một Scene chưa hoàn chỉnh."); return; }
        if (!Run("Phase 3", () => { Stage3DataCreator.CreateSampleData(); Game.EditorTools.Phase3GatherHudTools.CreateOrUpdateHud(); })) return;
        if (!SceneHasComponent("ActionButton")) { Stop("Phase 3 chưa tạo ActionButton. Hãy hoàn tất Phase 3 rồi chạy lại các bước tiếp theo."); return; }
        if (!Run("Phase 4", Game.EditorTools.Phase4InventoryTools.UpgradeOpenScene)) return;
        if (!SceneHasComponent("PlayerInventory")) { Stop("Phase 4 chưa gắn PlayerInventory. Hãy hoàn tất Phase 4 trước khi tiếp tục."); return; }
        if (!Run("Phase 5", () => { Phase5CraftingTools.CreateSampleData(); Phase5CraftingTools.UpgradeScene(); })) return;
        if (!SceneHasComponent("CraftingHUD")) { Stop("Phase 5 chưa gắn CraftingHUD. Hãy hoàn tất Phase 5 trước khi tiếp tục."); return; }
        if (!Run("Phase 6", () => { Game.EditorTools.Phase6SurvivalTools.CreateData(); Game.EditorTools.Phase6SurvivalTools.UpgradeScene(); })) return;
        if (!SceneHasComponent("SurvivalController")) { Stop("Phase 6 chưa gắn SurvivalController. Hãy hoàn tất Phase 6 trước khi tiếp tục."); return; }
        if (!Run("Phase 8", () => { Game.EditorTools.Phase8CampaignTools.CreateCampaignContent(); Game.EditorTools.Phase8CampaignTools.UpgradeCurrentScene(); })) return;
        if (!SceneHasComponent("CampaignManager") || !SceneHasComponent("ArchipelagoManager")) { Stop("Phase 8 chưa gắn CampaignManager/ArchipelagoManager. Hãy hoàn tất Phase 8 trước khi tiếp tục."); return; }
        if (!Run("Phase 9", () => { Game.EditorTools.Phase9CaveTools.CreateCaveContent(); Game.EditorTools.Phase9CaveTools.UpgradeScene(); })) return;
        if (!SceneHasComponent("CaveManager") || !SceneHasComponent("CaveJournalHUD")) { Stop("Phase 9 chưa gắn CaveManager/CaveJournalHUD. Hãy hoàn tất Phase 9."); return; }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DaoSuong] Đã kết thúc chuỗi tạo game offline. Hãy chạy 'Kiểm tra toàn bộ reference' và EditMode Test Runner; không tự coi đây là xác nhận Play Mode.");
        EditorUtility.DisplayDialog("Đảo Sương", "Đã chạy chuỗi tạo game offline. Tiếp theo hãy chạy kiểm tra cấu hình, mở Play Mode và chạy toàn bộ EditMode tests.", "OK");
    }

    private static bool Run(string phase, Action action)
    {
        if (action == null) { Stop("Không có thao tác cho " + phase + "."); return false; }
        if (EditorApplication.isPlaying) { Stop("Không chạy công cụ tạo Scene khi đang Play Mode."); return false; }
        try
        {
            action();
            AssetDatabase.SaveAssets();
            Debug.Log("[DaoSuong Full Setup] Đã gọi công cụ " + phase + ". Hãy kiểm tra hộp thoại/Console của Phase nếu có cảnh báo.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Lỗi khi chạy " + phase, ex.Message + "\n\nXem Console để biết chi tiết. Có thể sửa lỗi rồi chạy lại riêng bước này.", "Đóng");
            return false;
        }
    }

    private static bool SceneHasComponent(string typeName)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return false;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
                if (component != null && string.Equals(component.GetType().Name, typeName, StringComparison.Ordinal))
                    return true;
        }
        return false;
    }

    private static void Stop(string message)
    {
        Debug.LogError("[DaoSuong Full Setup] " + message);
        EditorUtility.DisplayDialog("Đảo Sương - Dừng tạo game", message, "Đóng");
    }

    private static void ValidateAll()
    {
        Run("Phase 2 validate", Game.EditorTools.Phase2WorldTools.ValidateOpenSceneMenu);
        Run("Phase 3 validate", Game.EditorTools.Phase3GatherHudTools.ValidateHud);
        Run("Phase 4 validate", Phase4InventoryTools.ValidateOpenScene);
        Run("Phase 5 validate", Phase5CraftingTools.ValidateScene);
        Run("Phase 6 validate", Game.EditorTools.Phase6SurvivalTools.ValidateScene);
        Run("Phase 8 validate", Game.EditorTools.Phase8CampaignTools.ValidateScene);
        Run("Phase 9 validate", Game.EditorTools.Phase9CaveTools.ValidateScene);
        Debug.Log("[DaoSuong] Đã gọi validator từng phase. Đọc từng hộp thoại và Console để biết bước nào còn thiếu.");
    }

    [Serializable]
    private sealed class AssemblyDefinitionAudit
    {
        public string name;
        public string[] references;
    }

    private static void AuditProjectFiles()
    {
        string assetsRoot = Application.dataPath;
        string[] files = Directory.GetFiles(assetsRoot, "*", SearchOption.AllDirectories);
        List<string> errors = new List<string>();
        HashSet<string> guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string[]> assemblyGraph = new Dictionary<string, string[]>(StringComparer.Ordinal);
        int csharp = 0, tests = 0, metaCount = 0, missingMeta = 0, orphanMeta = 0, duplicateGuid = 0, asmdefCount = 0;
        Regex guidPattern = new Regex(@"^guid:\s*([a-fA-F0-9]{32})\s*$", RegexOptions.Multiline);
        foreach (string file in files)
        {
            if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                metaCount++;
                string pairedPath = file.Substring(0, file.Length - 5);
                if (!File.Exists(pairedPath) && !Directory.Exists(pairedPath))
                {
                    orphanMeta++;
                    errors.Add("Meta không có asset/thư mục tương ứng: " + file);
                }
                Match match = guidPattern.Match(File.ReadAllText(file));
                if (!match.Success) { errors.Add("Meta không có GUID hợp lệ: " + file); continue; }
                if (!guids.Add(match.Groups[1].Value)) { duplicateGuid++; errors.Add("GUID trùng: " + match.Groups[1].Value); }
            }
            else
            {
                if (!File.Exists(file + ".meta")) missingMeta++;
                if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    csharp++;
                    string code = File.ReadAllText(file);
                    tests += Regex.Matches(code, @"\[(Test|TestCase|UnityTest)\b").Count;
                }
                else if (file.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
                {
                    asmdefCount++;
                    try
                    {
                        AssemblyDefinitionAudit definition = JsonUtility.FromJson<AssemblyDefinitionAudit>(File.ReadAllText(file));
                        if (definition == null || string.IsNullOrWhiteSpace(definition.name))
                            errors.Add("Assembly Definition thiếu tên hợp lệ: " + file);
                        else if (assemblyGraph.ContainsKey(definition.name))
                            errors.Add("Tên Assembly Definition bị trùng: " + definition.name);
                        else
                            assemblyGraph.Add(definition.name, definition.references ?? Array.Empty<string>());
                    }
                    catch (Exception exception)
                    {
                        errors.Add("Không đọc được Assembly Definition " + file + ": " + exception.Message);
                    }
                }
            }
        }
        foreach (string directory in Directory.GetDirectories(assetsRoot, "*", SearchOption.AllDirectories))
            if (!File.Exists(directory + ".meta")) missingMeta++;
        if (missingMeta > 0) errors.Add("Thiếu .meta trên " + missingMeta + " asset/file/folder.");

        foreach (KeyValuePair<string, string[]> assembly in assemblyGraph)
        {
            string[] references = assembly.Value ?? Array.Empty<string>();
            for (int i = 0; i < references.Length; i++)
            {
                string reference = references[i];
                if (!string.IsNullOrWhiteSpace(reference) && reference.StartsWith("Game.", StringComparison.Ordinal) &&
                    !assemblyGraph.ContainsKey(reference))
                    errors.Add("Assembly " + assembly.Key + " tham chiếu assembly nội bộ không tồn tại: " + reference);
            }
        }

        Dictionary<string, int> visitState = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string assemblyName in assemblyGraph.Keys)
        {
            string cycle;
            if (HasAssemblyCycle(assemblyName, assemblyGraph, visitState, new List<string>(), out cycle))
            {
                errors.Add("Vòng phụ thuộc Assembly Definition: " + cycle);
                break;
            }
        }

        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        int enabledScenes = 0;
        for (int i = 0; i < buildScenes.Length; i++)
        {
            if (!buildScenes[i].enabled) continue;
            enabledScenes++;
            string scenePath = buildScenes[i].path;
            string absoluteScenePath = Path.IsPathRooted(scenePath)
                ? scenePath
                : Path.Combine(Directory.GetParent(Application.dataPath).FullName, scenePath);
            if (!File.Exists(absoluteScenePath)) errors.Add("Build Settings trỏ tới Scene không tồn tại: " + scenePath);
        }

        string summary = "C# files: " + csharp + " | asmdef: " + asmdefCount + " | test attributes: " + tests +
                         " | GUID unique: " + guids.Count + " | meta: " + metaCount + " | missing meta: " + missingMeta +
                         " | orphan meta: " + orphanMeta + " | duplicate GUID: " + duplicateGuid +
                         " | enabled build scenes: " + enabledScenes + " | errors: " + errors.Count;
        if (errors.Count == 0) Debug.Log("[DaoSuong Audit] PASS (static structure/reference audit): " + summary);
        else Debug.LogError("[DaoSuong Audit] FAIL: " + summary + "\n" + string.Join("\n", errors));
        EditorUtility.DisplayDialog("Dao Suong Static Audit", summary +
            "\n\nĐã kiểm tra toàn bộ Assets, GUID/meta, tham chiếu assembly nội bộ, vòng phụ thuộc và Scene trong Build Settings. Đây không phải C# compile, Test Runner, Play Mode hoặc Android device test.", "OK");
    }

    private static bool HasAssemblyCycle(string name, Dictionary<string, string[]> graph,
        Dictionary<string, int> visitState, List<string> path, out string cycle)
    {
        cycle = null;
        if (visitState.TryGetValue(name, out int state))
        {
            if (state == 2) return false;
            if (state == 1)
            {
                int start = path.IndexOf(name);
                List<string> cyclePath = start >= 0 ? path.GetRange(start, path.Count - start) : new List<string>(path);
                cyclePath.Add(name);
                cycle = string.Join(" -> ", cyclePath);
                return true;
            }
        }

        visitState[name] = 1;
        path.Add(name);
        string[] references = graph[name] ?? Array.Empty<string>();
        for (int i = 0; i < references.Length; i++)
        {
            string reference = references[i];
            if (!string.IsNullOrWhiteSpace(reference) && graph.ContainsKey(reference) &&
                HasAssemblyCycle(reference, graph, visitState, path, out cycle)) return true;
        }
        path.RemoveAt(path.Count - 1);
        visitState[name] = 2;
        return false;
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using DaoSuong.Content;
using Game.Core;
using Game.Gameplay;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>Creates hidden caves, treasure, lore and configures mobile-safe runtime UI.</summary>
    public static class Phase9CaveTools
    {
        private const string Root = "Assets/_Game/Data/Caves";
        private const string CaveFolder = Root + "/Definitions";
        private const string ItemFolder = "Assets/_Game/Data/Archipelago/Items/CaveLoot";
        private const string CreatureFolder = "Assets/_Game/Data/Archipelago/Creatures/CaveGuardians";
        private const string CatalogPath = Root + "/CaveCatalog.asset";

        [MenuItem("Game/Giai đoạn 9/1 - Tạo hang động bí mật và vật phẩm")]
        public static void CreateCaveContent()
        {
            EnsureFolder("Assets/_Game/Data/Archipelago/Items");
            EnsureFolder("Assets/_Game/Data/Archipelago/Creatures");
            EnsureFolder(Root);
            EnsureFolder(CaveFolder);
            EnsureFolder(ItemFolder);
            EnsureFolder(CreatureFolder);

            CaveCatalog catalog = CreateCatalog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = catalog;
            Debug.Log("[Phase 9] Đã tạo 6 hang nhiều phòng, 6 hộ vệ, 11 vật phẩm, 6 mạch truyện và kho báu có khóa.");
            EditorUtility.DisplayDialog("Đảo Sương - Phase 9",
                "Đã tạo dữ liệu hang động bí mật và vật phẩm.\n\nTiếp theo chạy: Game → Giai đoạn 9 → 2 - Nâng cấp Scene cho hang + điện thoại.", "OK");
        }

        [MenuItem("Game/Giai đoạn 9/2 - Nâng cấp Scene cho hang + điện thoại")]
        public static void UpgradeScene()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CaveCatalog catalog = CreateCatalog();
            ArchipelagoManager world = Object.FindFirstObjectByType<ArchipelagoManager>();
            GameObject player = GameObject.Find("Player");
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (world == null || player == null || canvas == null)
            {
                EditorUtility.DisplayDialog("Đảo Sương - Thiếu Scene Phase 8",
                    "Cần có ArchipelagoManager, Player và Canvas. Hãy hoàn tất Phase 2–8 trước.", "Đóng");
                return;
            }

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            CampaignManager campaign = Object.FindFirstObjectByType<CampaignManager>();
            if (inventory == null || campaign == null)
            {
                EditorUtility.DisplayDialog("Đảo Sương - Thiếu hệ thống sinh tồn",
                    "Player cần PlayerInventory và Scene cần CampaignManager. Hãy chạy nâng cấp Phase 8 trước.", "Đóng");
                return;
            }

            CaveManager cave = world.GetComponent<CaveManager>();
            if (cave == null) cave = Undo.AddComponent<CaveManager>(world.gameObject);
            Assign(cave, "catalog", catalog);
            Assign(cave, "player", player.transform);
            Assign(cave, "inventory", inventory);
            Assign(cave, "campaign", campaign);
            Assign(cave, "archipelago", world);
            Assign(world, "caveManager", cave);

            MobileRuntimeBootstrap mobile = player.GetComponent<MobileRuntimeBootstrap>();
            if (mobile == null) mobile = Undo.AddComponent<MobileRuntimeBootstrap>(player);

            RectTransform safeRoot = SafeAreaFitter.GetOrCreate(canvas.transform);
            if (safeRoot == null) safeRoot = canvas.transform as RectTransform;
            ReparentIntoSafeArea(canvas.transform, safeRoot);
            CaveJournalHUD journal = canvas.GetComponent<CaveJournalHUD>();
            if (journal == null) journal = Undo.AddComponent<CaveJournalHUD>(canvas.gameObject);

            // The landscape touch HUD was designed for a wide phone layout.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            EditorUtility.SetDirty(world);
            EditorUtility.SetDirty(cave);
            EditorUtility.SetDirty(mobile);
            EditorUtility.SetDirty(journal);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EnsureSceneInBuildSettings();

            EditorUtility.DisplayDialog("Đảo Sương - Phase 9",
                "Đã gắn CaveManager, 6 hang, guardian, loot/lore, sổ nhật ký, Safe Area và cấu hình mobile landscape.\n\n" +
                "Chạy bước 3 để kiểm tra reference. Sau đó vào Play Mode và thử hang bằng nút TÁC ĐỘNG.", "OK");
        }

        [MenuItem("Game/Giai đoạn 9/3 - Kiểm tra Scene + cấu hình mobile")]
        public static void ValidateScene()
        {
            List<string> errors = new List<string>();
            CaveCatalog catalog = AssetDatabase.LoadAssetAtPath<CaveCatalog>(CatalogPath);
            ArchipelagoManager world = Object.FindFirstObjectByType<ArchipelagoManager>();
            CaveManager cave = Object.FindFirstObjectByType<CaveManager>();
            CampaignManager campaign = Object.FindFirstObjectByType<CampaignManager>();
            GameObject player = GameObject.Find("Player");
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();

            if (catalog == null || catalog.caves == null || catalog.caves.Length != 6)
                errors.Add("CaveCatalog phải có đúng 6 hang.");
            else
            {
                HashSet<string> caveIds = new HashSet<string>(StringComparer.Ordinal);
                HashSet<string> islandIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < catalog.caves.Length; i++)
                {
                    CaveDefinition definition = catalog.caves[i];
                    if (definition == null) { errors.Add("CaveDefinition null ở vị trí " + i); continue; }
                    if (!CaveRules.IsStableIdValid(definition.stableId) || !caveIds.Add(definition.stableId))
                        errors.Add("Stable ID hang rỗng/trùng: " + definition.name);
                    if (string.IsNullOrWhiteSpace(definition.islandId) || !islandIds.Add(definition.islandId))
                        errors.Add("Island ID hang rỗng/trùng: " + definition.name);
                    if (string.IsNullOrWhiteSpace(definition.displayName) || string.IsNullOrWhiteSpace(definition.hiddenStory))
                        errors.Add("Hang " + definition.name + " thiếu tên hoặc tình tiết ẩn.");
                    if (definition.roomCount != CaveRules.ClampRoomCount(definition.roomCount))
                        errors.Add("Hang " + definition.displayName + " có số phòng ngoài giới hạn 2–5.");
                    if (float.IsNaN(definition.ambientTemperatureC) || float.IsInfinity(definition.ambientTemperatureC) ||
                        float.IsNaN(definition.environmentExposure) || float.IsInfinity(definition.environmentExposure))
                        errors.Add("Hang " + definition.displayName + " có thông số môi trường không hữu hạn.");
                    if (definition.guardian == null) errors.Add("Hang " + definition.displayName + " chưa có guardian.");
                    else if (definition.guardian.maxHealth <= 0f || definition.guardian.moveSpeed < 0f || definition.guardian.damage < 0f)
                        errors.Add("Guardian " + definition.guardian.displayName + " có chỉ số không hợp lệ.");

                    if (definition.loot == null || definition.loot.Length < 4)
                    {
                        errors.Add("Hang " + definition.displayName + " cần tối thiểu 4 entry loot/lore.");
                    }
                    else
                    {
                        HashSet<string> lootIds = new HashSet<string>(StringComparer.Ordinal);
                        for (int j = 0; j < definition.loot.Length; j++)
                        {
                            CaveLootEntry entry = definition.loot[j];
                            if (entry == null || !CaveRules.IsStableIdValid(entry.stableId) || !lootIds.Add(entry.stableId))
                            {
                                errors.Add("Loot ID null/rỗng/trùng trong hang " + definition.displayName);
                                continue;
                            }
                            if (entry.item == null && string.IsNullOrWhiteSpace(entry.secretText))
                                errors.Add("Entry " + entry.stableId + " không có vật phẩm hoặc nội dung bí mật.");
                            if (entry.item != null && (string.IsNullOrWhiteSpace(entry.item.id) || entry.minAmount < 1 || entry.maxAmount < entry.minAmount))
                                errors.Add("Entry " + entry.stableId + " có vật phẩm hoặc số lượng không hợp lệ.");
                            if (float.IsNaN(entry.spawnChance) || float.IsInfinity(entry.spawnChance) || entry.spawnChance < 0f || entry.spawnChance > 1f)
                                errors.Add("Entry " + entry.stableId + " có tỷ lệ xuất hiện ngoài 0–1.");
                        }
                    }
                }
                if (islandIds.Count != 6) errors.Add("Cần định nghĩa hang cho đủ 6 island ID khác nhau.");

                CaveDefinition homeCave = catalog.FindForIsland("home");
                CaveDefinition ancientCave = catalog.FindForIsland("ancient");
                bool guaranteedKey = false;
                if (homeCave != null && homeCave.loot != null)
                {
                    for (int i = 0; i < homeCave.loot.Length; i++)
                    {
                        CaveLootEntry entry = homeCave.loot[i];
                        if (entry != null && entry.item != null && entry.item.id == "relic_key" &&
                            Mathf.Approximately(entry.spawnChance, 1f) && entry.minAmount >= 1)
                            guaranteedKey = true;
                    }
                }
                bool archiveUsesKey = false;
                if (ancientCave != null && ancientCave.loot != null)
                {
                    for (int i = 0; i < ancientCave.loot.Length; i++)
                    {
                        CaveLootEntry entry = ancientCave.loot[i];
                        if (entry != null && entry.requiredKey != null && entry.requiredKey.id == "relic_key")
                            archiveUsesKey = true;
                    }
                }
                if (!guaranteedKey) errors.Add("Mạch tiến trình lỗi: chìa khóa relic_key phải có tỷ lệ 100% ở hang đảo khởi đầu.");
                if (!archiveUsesKey) errors.Add("Mạch tiến trình lỗi: kho lưu trữ đảo cổ đại phải cần relic_key.");
            }

            ValidateGlobalContentIds(errors);

            if (world == null) errors.Add("Scene thiếu ArchipelagoManager.");
            if (campaign == null) errors.Add("Scene thiếu CampaignManager.");
            if (player == null)
            {
                errors.Add("Scene thiếu GameObject Player.");
            }
            else
            {
                PlayerInventory inventory = player.GetComponent<PlayerInventory>();
                if (inventory == null) errors.Add("Player thiếu PlayerInventory.");
                if (player.GetComponent<PlayerController>() == null) errors.Add("Player thiếu PlayerController.");
                if (player.GetComponent<PlayerGatherer>() == null) errors.Add("Player thiếu PlayerGatherer.");
                if (player.GetComponent<SurvivalController>() == null) errors.Add("Player thiếu SurvivalController.");
                if (player.GetComponent<MobileRuntimeBootstrap>() == null) errors.Add("Player thiếu MobileRuntimeBootstrap.");
                if (player.GetComponent<CharacterController>() == null) errors.Add("Player thiếu CharacterController.");
            }

            if (world != null && cave != null && GetReference(world, "caveManager") != cave)
                errors.Add("ArchipelagoManager chưa tham chiếu đúng CaveManager.");
            if (cave == null)
            {
                errors.Add("Scene thiếu CaveManager.");
            }
            else
            {
                if (GetReference(cave, "catalog") != catalog) errors.Add("CaveManager chưa gán đúng CaveCatalog.");
                if (world != null && GetReference(cave, "archipelago") != world) errors.Add("CaveManager chưa gán ArchipelagoManager.");
                if (campaign != null && GetReference(cave, "campaign") != campaign) errors.Add("CaveManager chưa gán CampaignManager.");
                if (player != null)
                {
                    if (GetReference(cave, "player") != player.transform) errors.Add("CaveManager chưa gán Player Transform.");
                    if (GetReference(cave, "inventory") != player.GetComponent<PlayerInventory>()) errors.Add("CaveManager chưa gán PlayerInventory.");
                }
            }

            if (canvas == null)
            {
                errors.Add("Scene thiếu Canvas.");
            }
            else
            {
                Transform safeRoot = canvas.transform.Find("MobileSafeAreaRoot");
                if (safeRoot == null) errors.Add("Canvas thiếu MobileSafeAreaRoot.");
                else if (safeRoot.GetComponent<SafeAreaFitter>() == null) errors.Add("MobileSafeAreaRoot thiếu SafeAreaFitter.");
                if (canvas.GetComponent<CanvasScaler>() == null) errors.Add("Canvas thiếu CanvasScaler cho nhiều kích thước điện thoại.");
                if (canvas.GetComponent<GraphicRaycaster>() == null) errors.Add("Canvas thiếu GraphicRaycaster.");

                MobileInput input = Object.FindFirstObjectByType<MobileInput>();
                if (input == null) errors.Add("Scene thiếu MobileInput.");
                else
                {
                    string[] inputFields = { "joystick", "lookZone", "jumpButton", "actionButton", "sprintButton" };
                    for (int i = 0; i < inputFields.Length; i++)
                        if (GetReference(input, inputFields[i]) == null) errors.Add("MobileInput thiếu reference: " + inputFields[i]);
                }
                if (safeRoot != null)
                {
                    string[] controls = { "LookZone", "Joystick", "JumpButton", "ActionButton", "SprintButton" };
                    for (int i = 0; i < controls.Length; i++)
                        if (safeRoot.Find(controls[i]) == null) errors.Add("Safe Area thiếu nút điều khiển: " + controls[i]);
                }
                if (canvas.GetComponent<SurvivalHUD>() == null) errors.Add("Canvas thiếu SurvivalHUD.");
                if (canvas.GetComponent<InventoryHUD>() == null) errors.Add("Canvas thiếu InventoryHUD.");
                if (canvas.GetComponent<CraftingHUD>() == null) errors.Add("Canvas thiếu CraftingHUD.");
                if (canvas.GetComponent<CaveJournalHUD>() == null) errors.Add("Canvas thiếu CaveJournalHUD để đọc lại tình tiết đã khám phá.");
            }

            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.LandscapeLeft ||
                PlayerSettings.allowedAutorotateToPortrait || PlayerSettings.allowedAutorotateToPortraitUpsideDown)
                errors.Add("Player Settings chưa khóa màn hình điện thoại ở chế độ ngang.");

            if (errors.Count == 0)
            {
                Debug.Log("[Phase 9] PASS: dữ liệu hang, kho báu, guardian, liên kết gameplay và HUD mobile hợp lệ.");
                EditorUtility.DisplayDialog("Phase 9 - PASS", "Không phát hiện lỗi cấu hình Phase 9. Đây là kiểm tra Scene/data; vẫn cần chạy Test Runner và thử trên thiết bị Android thật.", "OK");
            }
            else
            {
                Debug.LogError("[Phase 9] FAIL:\n- " + string.Join("\n- ", errors));
                EditorUtility.DisplayDialog("Phase 9 - Cần sửa", string.Join("\n", errors), "OK");
            }
        }

        private static void ValidateGlobalContentIds(List<string> errors)
        {
            HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);
            string[] itemGuids = AssetDatabase.FindAssets("t:ItemData");
            for (int i = 0; i < itemGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(itemGuids[i]);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null || string.IsNullOrWhiteSpace(item.id)) continue;
                if (!itemIds.Add(item.id.Trim())) errors.Add("Trùng ItemData.id toàn dự án: " + item.id + " (" + path + ").");
            }

            HashSet<string> creatureIds = new HashSet<string>(StringComparer.Ordinal);
            string[] creatureGuids = AssetDatabase.FindAssets("t:CreatureDefinition");
            for (int i = 0; i < creatureGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(creatureGuids[i]);
                CreatureDefinition creature = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(path);
                if (creature == null || string.IsNullOrWhiteSpace(creature.stableId)) continue;
                if (!creatureIds.Add(creature.stableId.Trim())) errors.Add("Trùng CreatureDefinition.stableId toàn dự án: " + creature.stableId + " (" + path + ").");
            }
        }

        private static CaveCatalog CreateCatalog()
        {
            ItemData pearl = CreateItem("CavePearl", "cave_pearl", "Ngọc Trai Hang", new Color(0.7f, 0.9f, 1f), 0.15f);
            ItemData glowMushroom = CreateItem("GlowMushroom", "glow_mushroom", "Nấm Phát Sáng", new Color(0.25f, 0.9f, 0.55f), 0.08f);
            ItemData amber = CreateItem("AmberShard", "amber_shard", "Mảnh Hổ Phách", new Color(1f, 0.55f, 0.12f), 0.12f);
            ItemData venom = CreateItem("CaveSwampVenom", "cave_swamp_venom", "Túi Nọc Đầm Lầy", new Color(0.55f, 0.85f, 0.12f), 0.2f);
            ItemData obsidian = CreateItem("ObsidianCore", "obsidian_core", "Lõi Hắc Diện", new Color(0.22f, 0.12f, 0.2f), 0.5f);
            ItemData frost = CreateItem("FrostEssence", "frost_essence", "Tinh Hoa Băng", new Color(0.35f, 0.85f, 1f), 0.12f);
            ItemData coin = CreateItem("AncientCoin", "ancient_coin", "Cổ Kim", new Color(0.95f, 0.72f, 0.18f), 0.05f);
            ItemData rune = CreateItem("EchoRune", "echo_rune", "Cổ Ngữ Vọng Âm", new Color(0.55f, 0.35f, 1f), 0.1f);
            ItemData relicKey = CreateItem("RelicKey", "relic_key", "Chìa Khóa Di Tích", new Color(0.9f, 0.8f, 0.35f), 0.1f);
            ItemData fossil = CreateItem("FossilFragment", "fossil_fragment", "Mảnh Hóa Thạch", new Color(0.78f, 0.72f, 0.56f), 0.25f);
            ItemData crystalDust = CreateItem("CrystalDust", "crystal_dust", "Bụi Pha Lê", new Color(0.35f, 0.75f, 0.9f), 0.03f);

            CreatureDefinition drowned = CreateGuardian("DrownedGuardian", "drowned_guardian", "Vệ Binh Chìm", 90f, 2.0f, 8f, new Color(0.2f, 0.55f, 0.65f), pearl);
            CreatureDefinition root = CreateGuardian("RootWarden", "root_warden", "Hộ Vệ Rễ Cổ", 125f, 2.1f, 11f, new Color(0.28f, 0.52f, 0.2f), amber);
            CreatureDefinition bog = CreateGuardian("CaveBogCrawler", "cave_bog_crawler", "Bò Sát Bùn Đen", 150f, 2.6f, 13f, new Color(0.4f, 0.55f, 0.12f), venom);
            CreatureDefinition ash = CreateGuardian("AshSentinel", "ash_sentinel", "Kỵ Sĩ Tro Tàn", 190f, 2.3f, 16f, new Color(0.7f, 0.22f, 0.1f), obsidian);
            CreatureDefinition wraith = CreateGuardian("FrostWraith", "frost_wraith", "Bóng Ma Băng", 220f, 2.9f, 18f, new Color(0.55f, 0.85f, 1f), frost);
            CreatureDefinition echo = CreateGuardian("AncientEcho", "ancient_echo", "Ảnh Vọng Cổ Đại", 300f, 2.5f, 22f, new Color(0.55f, 0.3f, 0.85f), rune);

            CaveDefinition[] caves = new CaveDefinition[6];
            caves[0] = CreateCave("TidalVault", "home", "Hầm Ngầm Thủy Triều",
                "Bức tường khắc tên những người đã biến mất trước khi màn sương phủ kín quần đảo. Một dòng chữ còn nguyên: 'Đừng đánh thức tiếng chuông dưới đáy biển.'",
                3, 1, drowned, new Color(0.17f, 0.27f, 0.31f), new Color(0.2f, 0.78f, 0.92f), new[]
                {
                    Loot("tidal_pearl", pearl, 2, 4, 1f, 0, null, true, "Ngọc trai phát ra nhịp sáng giống một trái tim."),
                    Loot("glow_mushroom", glowMushroom, 2, 5, 0.9f, 1, null, true, "Nấm mọc quanh một dấu bàn tay khổng lồ."),
                    Loot("first_relic_key", relicKey, 1, 1, 1f, 1, null, true, "Chiếc chìa khóa mang phù hiệu của Đảo Cổ Đại."),
                    Loot("drowned_memory", null, 0, 0, 1f, 2, null, false, "Một thủy thủ nói rằng boss trên đảo chỉ là người canh cổng.")
                });
            caves[1] = CreateCave("RootHollow", "forest", "Lòng Rễ Thì Thầm",
                "Những rễ cây xuyên qua đá như mạch máu. Tiếng thì thầm gọi tên một hòn đảo chưa từng xuất hiện trên bản đồ.",
                4, 2, root, new Color(0.18f, 0.27f, 0.17f), new Color(0.35f, 0.95f, 0.55f), new[]
                {
                    Loot("root_amber", amber, 2, 4, 1f, 1, null, true, "Hổ phách chứa một chiếc lá vẫn còn xanh."),
                    Loot("root_mushroom", glowMushroom, 3, 6, 0.95f, 0, null, true, "Bào tử phát sáng tạo thành hình một con mắt."),
                    Loot("root_fossil", fossil, 1, 3, 0.7f, 2, null, true, "Hóa thạch cho thấy sinh vật này từng sống dưới biển sâu."),
                    Loot("root_memory", null, 0, 0, 1f, 3, null, false, "Một bản đồ cũ đánh dấu lối vào nằm dưới chân núi lửa.")
                });
            caves[2] = CreateCave("BlackMire", "swamp", "Hố Sâu Đầm Đen",
                "Bùn đen không phản chiếu gương mặt bạn. Nó phản chiếu một người khác, đang đứng phía sau và cầm chiếc đèn lồng đã tắt.",
                4, 3, bog, new Color(0.17f, 0.22f, 0.12f), new Color(0.65f, 0.9f, 0.2f), new[]
                {
                    Loot("mire_venom", venom, 2, 4, 1f, 1, null, true, "Nọc độc được đựng trong lọ thủy tinh không có đáy."),
                    Loot("mire_crystal", crystalDust, 5, 9, 0.9f, 2, null, true, "Bụi pha lê rung lên khi nghe tiếng bước chân."),
                    Loot("mire_coin", coin, 2, 5, 0.65f, 0, null, true, "Đồng tiền có ngày đúc trước khi hòn đảo được hình thành."),
                    Loot("mire_memory", null, 0, 0, 1f, 3, null, false, "Một cánh cửa bị niêm phong ở núi lửa giữ lại hơi thở của mặt đất.")
                });
            caves[3] = CreateCave("AshHeart", "volcano", "Trái Tim Tro Đỏ",
                "Bên dưới lớp dung nham nguội là một cỗ máy đang đếm ngược. Mỗi lần nó rung, một ngày trên mặt đất lại bị rút ngắn.",
                4, 4, ash, new Color(0.28f, 0.13f, 0.11f), new Color(1f, 0.3f, 0.08f), new[]
                {
                    Loot("ash_core", obsidian, 2, 3, 1f, 2, null, true, "Lõi đá giữ nhiệt dù đã tách khỏi dung nham."),
                    Loot("ash_crystal", crystalDust, 8, 14, 0.95f, 1, null, true, "Tinh thể phát nổ thành bụi sáng nếu rơi xuống nước."),
                    Loot("ash_coin", coin, 3, 7, 0.75f, 0, null, true, "Mặt sau đồng tiền khắc hình một con rồng đang ngủ."),
                    Loot("ash_memory", null, 0, 0, 1f, 3, null, false, "Long băng không canh giữ kho báu. Nó canh giữ thứ đã bị chôn dưới băng.")
                });
            caves[4] = CreateCave("BlueChasm", "frost", "Vực Lam Tĩnh Lặng",
                "Trong băng có những bóng người đang đi bộ. Họ không cử động, nhưng dấu chân bên ngoài vẫn tiếp tục xuất hiện.",
                4, 4, wraith, new Color(0.16f, 0.24f, 0.34f), new Color(0.35f, 0.82f, 1f), new[]
                {
                    Loot("frost_essence", frost, 2, 4, 1f, 1, null, true, "Tinh hoa băng không tan ngay cả khi đặt cạnh lửa."),
                    Loot("frost_fossil", fossil, 2, 3, 0.9f, 2, null, true, "Hóa thạch của một loài cá sống trước thời đại sương mù."),
                    Loot("frost_coin", coin, 4, 8, 0.8f, 0, null, true, "Đồng tiền phủ băng nhưng không hề lạnh."),
                    Loot("frost_memory", null, 0, 0, 1f, 3, null, false, "Cánh cổng cuối cùng chỉ mở khi sáu tiếng vọng cùng vang lên.")
                });
            caves[5] = CreateCave("EchoSanctum", "ancient", "Thánh Đường Vọng Âm",
                "Đây là trung tâm của quần đảo. Những bức tượng không quay về phía bàn thờ; tất cả đều nhìn ra biển, như thể đang chờ một thứ gì đó trở về.",
                5, 5, echo, new Color(0.21f, 0.18f, 0.3f), new Color(0.68f, 0.42f, 1f), new[]
                {
                    Loot("ancient_coin", coin, 8, 14, 1f, 1, null, true, "Cổ kim có cùng ký hiệu với các cột đá trên mặt đất."),
                    Loot("echo_rune", rune, 1, 2, 0.9f, 2, null, true, "Cổ ngữ lưu lại âm thanh cuối cùng của người tạo ra quần đảo."),
                    Loot("sealed_archive", rune, 2, 4, 1f, 4, relicKey, true, "Kho lưu trữ được mở bằng chìa khóa tìm thấy ở Hầm Ngầm Thủy Triều."),
                    Loot("sanctum_memory", null, 0, 0, 1f, 4, null, false, "Bí mật cuối cùng: màn sương không đến từ biển. Nó đang bảo vệ thế giới khỏi thứ ở bên kia.")
                });

            CaveCatalog catalog = LoadOrCreate<CaveCatalog>(CatalogPath, "CaveCatalog");
            catalog.caves = caves;
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static CaveDefinition CreateCave(string file, string islandId, string displayName, string story,
            int rooms, int danger, CreatureDefinition guardian, Color rock, Color crystal, CaveLootEntry[] loot)
        {
            CaveDefinition cave = LoadOrCreate<CaveDefinition>(CaveFolder + "/" + file + ".asset", file);
            cave.stableId = file.ToLowerInvariant();
            cave.islandId = islandId;
            cave.displayName = displayName;
            cave.hiddenStory = story;
            cave.roomCount = rooms;
            cave.dangerLevel = danger;
            cave.ambientTemperatureC = islandId == "volcano" ? 28f : islandId == "frost" ? 5f :
                islandId == "swamp" ? 17f : islandId == "forest" ? 13f : islandId == "ancient" ? 12f : 15f;
            cave.environmentExposure = 0.08f;
            cave.guardian = guardian;
            cave.rockColor = rock;
            cave.crystalColor = crystal;
            cave.loot = loot;
            EditorUtility.SetDirty(cave);
            return cave;
        }

        private static CaveLootEntry Loot(string id, ItemData item, int min, int max, float chance,
            int room, ItemData key, bool consumeKey, string story)
        {
            return new CaveLootEntry
            {
                stableId = id, item = item, minAmount = min, maxAmount = max,
                spawnChance = chance, roomIndex = room, requiredKey = key,
                consumeKey = consumeKey, secretText = story
            };
        }

        private static ItemData CreateItem(string file, string id, string label, Color color, float weight)
        {
            ItemData item = LoadOrCreate<ItemData>(ItemFolder + "/" + file + ".asset", file);
            item.id = id;
            item.displayName = label;
            item.maxStack = 50;
            item.weight = weight;
            item.color = color;
            EditorUtility.SetDirty(item);
            return item;
        }

        private static CreatureDefinition CreateGuardian(string file, string id, string label, float hp,
            float speed, float damage, Color color, ItemData reward)
        {
            CreatureDefinition creature = LoadOrCreate<CreatureDefinition>(CreatureFolder + "/" + file + ".asset", file);
            creature.stableId = id;
            creature.displayName = label;
            creature.maxHealth = hp;
            creature.moveSpeed = speed;
            creature.damage = damage;
            creature.detectionRange = 24f;
            creature.attackRange = 2f;
            creature.attackCooldown = 2.2f;
            creature.bodyScale = 1.6f;
            creature.bodyColor = color;
            creature.rewardAmount = 2;
            creature.rewardItem = reward;
            EditorUtility.SetDirty(creature);
            return creature;
        }

        private static T LoadOrCreate<T>(string path, string assetName) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(folder);
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "Assets") return;
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void Assign(Object target, string property, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null)
            {
                Debug.LogError("[Phase 9] Không tìm thấy field " + property + " trên " + target.GetType().Name);
                return;
            }
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Object GetReference(Object target, string property)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            return field != null ? field.objectReferenceValue : null;
        }

        private static void ReparentIntoSafeArea(Transform canvasRoot, Transform safeRoot)
        {
            if (canvasRoot == null || safeRoot == null) return;
            string[] names = { "LookZone", "Joystick", "JumpButton", "ActionButton", "SprintButton", "HudEditButton", "EditController", "GatherHud" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform child = canvasRoot.Find(names[i]);
                if (child == null || child == safeRoot) continue;
                Undo.RecordObject(child, "Move HUD into Safe Area");
                child.SetParent(safeRoot, false);
                EditorUtility.SetDirty(child);
            }
        }

        private static void EnsureSceneInBuildSettings()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path)) return;
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index = scenes.FindIndex(x => x.path == scene.path);
            if (index >= 0) scenes[index] = new EditorBuildSettingsScene(scene.path, true);
            else scenes.Add(new EditorBuildSettingsScene(scene.path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif

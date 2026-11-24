#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using DaoSuong.Content;
using Game.Gameplay;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>Creates the 99-day archipelago content and wires it into the current playable scene.</summary>
    public static class Phase8CampaignTools
    {
        private const string Root = "Assets/_Game/Data/Archipelago";
        private const string IslandFolder = Root + "/Islands";
        private const string CreatureFolder = Root + "/Creatures";
        private const string BossFolder = Root + "/Bosses";
        private const string ItemFolder = Root + "/Items";
        private const string BuildingFolder = Root + "/Buildings";
        private const string CatalogPath = Root + "/ArchipelagoCatalog.asset";
        private const string BuildingCatalogPath = BuildingFolder + "/BuildingCatalog.asset";
        private const string GatherSettingsPath = "Assets/_Game/Data/Gather/GatherSettings.asset";
        private const string TerrainMaterialPath = "Assets/_Game/Art/Materials/M_Terrain.mat";

        private sealed class ContentSet
        {
            public ArchipelagoCatalog archipelago;
            public BuildingCatalog buildings;
            public GatherSettings gathering;
        }

        [MenuItem("Game/Giai đoạn 8/1 - Tạo nội dung quần đảo 99 ngày")]
        public static void CreateCampaignContent()
        {
            EnsureFolder(Root);
            EnsureFolder(IslandFolder);
            EnsureFolder(CreatureFolder);
            EnsureFolder(BossFolder);
            EnsureFolder(ItemFolder);
            EnsureFolder(BuildingFolder);

            // Reuse the existing Phase 3 creator so a fresh checkout also gets valid gather assets.
            Stage3DataCreator.CreateSampleData();
            ContentSet content = CreateContent();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = content.archipelago;
            Debug.Log("[Phase 8] Đã tạo nội dung 6 đảo, 7 sinh vật, 6 boss, vật liệu hiếm và 7 công trình. Hãy chạy bước 2 để gắn vào Scene.");
            EditorUtility.DisplayDialog("Đảo Sương - Phase 8",
                "Đã tạo dữ liệu quần đảo sinh tồn 99 ngày.\n\nTiếp theo: Game → Giai đoạn 8 → 2 - Nâng cấp Scene hiện tại.", "OK");
        }

        [MenuItem("Game/Giai đoạn 8/2 - Nâng cấp Scene hiện tại")]
        public static void UpgradeCurrentScene()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ContentSet content = CreateContent();
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject == null)
            {
                try { playerObject = GameObject.FindGameObjectWithTag("Player"); }
                catch (UnityException) { /* Player tag may not yet exist in an incomplete scene. */ }
            }
            IslandGenerator island = Object.FindFirstObjectByType<IslandGenerator>();
            DayNightCycle dayNight = Object.FindFirstObjectByType<DayNightCycle>();
            WeatherSystem weather = Object.FindFirstObjectByType<WeatherSystem>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Material terrain = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);

            List<string> missing = new List<string>();
            if (playerObject == null) missing.Add("Player");
            if (island == null) missing.Add("IslandGenerator");
            if (dayNight == null) missing.Add("DayNightCycle");
            if (canvas == null) missing.Add("Canvas");
            if (terrain == null) missing.Add("M_Terrain.mat (chạy Phase 2 tạo World Assets)");
            if (content.gathering == null) missing.Add("GatherSettings (Phase 3)");
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog("Đảo Sương - Chưa đủ nền tảng",
                    "Thiếu thành phần:\n• " + string.Join("\n• ", missing) +
                    "\n\nHoàn tất Phase 2–7 rồi chạy lại bước này.", "Đóng");
                return;
            }

            PlayerInventory inventory = playerObject.GetComponent<PlayerInventory>();
            SurvivalController survival = playerObject.GetComponent<SurvivalController>();
            PlayerController movement = playerObject.GetComponent<PlayerController>();
            if (inventory == null || survival == null || movement == null)
            {
                EditorUtility.DisplayDialog("Đảo Sương - Thiếu hệ thống người chơi",
                    "Player cần PlayerInventory, SurvivalController và PlayerController. Hãy hoàn tất Phase 4–6 trước.", "Đóng");
                return;
            }

            CampaignManager campaign = island.GetComponent<CampaignManager>();
            if (campaign == null) campaign = Undo.AddComponent<CampaignManager>(island.gameObject);
            SetObject(campaign, "dayNight", dayNight);
            SetObject(campaign, "survival", survival);
            SetInt(campaign, "requiredDays", 99);
            SetBool(campaign, "loadSavedRun", true);

            ArchipelagoManager archipelago = island.GetComponent<ArchipelagoManager>();
            if (archipelago == null) archipelago = Undo.AddComponent<ArchipelagoManager>(island.gameObject);
            SetObject(archipelago, "catalog", content.archipelago);
            SetObject(archipelago, "templateGenerator", island);
            SetObject(archipelago, "terrainMaterial", terrain);
            SetObject(archipelago, "gatherSettings", content.gathering);
            SetObject(archipelago, "pickupMaterial", terrain);
            SetObject(archipelago, "player", playerObject.transform);
            SetObject(archipelago, "campaign", campaign);

            BaseBuildingManager building = playerObject.GetComponent<BaseBuildingManager>();
            if (building == null) building = Undo.AddComponent<BaseBuildingManager>(playerObject);
            SetObject(building, "catalog", content.buildings);
            SetObject(building, "inventory", inventory);
            SetObject(building, "survival", survival);
            SetObject(building, "player", playerObject.transform);
            SetObject(building, "archipelago", archipelago);
            SetObjectArray(building, "persistentItemCatalog", FindItemAssets());

            CombatController combat = playerObject.GetComponent<CombatController>();
            if (combat == null) combat = Undo.AddComponent<CombatController>(playerObject);
            SetObject(combat, "survival", survival);
            SetObject(combat, "inventory", inventory);
            SetFloat(dayNight, "dayLengthSeconds", 360f); // Six real minutes per in-game day; editable in Inspector.

            if (canvas.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            CampaignHUD hud = canvas.GetComponent<CampaignHUD>();
            if (hud == null) hud = Undo.AddComponent<CampaignHUD>(canvas.gameObject);

            EnsureInputEventSystem();
            EditorUtility.SetDirty(campaign);
            EditorUtility.SetDirty(archipelago);
            EditorUtility.SetDirty(building);
            EditorUtility.SetDirty(combat);
            EditorUtility.SetDirty(canvas);
            EditorUtility.SetDirty(hud);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EnsureActiveSceneInBuildSettings();

            EditorUtility.DisplayDialog("Đảo Sương - Phase 8",
                "Đã gắn hệ thống 99 ngày, 6 đảo, quái/boss, chiến đấu, xây căn cứ và HUD.\n\n" +
                "Chạy Game → Giai đoạn 8 → 3 - Kiểm tra Scene. Sau đó vào Play Mode để kiểm thử.", "OK");
        }

        [MenuItem("Game/Giai đoạn 8/3 - Kiểm tra Scene")]
        public static void ValidateScene()
        {
            List<string> errors = new List<string>();
            ArchipelagoCatalog catalog = AssetDatabase.LoadAssetAtPath<ArchipelagoCatalog>(CatalogPath);
            BuildingCatalog buildingCatalog = AssetDatabase.LoadAssetAtPath<BuildingCatalog>(BuildingCatalogPath);
            GatherSettings gather = AssetDatabase.LoadAssetAtPath<GatherSettings>(GatherSettingsPath);
            GameObject player = GameObject.Find("Player");
            IslandGenerator island = Object.FindFirstObjectByType<IslandGenerator>();
            CampaignManager campaign = Object.FindFirstObjectByType<CampaignManager>();
            ArchipelagoManager archipelago = Object.FindFirstObjectByType<ArchipelagoManager>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();

            if (catalog == null || catalog.islands == null || catalog.islands.Length < 6)
                errors.Add("ArchipelagoCatalog cần tối thiểu 6 đảo.");
            else
            {
                HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < catalog.islands.Length; i++)
                {
                    IslandDestination destination = catalog.islands[i];
                    if (destination == null) { errors.Add("Có cấu hình đảo null ở vị trí " + i); continue; }
                    if (string.IsNullOrWhiteSpace(destination.id) || !ids.Add(destination.id)) errors.Add("ID đảo rỗng hoặc trùng: " + destination.id);
                    if (destination.settings == null) errors.Add("Đảo " + destination.id + " thiếu IslandSettings.");
                    if (i > 0 && destination.boss == null) errors.Add("Đảo " + destination.id + " thiếu boss.");
                }
            }
            if (buildingCatalog == null || buildingCatalog.pieces == null || buildingCatalog.pieces.Length < 5)
                errors.Add("BuildingCatalog cần tối thiểu 5 công trình.");
            if (gather == null || gather.tree == null || gather.rockDefault == null)
                errors.Add("GatherSettings thiếu dữ liệu cây/đá. Chạy Game/Giai đoạn 3/Tạo dữ liệu mẫu.");
            if (player == null) errors.Add("Không tìm thấy GameObject Player.");
            else
            {
                if (player.GetComponent<PlayerInventory>() == null) errors.Add("Player thiếu PlayerInventory.");
                if (player.GetComponent<SurvivalController>() == null) errors.Add("Player thiếu SurvivalController.");
                if (player.GetComponent<BaseBuildingManager>() == null) errors.Add("Player thiếu BaseBuildingManager.");
                if (player.GetComponent<CombatController>() == null) errors.Add("Player thiếu CombatController.");
            }
            if (island == null) errors.Add("Thiếu IslandGenerator.");
            if (campaign == null) errors.Add("Thiếu CampaignManager.");
            else if (GetInt(campaign, "requiredDays") != 99) errors.Add("CampaignManager.requiredDays phải bằng 99.");
            if (archipelago == null) errors.Add("Thiếu ArchipelagoManager.");
            else
            {
                if (GetReference(archipelago, "catalog") == null) errors.Add("ArchipelagoManager chưa gán ArchipelagoCatalog.");
                if (GetReference(archipelago, "templateGenerator") == null) errors.Add("ArchipelagoManager chưa gán IslandGenerator mẫu.");
                if (GetReference(archipelago, "terrainMaterial") == null) errors.Add("ArchipelagoManager chưa gán Terrain Material.");
                if (GetReference(archipelago, "gatherSettings") == null) errors.Add("ArchipelagoManager chưa gán GatherSettings.");
            }
            if (canvas == null || canvas.GetComponent<CampaignHUD>() == null) errors.Add("Canvas thiếu CampaignHUD.");
            if (Object.FindFirstObjectByType<EventSystem>() == null || Object.FindFirstObjectByType<EventSystem>().GetComponent<InputSystemUIInputModule>() == null)
                errors.Add("Thiếu EventSystem với InputSystemUIInputModule.");

            if (errors.Count == 0)
            {
                Debug.Log("[Phase 8] Validate passed: 6+ islands, 99-day campaign, boss catalog, building catalog and runtime references are present.");
                EditorUtility.DisplayDialog("Phase 8 - Validate", "Không phát hiện thiếu reference cấu hình cốt lõi.\n\nCần tiếp tục Play Mode, Test Runner và build Android thực tế.", "OK");
            }
            else
            {
                Debug.LogError("[Phase 8] Validate có " + errors.Count + " lỗi:\n• " + string.Join("\n• ", errors));
                EditorUtility.DisplayDialog("Phase 8 - Validate", string.Join("\n", errors), "Đóng");
            }
        }

        private static ContentSet CreateContent()
        {
            EnsureFolder(Root); EnsureFolder(IslandFolder); EnsureFolder(CreatureFolder);
            EnsureFolder(BossFolder); EnsureFolder(ItemFolder); EnsureFolder(BuildingFolder);

            ItemData wood = LoadItem("Assets/_Game/Data/Gather/Wood.asset", "wood", "Gỗ", new Color(0.52f, 0.34f, 0.19f), 0.5f);
            ItemData stone = LoadItem("Assets/_Game/Data/Gather/Stone.asset", "stone", "Đá", new Color(0.52f, 0.54f, 0.57f), 0.7f);
            ItemData fang = CreateItem("BossFang", "boss_fang", "Nanh quái", new Color(0.82f, 0.83f, 0.75f), 0.2f);
            ItemData venom = CreateItem("SwampVenom", "swamp_venom", "Nọc đầm lầy", new Color(0.39f, 0.78f, 0.18f), 0.1f);
            ItemData ember = CreateItem("EmberCrystal", "ember_crystal", "Tinh thể tro", new Color(1f, 0.28f, 0.08f), 0.2f);
            ItemData frost = CreateItem("FrostShard", "frost_shard", "Mảnh băng vĩnh cửu", new Color(0.45f, 0.82f, 1f), 0.2f);
            ItemData ancient = CreateItem("AncientCore", "ancient_core", "Lõi cổ đại", new Color(0.63f, 0.42f, 0.9f), 0.3f);

            CreatureDefinition crab = CreateCreature("BeachCrab", "beach_crab", "Cua đá", 45f, 2.4f, 7f, new Color(0.74f, 0.34f, 0.2f), fang);
            CreatureDefinition boar = CreateCreature("ForestBoar", "forest_boar", "Lợn rừng", 65f, 3.3f, 10f, new Color(0.39f, 0.24f, 0.13f), fang);
            CreatureDefinition bogCrawler = CreateCreature("BogCrawler", "bog_crawler", "Bò sát đầm lầy", 58f, 2.8f, 9f, new Color(0.27f, 0.48f, 0.18f), venom);
            CreatureDefinition ashHound = CreateCreature("AshHound", "ash_hound", "Chó tro", 82f, 4.1f, 14f, new Color(0.62f, 0.19f, 0.12f), ember);
            CreatureDefinition frostWolf = CreateCreature("FrostWolf", "frost_wolf", "Sói băng", 95f, 4.4f, 16f, new Color(0.52f, 0.75f, 0.9f), frost);
            CreatureDefinition ancientSentinel = CreateCreature("AncientSentinel", "ancient_sentinel", "Vệ binh cổ", 135f, 2.7f, 22f, new Color(0.46f, 0.34f, 0.68f), ancient);
            CreatureDefinition caveBat = CreateCreature("CaveBat", "cave_bat", "Dơi hang", 32f, 5f, 6f, new Color(0.25f, 0.23f, 0.31f), fang);

            BossDefinition tidalGiant = CreateBoss("TidalGiant", "tidal_giant", "Khổng lồ thủy triều", 650f, 2, 20f, new Color(0.2f, 0.42f, 0.55f), fang, 3f, 4.5f, 7f, 26f);
            BossDefinition saltWarden = CreateBoss("SaltWarden", "salt_warden", "Hộ vệ muối", 900f, 4, 25f, new Color(0.58f, 0.54f, 0.45f), fang, 3.2f, 5f, 8f, 34f);
            BossDefinition mireQueen = CreateBoss("MireQueen", "mire_queen", "Nữ hoàng đầm lầy", 1250f, 9, 31f, new Color(0.31f, 0.57f, 0.16f), venom, 3.5f, 6f, 9f, 42f);
            BossDefinition ashTitan = CreateBoss("AshTitan", "ash_titan", "Titan tro tàn", 1700f, 16, 38f, new Color(0.72f, 0.19f, 0.07f), ember, 3.8f, 6.5f, 10f, 48f);
            BossDefinition frostWyrm = CreateBoss("FrostWyrm", "frost_wyrm", "Long băng", 2200f, 23, 46f, new Color(0.35f, 0.7f, 0.95f), frost, 4f, 7f, 12f, 54f);
            BossDefinition ancientCore = CreateBoss("AncientCoreBoss", "ancient_core_boss", "Thủ hộ lõi cổ đại", 3000f, 36, 55f, new Color(0.53f, 0.3f, 0.82f), ancient, 4.5f, 8f, 14f, 64f);

            IslandSettings homeSettings = CreateIslandSettings("Island_Home", new Color(0.28f, 0.52f, 0.26f), new Color(0.46f, 0.45f, 0.4f), new Color(0.86f, 0.78f, 0.55f), 28f, 400, 70, 0.18f, 0.72f);
            IslandSettings forestSettings = CreateIslandSettings("Island_Forest", new Color(0.12f, 0.38f, 0.18f), new Color(0.34f, 0.36f, 0.31f), new Color(0.83f, 0.72f, 0.43f), 32f, 520, 80, 0.20f, 0.78f);
            IslandSettings swampSettings = CreateIslandSettings("Island_Swamp", new Color(0.19f, 0.31f, 0.13f), new Color(0.3f, 0.32f, 0.25f), new Color(0.55f, 0.5f, 0.34f), 18f, 240, 90, 0.28f, 0.88f);
            IslandSettings volcanoSettings = CreateIslandSettings("Island_Volcano", new Color(0.36f, 0.18f, 0.12f), new Color(0.28f, 0.25f, 0.25f), new Color(0.46f, 0.31f, 0.22f), 46f, 160, 140, 0.22f, 0.75f);
            IslandSettings frostSettings = CreateIslandSettings("Island_Frost", new Color(0.31f, 0.46f, 0.52f), new Color(0.46f, 0.54f, 0.62f), new Color(0.84f, 0.91f, 0.98f), 38f, 140, 120, 0.18f, 0.7f);
            IslandSettings ancientSettings = CreateIslandSettings("Island_Ancient", new Color(0.3f, 0.24f, 0.42f), new Color(0.39f, 0.33f, 0.48f), new Color(0.6f, 0.52f, 0.73f), 42f, 190, 150, 0.24f, 0.82f);

            ArchipelagoCatalog catalog = LoadOrCreate<ArchipelagoCatalog>(CatalogPath, "ArchipelagoCatalog");
            if (catalog.islands == null || catalog.islands.Length == 0)
            {
                catalog.islands = new[]
                {
                    Destination("home", "Đảo Khởi Đầu", homeSettings, Vector3.zero, 11001, 0, null, 4,
                        new[] { crab, boar, caveBat }, tidalGiant),
                    Destination("forest", "Đảo Rừng Sâu", forestSettings, new Vector3(560f, 0f, 0f), 22002, 2, "tidal_giant", 5,
                        new[] { boar, caveBat }, saltWarden),
                    Destination("swamp", "Đảo Đầm Lầy", swampSettings, new Vector3(-560f, 0f, 0f), 33003, 4, "salt_warden", 5,
                        new[] { bogCrawler, caveBat }, mireQueen),
                    Destination("volcano", "Đảo Núi Lửa", volcanoSettings, new Vector3(0f, 0f, 560f), 44004, 9, "mire_queen", 5,
                        new[] { ashHound, caveBat }, ashTitan),
                    Destination("frost", "Đảo Băng Tuyết", frostSettings, new Vector3(0f, 0f, -560f), 55005, 16, "ash_titan", 5,
                        new[] { frostWolf, caveBat }, frostWyrm),
                    Destination("ancient", "Đảo Cổ Đại", ancientSettings, new Vector3(560f, 0f, 560f), 66006, 23, "frost_wyrm", 6,
                        new[] { ancientSentinel, frostWolf }, ancientCore)
                };
            }
            EditorUtility.SetDirty(catalog);

            BuildingDefinition foundation = CreateBuilding("Foundation", "Nền gỗ", PrimitiveType.Cube,
                new Vector3(3f, 0.35f, 3f), new Color(0.48f, 0.3f, 0.17f), Costs(wood, 5));
            BuildingDefinition wall = CreateBuilding("Wall", "Tường gỗ", PrimitiveType.Cube,
                new Vector3(3f, 3f, 0.28f), new Color(0.43f, 0.27f, 0.15f), Costs(wood, 6));
            BuildingDefinition door = CreateBuilding("Door", "Cửa căn cứ", PrimitiveType.Cube,
                new Vector3(1.2f, 2.6f, 0.24f), new Color(0.3f, 0.19f, 0.1f), Costs(wood, 5, stone, 1));
            BuildingDefinition roof = CreateBuilding("Roof", "Mái che", PrimitiveType.Cube,
                new Vector3(3.2f, 0.24f, 3.2f), new Color(0.35f, 0.25f, 0.17f), Costs(wood, 5));
            BuildingDefinition storage = CreateBuilding("Storage", "Thùng kho", PrimitiveType.Cube,
                new Vector3(1.5f, 1.35f, 1.5f), new Color(0.52f, 0.35f, 0.18f), Costs(wood, 8, stone, 2));
            BuildingDefinition campfire = CreateBuilding("Campfire", "Lửa trại", PrimitiveType.Cylinder,
                new Vector3(1.4f, 0.3f, 1.4f), new Color(0.28f, 0.25f, 0.22f), Costs(stone, 6, wood, 3));
            campfire.providesWarmth = true; campfire.warmthRadius = 12f; campfire.warmthCelsius = 10f;
            BuildingDefinition watchtower = CreateBuilding("Watchtower", "Tháp canh", PrimitiveType.Cube,
                new Vector3(2.4f, 5.5f, 2.4f), new Color(0.36f, 0.27f, 0.18f), Costs(wood, 14, stone, 8));

            BuildingCatalog buildingCatalog = LoadOrCreate<BuildingCatalog>(BuildingCatalogPath, "BuildingCatalog");
            if (buildingCatalog.pieces == null || buildingCatalog.pieces.Length == 0)
                buildingCatalog.pieces = new[] { foundation, wall, door, roof, storage, campfire, watchtower };
            EditorUtility.SetDirty(buildingCatalog);

            return new ContentSet
            {
                archipelago = catalog,
                buildings = buildingCatalog,
                gathering = AssetDatabase.LoadAssetAtPath<GatherSettings>(GatherSettingsPath)
            };
        }

        private static IslandDestination Destination(string id, string name, IslandSettings settings, Vector3 position,
            int seed, int unlockDay, string requiredBoss, int creatureCount, CreatureDefinition[] creatures, BossDefinition boss)
        {
            return new IslandDestination
            {
                id = id, displayName = name, settings = settings, worldPosition = position,
                seed = seed, unlockDay = unlockDay, requiredBossId = requiredBoss,
                creatureCount = creatureCount, creatures = creatures, boss = boss
            };
        }

        private static IslandSettings CreateIslandSettings(string file, Color forest, Color rock, Color sand,
            float mountainHeight, int trees, int rocks, float swampRadius, float swampMoisture)
        {
            IslandSettings settings = LoadOrCreate<IslandSettings>(IslandFolder + "/" + file + ".asset", file);
            if (settings.worldSize == 480f && settings.maxTrees == 400 && settings.maxRocks == 80)
            {
                settings.worldSize = 360f;
                settings.chunksPerSide = 3;
                settings.cellsPerChunk = 16;
                settings.maxTrees = trees;
                settings.maxRocks = rocks;
                settings.caveEntrances = 2;
                settings.mountainHeight = mountainHeight;
                settings.swampRadius = swampRadius;
                settings.swampMoisture = swampMoisture;
                settings.forest = forest;
                settings.rock = rock;
                settings.sand = sand;
                settings.swamp = Color.Lerp(forest, rock, 0.45f);
                settings.snow = file.Contains("Frost") ? new Color(0.88f, 0.96f, 1f)
                    : file.Contains("Ancient") ? new Color(0.7f, 0.66f, 0.82f) : new Color(0.91f, 0.94f, 0.97f);
                settings.seabed = Color.Lerp(rock, new Color(0.08f, 0.17f, 0.22f), 0.55f);
                settings.lowlandHeight = 2.5f;
                settings.hillHeight = Mathf.Max(8f, mountainHeight * 0.42f);
                settings.mountainStartHeight = Mathf.Max(10f, mountainHeight * 0.55f);
                settings.snowStartHeight = file.Contains("Frost") ? 7f : file.Contains("Ancient") ? 34f : 60f;
                settings.seabedDepth = -9f;
                settings.coastRoughness = 0.42f;
                EditorUtility.SetDirty(settings);
            }
            return settings;
        }

        private static CreatureDefinition CreateCreature(string file, string id, string label, float hp, float speed,
            float damage, Color color, ItemData reward)
        {
            CreatureDefinition creature = LoadOrCreate<CreatureDefinition>(CreatureFolder + "/" + file + ".asset", file);
            if (creature.maxHealth == 35f && creature.stableId == "creature")
            {
                creature.stableId = id; creature.displayName = label; creature.maxHealth = hp;
                creature.moveSpeed = speed; creature.damage = damage; creature.bodyColor = color;
                creature.detectionRange = 23f; creature.attackRange = 1.8f; creature.attackCooldown = 2.2f;
                creature.bodyScale = file == "CaveBat" ? 0.65f : 1.05f;
                creature.rewardAmount = 1; creature.rewardItem = reward;
                EditorUtility.SetDirty(creature);
            }
            return creature;
        }

        private static BossDefinition CreateBoss(string file, string id, string label, float hp, int spawnDay,
            float damage, Color color, ItemData reward, float scale, float radius, float areaRadius, float areaDamage)
        {
            BossDefinition boss = LoadOrCreate<BossDefinition>(BossFolder + "/" + file + ".asset", file);
            if (boss.maxHealth == 500f && boss.stableId == "boss")
            {
                boss.stableId = id; boss.displayName = label; boss.maxHealth = hp; boss.spawnDay = spawnDay;
                boss.damage = damage; boss.bodyColor = color; boss.rewardItem = reward;
                boss.minimumPlayers = 1; boss.maximumPlayers = 4;
                boss.phaseTwoDamageMultiplier = 1.55f; boss.moveSpeed = 1.7f + (spawnDay * 0.012f);
                boss.detectionRange = 42f; boss.attackRange = 3.3f; boss.attackCooldown = 2.6f;
                boss.areaAttackRadius = radius; boss.areaAttackDamage = areaDamage;
                boss.bodyScale = scale; boss.rewardAmount = 2;
                EditorUtility.SetDirty(boss);
            }
            return boss;
        }

        private static BuildingDefinition CreateBuilding(string file, string label, PrimitiveType shape,
            Vector3 size, Color color, BuildCost[] costs)
        {
            BuildingDefinition building = LoadOrCreate<BuildingDefinition>(BuildingFolder + "/" + file + ".asset", file);
            if (building.id == "building")
            {
                building.id = file.ToLowerInvariant(); building.displayName = label;
                building.shape = shape; building.size = size; building.color = color;
                building.costs = costs; building.maxHealth = 250f;
                EditorUtility.SetDirty(building);
            }
            return building;
        }

        private static BuildCost[] Costs(ItemData itemA, int amountA, ItemData itemB = null, int amountB = 0)
        {
            if (itemB == null) return new[] { new BuildCost { item = itemA, amount = amountA } };
            return new[] { new BuildCost { item = itemA, amount = amountA }, new BuildCost { item = itemB, amount = amountB } };
        }

        private static ItemData LoadItem(string path, string id, string label, Color color, float weight)
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null) return item;
            item = ScriptableObject.CreateInstance<ItemData>();
            item.name = System.IO.Path.GetFileNameWithoutExtension(path);
            item.id = id; item.displayName = label; item.maxStack = 50; item.color = color; item.weight = weight;
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        private static ItemData CreateItem(string file, string id, string label, Color color, float weight)
        {
            return LoadItem(ItemFolder + "/" + file + ".asset", id, label, color, weight);
        }

        private static T LoadOrCreate<T>(string path, string assetName) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
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

        private static void EnsureInputEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(eventObject, "Create UI Event System");
            }
            else
            {
                StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
                if (legacy != null) Object.DestroyImmediate(legacy, true);
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                    Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
            }
        }

        private static void EnsureActiveSceneInBuildSettings()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path)) return;
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index = scenes.FindIndex(item => item.path == scene.path);
            if (index >= 0) scenes[index] = new EditorBuildSettingsScene(scene.path, true);
            else scenes.Add(new EditorBuildSettingsScene(scene.path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetObject(Object target, string property, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) { Debug.LogError("[Phase 8] Không tìm thấy field " + property + " trên " + target.GetType().Name); return; }
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string property, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) { Debug.LogError("[Phase 8] Không tìm thấy field " + property); return; }
            field.intValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string property, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) { Debug.LogError("[Phase 8] Không tìm thấy field " + property); return; }
            field.boolValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemData[] FindItemAssets()
        {
            string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/_Game/Data" });
            List<ItemData> items = new List<ItemData>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null && seen.Add(item.name)) items.Add(item);
            }
            return items.ToArray();
        }

        private static void SetObjectArray(Object target, string property, Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null || !field.isArray) { Debug.LogError("[Phase 8] Không tìm thấy array " + property); return; }
            field.arraySize = values != null ? values.Length : 0;
            for (int i = 0; i < field.arraySize; i++)
                field.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string property, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) { Debug.LogError("[Phase 8] Không tìm thấy field " + property); return; }
            field.floatValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Object GetReference(Object target, string property)
        {
            if (target == null) return null;
            SerializedProperty field = new SerializedObject(target).FindProperty(property);
            return field != null ? field.objectReferenceValue : null;
        }

        private static int GetInt(Object target, string property)
        {
            if (target == null) return -1;
            SerializedProperty field = new SerializedObject(target).FindProperty(property);
            return field != null ? field.intValue : -1;
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using DaoSuong.Content;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Gameplay
{
    /// <summary>
    /// Builds compact underground dungeons lazily. Only the active cave is enabled, keeping
    /// collider, light and renderer counts low on Android. Loot/discoveries persist locally.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-700)]
    public sealed class CaveManager : MonoBehaviour
    {
        private const string SaveKey = "DAO_SUONG_CAVES_V1";
        private const float UndergroundDepth = 34f;
        private const float RoomSpacing = 22f;

        [Serializable]
        public sealed class CaveLoreRecord
        {
            public string id;
            public string text;
        }

        [Serializable]
        private sealed class RemainingEntry
        {
            public string id;
            public int amount;
        }

        [Serializable]
        private sealed class SaveData
        {
            public int version = 4;
            public string[] collected = Array.Empty<string>();
            public string[] unlocked = Array.Empty<string>();
            public RemainingEntry[] remaining = Array.Empty<RemainingEntry>();
            public CaveLoreRecord[] lore = Array.Empty<CaveLoreRecord>();
        }

        private sealed class CaveRuntime
        {
            public CaveDefinition Definition;
            public Transform Root;
            public Transform EntryAnchor;
            public Vector3 SpawnPoint;
            public float FloorY;
            public bool Built;
            public bool GuardianSpawned;
        }

        public static CaveManager Instance { get; private set; }

        [Header("Content")]
        [SerializeField] private CaveCatalog catalog;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private CampaignManager campaign;
        [SerializeField] private ArchipelagoManager archipelago;
        [SerializeField, Min(20f)] private float undergroundDepth = UndergroundDepth;

        private readonly Dictionary<string, CaveRuntime> caves = new Dictionary<string, CaveRuntime>(StringComparer.Ordinal);
        private readonly HashSet<string> collectedIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> unlockedTreasureIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> remainingLoot = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<CaveLoreRecord> discoveredLore = new List<CaveLoreRecord>();
        private readonly HashSet<string> discoveredLoreIds = new HashSet<string>(StringComparer.Ordinal);
        private Material rockMaterial;
        private Material crystalMaterial;
        private Material portalMaterial;
        private MaterialPropertyBlock propertyBlock;
        private CaveRuntime activeCave;
        private Vector3 returnPosition;
        private Quaternion returnRotation;
        private bool isInside;

        public bool IsInsideCave => isInside;
        public Transform ActiveCaveRoot => activeCave != null ? activeCave.Root : null;
        public float ActiveFloorY => activeCave != null ? activeCave.FloorY : 0f;
        public float ActiveAmbientTemperatureC => activeCave != null && activeCave.Definition != null
            ? activeCave.Definition.ambientTemperatureC : 14f;
        public float ActiveExposure => activeCave != null && activeCave.Definition != null
            ? activeCave.Definition.environmentExposure : 0.08f;
        public string ActiveCaveId => activeCave != null && activeCave.Definition != null ? activeCave.Definition.stableId : string.Empty;
        public IReadOnlyList<CaveLoreRecord> DiscoveredLore => discoveredLore;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[Cave] Chỉ được có một CaveManager trong Scene.", this);
                enabled = false;
                return;
            }
            Instance = this;
            if (archipelago == null) archipelago = GetComponent<ArchipelagoManager>();
            if (campaign == null) campaign = FindFirstObjectByType<CampaignManager>();
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) player = playerObject.transform;
            }
            if (inventory == null && player != null) inventory = player.GetComponent<PlayerInventory>();
            propertyBlock = new MaterialPropertyBlock();
            LoadCollected();
            CreateRuntimeMaterials();
        }

        private void OnDestroy()
        {
            // If the scene is unloaded while the player is underground, never leave a
            // persistent/network player at the negative-Y dungeon coordinate.
            if (isInside && player != null) TeleportPlayer(returnPosition, returnRotation);
            isInside = false;
            activeCave = null;
            if (Instance == this) Instance = null;
            DestroyRuntimeMaterial(rockMaterial);
            DestroyRuntimeMaterial(crystalMaterial);
            DestroyRuntimeMaterial(portalMaterial);
        }

        public void RegisterIsland(IslandDestination destination, IslandGenerator generator)
        {
            if (destination == null || generator == null || catalog == null) return;
            CaveDefinition definition = catalog.FindForIsland(destination.id);
            if (definition == null)
            {
                Debug.LogWarning("[Cave] Không có CaveDefinition cho đảo " + destination.id, this);
                return;
            }
            if (!CaveRules.IsStableIdValid(definition.stableId))
            {
                Debug.LogError("[Cave] Stable ID hang không hợp lệ: " + definition.name, definition);
                return;
            }

            if (!caves.TryGetValue(definition.stableId, out CaveRuntime runtime))
            {
                runtime = new CaveRuntime { Definition = definition };
                caves.Add(definition.stableId, runtime);
            }

            CaveEntrance[] entrances = generator.GetComponentsInChildren<CaveEntrance>(true);
            for (int i = 0; i < entrances.Length; i++)
            {
                CaveEntrance entrance = entrances[i];
                if (entrance == null) continue;
                entrance.Bind(this, definition);
                if (runtime.EntryAnchor == null) runtime.EntryAnchor = entrance.transform;
            }
            if (entrances.Length == 0)
                Debug.LogWarning("[Cave] Đảo " + destination.id + " chưa sinh lối vào hang.", generator);
        }

        public bool HasCollected(string uniqueId)
            => !string.IsNullOrWhiteSpace(uniqueId) && collectedIds.Contains(uniqueId);

        public void MarkCollected(string uniqueId, string loreText = null)
        {
            if (!CaveRules.IsStableIdValid(uniqueId)) return;
            bool changed = remainingLoot.Remove(uniqueId);
            changed |= collectedIds.Add(uniqueId);
            changed |= AddLoreInternal(uniqueId, loreText);
            if (changed) SaveCollected();
        }

        public void DiscoverLore(string uniqueId, string loreText)
        {
            if (!AddLoreInternal(uniqueId, loreText)) return;
            SaveCollected();
        }

        public int GetRemainingAmount(string uniqueId, int defaultAmount)
        {
            if (string.IsNullOrWhiteSpace(uniqueId)) return CaveRules.ClampSavedRemainingAmount(defaultAmount);
            return remainingLoot.TryGetValue(uniqueId, out int saved)
                ? CaveRules.ClampSavedRemainingAmount(saved)
                : CaveRules.ClampSavedRemainingAmount(defaultAmount);
        }

        /// <summary>Commits unlock + partial loot in one save so reload cannot duplicate a partial stack.</summary>
        public void RecordTreasureProgress(string uniqueId, bool unlocked, int remainingAmount, string loreText = null)
        {
            if (!CaveRules.IsStableIdValid(uniqueId)) return;
            if (unlocked) unlockedTreasureIds.Add(uniqueId);
            int remaining = CaveRules.ClampSavedRemainingAmount(remainingAmount);
            if (remaining <= 0)
            {
                remainingLoot.Remove(uniqueId);
                collectedIds.Add(uniqueId);
                AddLoreInternal(uniqueId, loreText);
            }
            else
            {
                collectedIds.Remove(uniqueId);
                remainingLoot[uniqueId] = remaining;
            }
            SaveCollected();
        }

        public bool HasUnlocked(string uniqueId)
            => !string.IsNullOrWhiteSpace(uniqueId) && unlockedTreasureIds.Contains(uniqueId);

        public void MarkUnlocked(string uniqueId)
        {
            if (string.IsNullOrWhiteSpace(uniqueId) || !unlockedTreasureIds.Add(uniqueId)) return;
            SaveCollected();
        }

        public bool IsInsideActiveCave(Transform actor)
        {
            return isInside && actor != null && activeCave != null && activeCave.Root != null &&
                   actor.IsChildOf(activeCave.Root);
        }

        public bool TryEnter(CaveEntrance entrance)
        {
            CaveDefinition definition = entrance != null ? entrance.Definition : null;
            bool finished = campaign != null && campaign.IsFinished;
            if (!CaveRules.CanEnter(definition != null, isInside, finished) || player == null ||
                !CaveRules.IsStableIdValid(definition.stableId))
                return false;
            if (!caves.TryGetValue(definition.stableId, out CaveRuntime runtime))
            {
                runtime = new CaveRuntime { Definition = definition, EntryAnchor = entrance.transform };
                caves[definition.stableId] = runtime;
            }
            // An island may have multiple surface entrances leading to the same cave.
            // Anchor the shared dungeon at the actual entrance used this time.
            runtime.EntryAnchor = entrance.transform;
            if (runtime.Built && runtime.Root != null)
                PositionRuntimeAtEntrance(runtime, entrance.transform);
            else
            {
                runtime.Built = false;
                EnsureBuilt(runtime);
            }
            if (runtime.Root == null) return false;

            returnPosition = player.position;
            returnRotation = player.rotation;
            activeCave = runtime;
            isInside = true;
            runtime.Root.gameObject.SetActive(true);
            TeleportPlayer(runtime.SpawnPoint, runtime.Root.rotation);
            HudMessages.Toast("Đã vào " + definition.displayName + ". Hãy tìm dấu tích và kho báu ẩn.");
            HudMessages.SetPrompt("Khám phá hang · chạm TÁC ĐỘNG tại cổng phát sáng để trở ra");
            return true;
        }

        public bool ExitCave()
        {
            if (!isInside || activeCave == null) return false;
            CaveRuntime previous = activeCave;
            isInside = false;
            activeCave = null;
            if (player != null) TeleportPlayer(returnPosition, returnRotation);
            if (previous.Root != null) previous.Root.gameObject.SetActive(false);
            HudMessages.Toast("Bạn đã trở lại mặt đất.");
            HudMessages.SetPrompt(string.Empty);
            return true;
        }

        public void ForceExit()
        {
            if (isInside) ExitCave();
        }

        /// <summary>Starts a clean 99-day attempt: remove generated caves and per-run loot/lore progress.</summary>
        public void ResetForNewRun()
        {
            ForceExit();
            foreach (KeyValuePair<string, CaveRuntime> pair in caves)
            {
                CaveRuntime runtime = pair.Value;
                if (runtime == null || runtime.Root == null) continue;
                runtime.Root.gameObject.SetActive(false);
                Destroy(runtime.Root.gameObject);
            }
            caves.Clear();
            collectedIds.Clear();
            unlockedTreasureIds.Clear();
            remainingLoot.Clear();
            discoveredLore.Clear();
            discoveredLoreIds.Clear();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        private void EnsureBuilt(CaveRuntime runtime)
        {
            if (runtime.Built) return;
            if (runtime.EntryAnchor == null || runtime.Definition == null)
            {
                Debug.LogError("[Cave] Thiếu điểm neo hoặc dữ liệu hang.", this);
                return;
            }

            GameObject root = new GameObject("Dungeon_" + runtime.Definition.stableId);
            root.transform.SetParent(transform, true);
            runtime.Root = root.transform;
            if (!PositionRuntimeAtEntrance(runtime, runtime.EntryAnchor))
            {
                Destroy(root);
                runtime.Root = null;
                return;
            }

            int roomCount = CaveRules.ClampRoomCount(runtime.Definition.roomCount);
            int secretRoomIndex = Mathf.Clamp(roomCount / 2, 1, roomCount - 1);
            for (int room = 0; room < roomCount; room++)
                BuildRoom(runtime, room, roomCount, room == secretRoomIndex);
            BuildEntrancePortal(runtime);
            BuildLoot(runtime, roomCount);
            BuildGuardian(runtime, roomCount);
            runtime.SpawnPoint = runtime.Root.TransformPoint(new Vector3(0f, 0.2f, 8f));
            runtime.Built = true;
            root.SetActive(false);
        }

        private bool PositionRuntimeAtEntrance(CaveRuntime runtime, Transform entrance)
        {
            if (runtime == null || runtime.Root == null || entrance == null) return false;
            Vector3 outward = Vector3.ProjectOnPlane(entrance.forward, Vector3.up).normalized;
            if (outward.sqrMagnitude < 0.001f) outward = Vector3.forward;
            Quaternion rotation = Quaternion.LookRotation(-outward, Vector3.up);
            Vector3 origin = entrance.position + Vector3.down * Mathf.Max(20f, undergroundDepth);
            runtime.Root.SetPositionAndRotation(origin, rotation);
            runtime.FloorY = origin.y;
            runtime.SpawnPoint = runtime.Root.TransformPoint(new Vector3(0f, 0.2f, 8f));
            return true;
        }

        private void BuildRoom(CaveRuntime runtime, int index, int count, bool hasSecretPassage)
        {
            float centerZ = 10f + index * RoomSpacing;
            Color rock = runtime.Definition.rockColor;
            CreateBlock(runtime.Root, "CaveFloor_" + index, new Vector3(0f, -0.5f, centerZ), new Vector3(16f, 1f, 16f), Color.Lerp(rock, Color.black, 0.18f));
            CreateBlock(runtime.Root, "CaveCeiling_" + index, new Vector3(0f, 6.5f, centerZ), new Vector3(16f, 1f, 16f), Color.Lerp(rock, Color.black, 0.08f));
            CreateBlock(runtime.Root, "CaveWallL_" + index, new Vector3(-7.75f, 3f, centerZ), new Vector3(0.5f, 6f, 16f), rock);
            if (hasSecretPassage)
            {
                // Leave a side opening, then seal it with an interactable rock slab.
                // The player must discover and remove the seal with the Action button.
                CreateBlock(runtime.Root, "CaveWallR_A_" + index, new Vector3(7.75f, 3f, centerZ - 5.45f), new Vector3(0.5f, 6f, 5.1f), rock);
                CreateBlock(runtime.Root, "CaveWallR_B_" + index, new Vector3(7.75f, 3f, centerZ + 5.45f), new Vector3(0.5f, 6f, 5.1f), rock);
                BuildSecretAlcove(runtime, index, centerZ, rock);
                BuildSecretDoor(runtime, index, centerZ, rock);
            }
            else
            {
                CreateBlock(runtime.Root, "CaveWallR_" + index, new Vector3(7.75f, 3f, centerZ), new Vector3(0.5f, 6f, 16f), rock);
            }
            if (index == count - 1)
                CreateBlock(runtime.Root, "CaveEndWall", new Vector3(0f, 3f, centerZ + 8f), new Vector3(16f, 6f, 0.6f), rock);

            if (index < count - 1)
            {
                float tunnelZ = centerZ + 11f;
                CreateBlock(runtime.Root, "TunnelFloor_" + index, new Vector3(0f, -0.5f, tunnelZ), new Vector3(5f, 1f, 6f), Color.Lerp(rock, Color.black, 0.2f));
                CreateBlock(runtime.Root, "TunnelCeiling_" + index, new Vector3(0f, 6.5f, tunnelZ), new Vector3(5f, 1f, 6f), rock);
                CreateBlock(runtime.Root, "TunnelWallL_" + index, new Vector3(-2.75f, 3f, tunnelZ), new Vector3(0.5f, 6f, 6f), rock);
                CreateBlock(runtime.Root, "TunnelWallR_" + index, new Vector3(2.75f, 3f, tunnelZ), new Vector3(0.5f, 6f, 6f), rock);
            }

            CreateCrystal(runtime.Root, new Vector3(-6.8f, 4.1f, centerZ - 3f), runtime.Definition.crystalColor);
            CreateCrystal(runtime.Root, new Vector3(6.8f, 4.5f, centerZ + 3f), runtime.Definition.crystalColor);
            CreateLight(runtime.Root, new Vector3(0f, 4.6f, centerZ), runtime.Definition.crystalColor, 13f, 0.8f);
        }

        private void BuildSecretDoor(CaveRuntime runtime, int roomIndex, float centerZ, Color rock)
        {
            GameObject sealObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sealObject.name = "HiddenStoneSeal_" + roomIndex;
            sealObject.transform.SetParent(runtime.Root, false);
            sealObject.transform.localPosition = new Vector3(7.75f, 3f, centerZ);
            sealObject.transform.localScale = new Vector3(0.5f, 6f, 5.8f);
            ApplyMaterial(sealObject.GetComponent<Renderer>(), rockMaterial, Color.Lerp(rock, Color.black, 0.24f));

            CaveTreasure seal = sealObject.AddComponent<CaveTreasure>();
            string sealId = runtime.Definition.stableId + "/stone_seal_" + roomIndex;
            seal.Initialize(this, sealId, null, 0, null, false,
                "Vách đá đã trượt sang bên. Phía sau là một nhánh hang bị che giấu và kho đồ cũ của những người sống sót.");
        }

        private void BuildSecretAlcove(CaveRuntime runtime, int roomIndex, float centerZ, Color rock)
        {
            // Narrow side tunnel and a small chamber beyond the main room's right wall.
            CreateBlock(runtime.Root, "SecretPassageFloor_" + roomIndex, new Vector3(10.5f, -0.5f, centerZ), new Vector3(6f, 1f, 5f), Color.Lerp(rock, Color.black, 0.2f));
            CreateBlock(runtime.Root, "SecretPassageCeiling_" + roomIndex, new Vector3(10.5f, 6.5f, centerZ), new Vector3(6f, 1f, 5f), rock);
            CreateBlock(runtime.Root, "SecretPassageWallA_" + roomIndex, new Vector3(10.5f, 3f, centerZ - 2.75f), new Vector3(6f, 6f, 0.5f), rock);
            CreateBlock(runtime.Root, "SecretPassageWallB_" + roomIndex, new Vector3(10.5f, 3f, centerZ + 2.75f), new Vector3(6f, 6f, 0.5f), rock);

            CreateBlock(runtime.Root, "SecretAlcoveFloor_" + roomIndex, new Vector3(18f, -0.5f, centerZ), new Vector3(10f, 1f, 10f), Color.Lerp(rock, Color.black, 0.18f));
            CreateBlock(runtime.Root, "SecretAlcoveCeiling_" + roomIndex, new Vector3(18f, 6.5f, centerZ), new Vector3(10f, 1f, 10f), rock);
            CreateBlock(runtime.Root, "SecretAlcoveWallOuter_" + roomIndex, new Vector3(23.25f, 3f, centerZ), new Vector3(0.5f, 6f, 10f), rock);
            CreateBlock(runtime.Root, "SecretAlcoveWallFront_" + roomIndex, new Vector3(18f, 3f, centerZ + 5.25f), new Vector3(10f, 6f, 0.5f), rock);
            CreateBlock(runtime.Root, "SecretAlcoveWallBack_" + roomIndex, new Vector3(18f, 3f, centerZ - 5.25f), new Vector3(10f, 6f, 0.5f), rock);
            CreateCrystal(runtime.Root, new Vector3(21.1f, 3.8f, centerZ - 2.5f), runtime.Definition.crystalColor);
            CreateLight(runtime.Root, new Vector3(18f, 4.8f, centerZ), runtime.Definition.crystalColor, 9f, 0.65f);
        }

        private void BuildEntrancePortal(CaveRuntime runtime)
        {
            GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = "CaveExitPortal";
            portal.transform.SetParent(runtime.Root, false);
            portal.transform.localPosition = new Vector3(0f, 0.65f, 2f);
            portal.transform.localScale = new Vector3(2.2f, 0.18f, 2.2f);
            ApplyMaterial(portal.GetComponent<Renderer>(), portalMaterial, new Color(0.1f, 0.75f, 0.9f));
            Collider oldCollider = portal.GetComponent<Collider>();
            if (oldCollider != null)
            {
                oldCollider.enabled = false;
                Destroy(oldCollider);
            }
            SphereCollider trigger = portal.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.8f;
            portal.AddComponent<CaveExitPortal>().Initialize(this);
        }

        private void BuildLoot(CaveRuntime runtime, int roomCount)
        {
            CaveLootEntry[] entries = runtime.Definition.loot;
            if (entries == null || entries.Length == 0) return;
            var random = new System.Random(StableHash(runtime.Definition.stableId));
            for (int i = 0; i < entries.Length; i++)
            {
                CaveLootEntry entry = entries[i];
                if (entry == null || !CaveRules.IsStableIdValid(entry.stableId)) continue;
                if (!CaveRules.ShouldSpawnLoot((float)random.NextDouble(), entry.spawnChance)) continue;
                int min = Mathf.Clamp(entry.minAmount, 0, 999);
                int max = Mathf.Clamp(entry.maxAmount, min, 999);
                int amount = min == max ? min : random.Next(min, max + 1);
                string uniqueId = runtime.Definition.stableId + "/" + entry.stableId;
                int remaining = GetRemainingAmount(uniqueId, amount);
                bool hasLore = !string.IsNullOrWhiteSpace(entry.secretText);
                if (!CaveRules.ShouldCreateTreasure(entry.item != null, remaining, hasLore)) continue;

                // A zero-amount item row may still carry a lore/key interaction, but must
                // not appear as an impossible "Pick up Item x0" object.
                ItemData rewardItem = remaining > 0 ? entry.item : null;
                int rewardAmount = rewardItem != null ? remaining : 0;
                int room = Mathf.Clamp(entry.roomIndex, 0, roomCount - 1);
                float centerZ = 10f + room * RoomSpacing;
                Vector3 localPosition = new Vector3((float)(random.NextDouble() * 8.5 - 4.25), 0.8f,
                    centerZ + (float)(random.NextDouble() * 7.5 - 3.75));
                GameObject treasureObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                treasureObject.name = "CaveTreasure_" + entry.stableId;
                treasureObject.transform.SetParent(runtime.Root, false);
                treasureObject.transform.localPosition = localPosition;
                treasureObject.transform.localScale = rewardItem != null ? Vector3.one * 0.8f : Vector3.one * 0.55f;
                SphereCollider collider = treasureObject.GetComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 1.15f;
                Renderer renderer = treasureObject.GetComponent<Renderer>();
                Color color = rewardItem != null ? rewardItem.color : runtime.Definition.crystalColor;
                ApplyMaterial(renderer, crystalMaterial, color);
                CaveTreasure treasure = treasureObject.AddComponent<CaveTreasure>();
                treasure.Initialize(this, uniqueId, rewardItem, rewardAmount, entry.requiredKey, entry.consumeKey, entry.secretText);
            }

            if (!string.IsNullOrWhiteSpace(runtime.Definition.hiddenStory))
            {
                int lastRoom = roomCount - 1;
                float centerZ = 10f + lastRoom * RoomSpacing;
                GameObject memoryObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                memoryObject.name = "HiddenStoryMemory";
                memoryObject.transform.SetParent(runtime.Root, false);
                memoryObject.transform.localPosition = new Vector3(5.4f, 0.85f, centerZ + 3.1f);
                memoryObject.transform.localScale = Vector3.one * 0.65f;
                SphereCollider memoryCollider = memoryObject.GetComponent<SphereCollider>();
                memoryCollider.isTrigger = true;
                memoryCollider.radius = 1.25f;
                ApplyMaterial(memoryObject.GetComponent<Renderer>(), crystalMaterial, runtime.Definition.crystalColor);
                CaveTreasure memory = memoryObject.AddComponent<CaveTreasure>();
                memory.Initialize(this, runtime.Definition.stableId + "/hidden_story", null, 0, null, false,
                    runtime.Definition.hiddenStory);
            }

            // Every cave has a discoverable side cache. It is saved independently from the ordinary loot.
            CaveLootEntry cacheSource = null;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].item != null && entries[i].minAmount > 0)
                {
                    cacheSource = entries[i];
                    break;
                }
            }
            if (cacheSource != null)
            {
                int secretRoom = Mathf.Clamp(roomCount / 2, 1, roomCount - 1);
                float secretZ = 10f + secretRoom * RoomSpacing;
                GameObject cacheObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cacheObject.name = "HiddenCache";
                cacheObject.transform.SetParent(runtime.Root, false);
                cacheObject.transform.localPosition = new Vector3(18f, 0.75f, secretZ);
                cacheObject.transform.localScale = new Vector3(1.15f, 1.1f, 1.15f);
                BoxCollider cacheCollider = cacheObject.GetComponent<BoxCollider>();
                cacheCollider.isTrigger = true;
                ApplyMaterial(cacheObject.GetComponent<Renderer>(), crystalMaterial, cacheSource.item.color);
                CaveTreasure cache = cacheObject.AddComponent<CaveTreasure>();
                int cacheAmount = Mathf.Clamp(cacheSource.minAmount, 1, 3);
                string cacheId = runtime.Definition.stableId + "/hidden_cache";
                cache.Initialize(this, cacheId, cacheSource.item, GetRemainingAmount(cacheId, cacheAmount),
                    null, false, "Kho bí mật đã được tìm thấy. Dấu khắc trên rương trùng với ký hiệu ở lối vào.");
            }
        }

        private void BuildGuardian(CaveRuntime runtime, int roomCount)
        {
            CreatureDefinition guardian = runtime.Definition.guardian;
            if (guardian == null || runtime.GuardianSpawned || player == null) return;
            float z = 10f + (roomCount - 1) * RoomSpacing;
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "CaveGuardian_" + guardian.stableId;
            enemy.transform.SetParent(runtime.Root, false);
            enemy.transform.localPosition = new Vector3(0f, 1.6f, z);
            CreatureAgent agent = enemy.AddComponent<CreatureAgent>();
            agent.Initialize(guardian, null, player, campaign, inventory, archipelago);
            runtime.GuardianSpawned = true;
        }

        private void CreateCrystal(Transform parent, Vector3 localPosition, Color color)
        {
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crystal.name = "LuminousCrystal";
            crystal.transform.SetParent(parent, false);
            crystal.transform.localPosition = localPosition;
            crystal.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
            Collider collider = crystal.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
            ApplyMaterial(crystal.GetComponent<Renderer>(), crystalMaterial, color);
        }

        private void CreateLight(Transform parent, Vector3 localPosition, Color color, float range, float intensity)
        {
            GameObject lightObject = new GameObject("CaveCrystalLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private void CreateBlock(Transform parent, string objectName, Vector3 localPosition, Vector3 scale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = objectName;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = scale;
            ApplyMaterial(block.GetComponent<Renderer>(), rockMaterial, color);
        }

        private void ApplyMaterial(Renderer renderer, Material material, Color color)
        {
            if (renderer == null) return;
            if (material != null) renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            propertyBlock.SetColor("_EmissionColor", color * 0.35f);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void CreateRuntimeMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogWarning("[Cave] Không tìm thấy URP/Lit hoặc Standard; vật thể hang dùng material mặc định.", this);
                return;
            }
            rockMaterial = new Material(shader) { name = "Runtime_CaveRock" };
            crystalMaterial = new Material(shader) { name = "Runtime_CaveCrystal" };
            portalMaterial = new Material(shader) { name = "Runtime_CavePortal" };
            ConfigureMaterial(rockMaterial, new Color(0.2f, 0.22f, 0.25f));
            ConfigureMaterial(crystalMaterial, new Color(0.2f, 0.7f, 0.85f));
            ConfigureMaterial(portalMaterial, new Color(0.15f, 0.8f, 1f));
        }

        private static void ConfigureMaterial(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.2f);
            }
        }

        private static void DestroyRuntimeMaterial(Material material)
        {
            if (material == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(material);
            else UnityEngine.Object.DestroyImmediate(material);
        }

        private void TeleportPlayer(Vector3 position, Quaternion rotation)
        {
            if (player == null) return;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.SetPositionAndRotation(position, rotation);
            if (controller != null) controller.enabled = true;
            PlayerController movement = player.GetComponent<PlayerController>();
            if (movement != null) movement.ResetMotion();
            InputService.Current?.ClearPendingInput();
        }

        private void LoadCollected()
        {
            collectedIds.Clear();
            unlockedTreasureIds.Clear();
            remainingLoot.Clear();
            discoveredLore.Clear();
            discoveredLoreIds.Clear();
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.version < 1 || data.version > 4) return;
                if (data.collected != null)
                {
                    for (int i = 0; i < data.collected.Length; i++)
                        if (!string.IsNullOrWhiteSpace(data.collected[i])) collectedIds.Add(data.collected[i]);
                }
                if (data.unlocked != null)
                {
                    for (int i = 0; i < data.unlocked.Length; i++)
                        if (!string.IsNullOrWhiteSpace(data.unlocked[i])) unlockedTreasureIds.Add(data.unlocked[i]);
                }
                if (data.remaining != null)
                {
                    for (int i = 0; i < data.remaining.Length; i++)
                    {
                        RemainingEntry entry = data.remaining[i];
                        if (entry == null || !CaveRules.IsStableIdValid(entry.id)) continue;
                        int amount = CaveRules.ClampSavedRemainingAmount(entry.amount);
                        if (amount <= 0) collectedIds.Add(entry.id);
                        else remainingLoot[entry.id] = amount;
                    }
                }
                if (data.lore != null)
                {
                    for (int i = 0; i < data.lore.Length; i++)
                    {
                        CaveLoreRecord entry = data.lore[i];
                        if (entry != null) AddLoreInternal(entry.id, entry.text);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Cave] Save hang không hợp lệ; bỏ qua dữ liệu cũ: " + exception.Message, this);
            }
        }

        private void SaveCollected()
        {
            SaveData data = new SaveData
            {
                version = 4,
                collected = new List<string>(collectedIds).ToArray(),
                unlocked = new List<string>(unlockedTreasureIds).ToArray(),
                remaining = BuildRemainingEntries(),
                lore = discoveredLore.ToArray()
            };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private bool AddLoreInternal(string uniqueId, string loreText)
        {
            if (!CaveRules.IsStableIdValid(uniqueId) || string.IsNullOrWhiteSpace(loreText) ||
                !discoveredLoreIds.Add(uniqueId)) return false;
            discoveredLore.Add(new CaveLoreRecord { id = uniqueId, text = loreText.Trim() });
            return true;
        }

        private RemainingEntry[] BuildRemainingEntries()
        {
            RemainingEntry[] entries = new RemainingEntry[remainingLoot.Count];
            int index = 0;
            foreach (KeyValuePair<string, int> pair in remainingLoot)
                entries[index++] = new RemainingEntry { id = pair.Key, amount = CaveRules.ClampSavedRemainingAmount(pair.Value) };
            return entries;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                if (value == null) return hash;
                for (int i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash;
            }
        }
    }
}

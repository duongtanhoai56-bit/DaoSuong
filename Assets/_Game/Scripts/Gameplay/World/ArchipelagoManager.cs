using System;
using System.Collections.Generic;
using DaoSuong.Content;
using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-800)]
    public sealed class ArchipelagoManager : MonoBehaviour
    {
        public static ArchipelagoManager Instance { get; private set; }

        [Header("World content")]
        [SerializeField] private ArchipelagoCatalog catalog;
        [SerializeField] private IslandGenerator templateGenerator;
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private GatherSettings gatherSettings;
        [SerializeField] private Material pickupMaterial;
        [SerializeField] private Transform player;
        [SerializeField] private CampaignManager campaign;
        [SerializeField] private CaveManager caveManager;
        [SerializeField] private int normalCreatureSpawnOffset = 1700;

        private readonly Dictionary<string, IslandGenerator> generators = new Dictionary<string, IslandGenerator>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> bossSpawned = new Dictionary<string, bool>(StringComparer.Ordinal);
        private float nextBossCheck;

        public ArchipelagoCatalog Catalog => catalog;
        public IReadOnlyList<IslandDestination> Destinations => catalog != null ? (IReadOnlyList<IslandDestination>)catalog.islands : Array.Empty<IslandDestination>();
        public Transform Player => player;
        public CampaignManager Campaign => campaign;
        public string ActiveIslandId { get; private set; }
        public event Action<string> IslandChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[Archipelago] Chỉ được có một ArchipelagoManager trong Scene.", this);
                enabled = false;
                return;
            }
            Instance = this;
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) player = playerObject.transform;
            }
            if (templateGenerator == null) templateGenerator = FindFirstObjectByType<IslandGenerator>();
            if (campaign == null) campaign = FindFirstObjectByType<CampaignManager>();
            if (caveManager == null) caveManager = GetComponent<CaveManager>();
        }

        private void Start()
        {
            GenerateArchipelago();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextBossCheck) return;
            nextBossCheck = Time.unscaledTime + 1f;
            TrySpawnUnlockedBosses();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void GenerateArchipelago()
        {
            generators.Clear();
            bossSpawned.Clear();
            if (catalog == null || catalog.islands == null || catalog.islands.Length == 0)
            {
                Debug.LogError("[Archipelago] Thiếu ArchipelagoCatalog hoặc danh sách đảo rỗng.", this);
                return;
            }
            if (templateGenerator == null || terrainMaterial == null)
            {
                Debug.LogError("[Archipelago] Thiếu IslandGenerator mẫu hoặc Material địa hình.", this);
                return;
            }
            if (player == null)
            {
                Debug.LogError("[Archipelago] Không tìm thấy Player có tag Player.", this);
                return;
            }

            bool usedTemplate = false;
            IslandGenerator home = null;
            for (int i = 0; i < catalog.islands.Length; i++)
            {
                IslandDestination destination = catalog.islands[i];
                if (destination == null || string.IsNullOrWhiteSpace(destination.id) || destination.settings == null)
                {
                    Debug.LogWarning("[Archipelago] Bỏ qua cấu hình đảo thiếu ID hoặc IslandSettings ở vị trí " + i, this);
                    continue;
                }
                if (generators.ContainsKey(destination.id))
                {
                    Debug.LogWarning("[Archipelago] ID đảo bị trùng, bỏ qua: " + destination.id, this);
                    continue;
                }

                IslandGenerator generator;
                if (!usedTemplate)
                {
                    generator = templateGenerator;
                    usedTemplate = true;
                    home = generator;
                }
                else
                {
                    GameObject islandRoot = new GameObject("Island_" + destination.id);
                    islandRoot.transform.SetPositionAndRotation(destination.worldPosition, Quaternion.identity);
                    generator = islandRoot.AddComponent<IslandGenerator>();
                }

                generator.transform.SetPositionAndRotation(destination.worldPosition, Quaternion.identity);
                // Remote generators must not each monitor/teleport the same player.
                generator.Configure(destination.settings, terrainMaterial, destination.seed, generator == templateGenerator ? player : null, false);
                ConfigureGathering(generator);
                generator.Generate(destination.seed, false);
                if (caveManager == null) caveManager = GetComponent<CaveManager>();
                if (caveManager != null) caveManager.RegisterIsland(destination, generator);
                generators.Add(destination.id, generator);
                bossSpawned[destination.id] = false;
                SpawnNormalCreatures(destination, generator, i);
            }

            if (home != null && catalog.islands.Length > 0)
            {
                IslandDestination first = FindDestinationForGenerator(home);
                if (first != null) TravelTo(first.id, true);
                else
                {
                    CharacterController controller = player.GetComponent<CharacterController>();
                    if (controller != null) controller.enabled = false;
                    player.position = home.SpawnPoint + Vector3.up * 0.15f;
                    if (controller != null) controller.enabled = true;
                }
            }
            Debug.Log("[Archipelago] Đã sinh " + generators.Count + " đảo theo seed cố định.", this);
        }


        /// <summary>Repopulates ordinary enemies and resets boss spawn state for a new 99-day attempt.</summary>
        public void ResetForNewRun()
        {
            if (caveManager != null) caveManager.ResetForNewRun();
            CreatureAgent[] existing = FindObjectsByType<CreatureAgent>(FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] == null) continue;
                // Cave guardians are children of CaveManager-owned dungeon roots; the
                // reset above destroys those roots. Avoid scheduling a second Destroy.
                if (caveManager != null && existing[i].transform.IsChildOf(caveManager.transform)) continue;
                Destroy(existing[i].gameObject);
            }

            bossSpawned.Clear();
            if (catalog != null && catalog.islands != null)
            {
                for (int i = 0; i < catalog.islands.Length; i++)
                {
                    IslandDestination destination = catalog.islands[i];
                    if (destination == null || string.IsNullOrWhiteSpace(destination.id)) continue;
                    bossSpawned[destination.id] = false;
                    if (generators.TryGetValue(destination.id, out IslandGenerator generator) && generator != null)
                        SpawnNormalCreatures(destination, generator, i);
                }
                if (catalog.islands.Length > 0 && catalog.islands[0] != null)
                    TravelTo(catalog.islands[0].id, true);
            }
        }

        public bool TryTravel(string islandId) => TravelTo(islandId, false);

        public bool TravelTo(string islandId, bool ignoreUnlock)
        {
            if (caveManager != null && caveManager.IsInsideCave)
            {
                HudMessages.Toast("Hãy trở lại mặt đất trước khi di chuyển sang đảo khác.");
                return false;
            }
            IslandDestination destination = catalog != null ? catalog.Find(islandId) : null;
            if (destination == null || !generators.TryGetValue(islandId, out IslandGenerator generator) || generator == null || !generator.IsGenerated)
                return false;
            if (!ignoreUnlock && campaign != null && campaign.IsFinished) return false;
            if (!ignoreUnlock && campaign != null && !campaign.IsIslandUnlocked(destination))
            {
                HudMessages.Toast("Chưa mở khóa: " + destination.displayName);
                return false;
            }
            if (player == null) return false;

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            Vector3 facing = generator.transform.position - generator.SpawnPoint;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.001f) facing = Vector3.forward;
            player.SetPositionAndRotation(generator.SpawnPoint + Vector3.up * 0.18f, Quaternion.LookRotation(facing.normalized));
            if (controller != null) controller.enabled = true;
            PlayerController movement = player.GetComponent<PlayerController>();
            if (movement != null) movement.ResetMotion();
            ActiveIslandId = destination.id;
            IslandChanged?.Invoke(ActiveIslandId);
            HudMessages.Toast("Đã đến " + destination.displayName);
            return true;
        }

        public bool IsUnlocked(IslandDestination destination)
            => destination != null && (campaign == null || campaign.IsIslandUnlocked(destination));

        public IslandGenerator GetGenerator(string islandId)
            => !string.IsNullOrWhiteSpace(islandId) && generators.TryGetValue(islandId, out IslandGenerator value) ? value : null;

        public IslandGenerator GetIslandAt(Vector3 worldPosition)
        {
            IslandGenerator nearest = null;
            float nearestSqr = float.PositiveInfinity;
            foreach (IslandGenerator generator in generators.Values)
            {
                if (generator == null || !generator.IsGenerated) continue;
                if (generator.ContainsWorldPosition(worldPosition)) return generator;
                Vector3 center = generator.transform.position;
                float distance = (new Vector2(worldPosition.x - center.x, worldPosition.z - center.z)).sqrMagnitude;
                if (distance < nearestSqr) { nearestSqr = distance; nearest = generator; }
            }
            return nearest;
        }

        public float GetGroundHeight(Vector3 worldPosition, out bool isLand)
        {
            IslandGenerator island = GetIslandAt(worldPosition);
            if (island == null || !island.ContainsWorldPosition(worldPosition))
            {
                isLand = false;
                return 0f;
            }
            float height = island.GetHeight(worldPosition.x, worldPosition.z);
            isLand = height > 0.05f;
            return height;
        }

        private void ConfigureGathering(IslandGenerator generator)
        {
            if (gatherSettings == null)
            {
                Debug.LogWarning("[Archipelago] Chưa gán GatherSettings; đảo vẫn sinh địa hình nhưng không có cây/đá tương tác.", this);
                return;
            }
            ResourceSpawner spawner = generator.GetComponent<ResourceSpawner>();
            if (spawner == null) spawner = generator.gameObject.AddComponent<ResourceSpawner>();
            spawner.Configure(generator, gatherSettings, pickupMaterial != null ? pickupMaterial : terrainMaterial);
        }

        private void SpawnNormalCreatures(IslandDestination destination, IslandGenerator generator, int islandIndex)
        {
            if (destination.creatures == null || destination.creatures.Length == 0 || destination.creatureCount <= 0) return;
            for (int i = 0; i < destination.creatureCount; i++)
            {
                CreatureDefinition definition = destination.creatures[i % destination.creatures.Length];
                if (definition == null || !generator.TryGetRandomLandPoint(normalCreatureSpawnOffset + destination.seed + i * 97, out Vector3 spawn, true)) continue;
                GameObject creatureObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                CreatureAgent agent = creatureObject.AddComponent<CreatureAgent>();
                agent.Initialize(definition, null, player, campaign, player.GetComponent<PlayerInventory>(), this);
                Vector3 groundPosition = spawn;
                creatureObject.transform.position = groundPosition + Vector3.up * creatureObject.transform.localScale.y;
            }
        }

        private void TrySpawnUnlockedBosses()
        {
            if (campaign == null || campaign.IsFinished || campaign.Clock == null || catalog == null) return;
            for (int i = 0; i < catalog.islands.Length; i++)
            {
                IslandDestination destination = catalog.islands[i];
                if (destination == null || destination.boss == null || campaign.IsBossDefeated(destination.boss.stableId)) continue;
                if (campaign.Clock.DayCount < destination.boss.spawnDay) continue;
                if (bossSpawned.TryGetValue(destination.id, out bool hasSpawned) && hasSpawned) continue;
                if (!generators.TryGetValue(destination.id, out IslandGenerator generator) || generator == null) continue;
                if (!generator.TryGetRandomLandPoint(StableHash(destination.id) + destination.seed, out Vector3 spawn, true)) continue;

                GameObject bossObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                CreatureAgent agent = bossObject.AddComponent<CreatureAgent>();
                agent.Initialize(null, destination.boss, player, campaign, player.GetComponent<PlayerInventory>(), this);
                bossObject.transform.position = spawn + Vector3.up * bossObject.transform.localScale.y;
                bossSpawned[destination.id] = true;
                HudMessages.Toast("BOSS XUẤT HIỆN: " + destination.boss.displayName);
            }
        }

        private IslandDestination FindDestinationForGenerator(IslandGenerator generator)
        {
            for (int i = 0; i < catalog.islands.Length; i++)
            {
                IslandDestination destination = catalog.islands[i];
                if (destination != null && generators.TryGetValue(destination.id, out IslandGenerator current) && current == generator)
                    return destination;
            }
            return null;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash;
            }
        }
    }
}

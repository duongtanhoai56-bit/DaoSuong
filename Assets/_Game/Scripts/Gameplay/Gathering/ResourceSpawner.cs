using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ResourceSpawner : MonoBehaviour
    {
        [SerializeField] private IslandGenerator island;
        [SerializeField] private GatherSettings settings;
        [SerializeField] private Material pickupMaterial;

        private readonly List<ResourceNode> nodes = new List<ResourceNode>();
        private readonly List<GatherTarget> targets = new List<GatherTarget>();
        private readonly List<GatherTarget> pendingRespawns = new List<GatherTarget>();
        private Transform pickupRoot;

        public IReadOnlyList<ResourceNode> Nodes => nodes;

        public void Configure(IslandGenerator islandGenerator, GatherSettings gatherSettings, Material sharedPickupMaterial)
        {
            enabled = false;
            island = islandGenerator;
            settings = gatherSettings;
            pickupMaterial = sharedPickupMaterial;
            enabled = true;
        }

        private void OnEnable()
        {
            if (island == null || settings == null) return;
            island.Generated += Build;
            if (island.IsGenerated) Build();
        }

        private void Start()
        {
            if (island == null || settings == null)
                Debug.LogError("[Gather] ResourceSpawner thiếu Island hoặc GatherSettings. Gán reference trong Inspector hoặc gọi Configure trước khi Play.", this);
        }

        private void OnDisable()
        {
            if (island != null) island.Generated -= Build;
            UnsubscribeTargets();
        }

        private void Update()
        {
            for (int i = pendingRespawns.Count - 1; i >= 0; i--)
            {
                GatherTarget target = pendingRespawns[i];
                if (target == null)
                {
                    pendingRespawns.RemoveAt(i);
                    continue;
                }
                if (Time.time < target.RespawnAt) continue;

                target.Respawn();
                pendingRespawns.RemoveAt(i);
            }
        }

        public void RespawnAllNow()
        {
            for (int i = 0; i < pendingRespawns.Count; i++)
                if (pendingRespawns[i] != null) pendingRespawns[i].Respawn();
            pendingRespawns.Clear();
        }

        private void Build()
        {
            UnsubscribeTargets();
            nodes.Clear();
            pendingRespawns.Clear();

            if (pickupRoot != null)
            {
                if (Application.isPlaying) Destroy(pickupRoot.gameObject);
                else DestroyImmediate(pickupRoot.gameObject);
            }

            pickupRoot = new GameObject("WorldPickups").transform;
            pickupRoot.SetParent(transform, false);
            WorldPickup.DefaultMaterial = pickupMaterial;

            // IDs are stable generation-order indices for equal island seed/settings.
            int nextNodeId = 0;
            IReadOnlyList<GameObject> trees = island.TreeObjects;
            for (int i = 0; i < trees.Count; i++)
                AttachNode(trees[i], settings.tree, nextNodeId++);

            IReadOnlyList<GameObject> rocks = island.RockObjects;
            for (int i = 0; i < rocks.Count; i++)
            {
                GameObject rock = rocks[i];
                if (rock == null) continue;
                Vector3 position = rock.transform.position;
                bool mountain = island.GetBiome(position.x, position.z) == Biome.Mountain;
                AttachNode(rock, mountain ? settings.rockMountain : settings.rockDefault, nextNodeId++);
            }

            SpawnPickups(new DeterministicRandom(unchecked(island.Seed * 31 + 7)));
            Debug.Log($"[Gather] nodes={nodes.Count}, pickups={(pickupRoot != null ? pickupRoot.childCount : 0)}, seed={island.Seed}", this);
        }

        private void AttachNode(GameObject target, ResourceNodeData data, int id)
        {
            if (target == null || data == null) return;
            ResourceNode node = target.GetComponent<ResourceNode>();
            if (node == null) node = target.AddComponent<ResourceNode>();
            node.Initialize(id, data);
            node.Depleted += OnTargetDepleted;
            nodes.Add(node);
            targets.Add(node);
        }

        private void SpawnPickups(DeterministicRandom random)
        {
            if (settings.pickups == null) return;
            float halfSize = island.HalfSize - 6f;

            for (int ruleIndex = 0; ruleIndex < settings.pickups.Length; ruleIndex++)
            {
                PickupRule rule = settings.pickups[ruleIndex];
                if (rule == null || rule.item == null || rule.count <= 0) continue;

                int spawned = 0;
                int attempts = 0;
                int maximumAttempts = Mathf.Max(40, rule.count * 50);
                while (spawned < rule.count && attempts++ < maximumAttempts)
                {
                    // IslandGenerator queries accept world-space coordinates. Remote islands are translated.
                    float x = island.transform.position.x + random.Range(-halfSize, halfSize);
                    float z = island.transform.position.z + random.Range(-halfSize, halfSize);
                    Biome biome = island.GetBiome(x, z);
                    if (!rule.Allows(biome)) continue;

                    float height = island.GetHeight(x, z);
                    if (height < 0.05f) continue;
                    int minimum = Mathf.Max(0, rule.minAmount);
                    int maximum = Mathf.Max(minimum, rule.maxAmount);
                    int amount = random.Range(minimum, maximum == int.MaxValue ? int.MaxValue : maximum + 1);
                    Vector3 position = new Vector3(x, height + 0.2f, z);
                    float yaw = random.Range(0f, 360f);
                    WorldPickup pickup = WorldPickup.Spawn(rule.item, amount, position, pickupRoot, yaw);
                    if (pickup == null) continue;

                    pickup.RespawnDelay = rule.respawnSeconds;
                    pickup.Depleted += OnTargetDepleted;
                    targets.Add(pickup);
                    spawned++;
                }

                if (spawned < rule.count)
                    Debug.LogWarning($"[Gather] Chỉ đặt được {spawned}/{rule.count} pickup cho {rule.item.displayName}; biome/địa hình không đủ vùng phù hợp.", this);
            }
        }

        private void OnTargetDepleted(GatherTarget target)
        {
            if (target != null && !pendingRespawns.Contains(target))
                pendingRespawns.Add(target);
        }

        private void UnsubscribeTargets()
        {
            for (int i = 0; i < targets.Count; i++)
            {
                GatherTarget target = targets[i];
                if (target != null) target.Depleted -= OnTargetDepleted;
            }
            targets.Clear();
        }

        private void OnValidate()
        {
            if (pickupMaterial == null && island != null)
                Debug.LogWarning("ResourceSpawner: cần gán M_Terrain cho Pickup Material.", this);
        }
    }
}

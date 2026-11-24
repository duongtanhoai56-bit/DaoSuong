using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class BaseBuildingManager : MonoBehaviour
    {
        private const string SaveKey = "DAO_SUONG_BASE_V1";
        [SerializeField] private BuildingCatalog catalog;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private SurvivalController survival;
        [SerializeField] private Transform player;
        [SerializeField] private ArchipelagoManager archipelago;
        [SerializeField] private ItemData[] persistentItemCatalog = Array.Empty<ItemData>();
        [SerializeField, Min(1f)] private float placementDistance = 4f;
        [SerializeField, Min(0.5f)] private float gridSize = 2f;

        [Serializable]
        private sealed class BuildingRecord
        {
            public string id;
            public Vector3 position;
            public float yaw;
        }

        [Serializable]
        private sealed class BuildingSave
        {
            public int version = 1;
            public BuildingRecord[] buildings = Array.Empty<BuildingRecord>();
        }

        private readonly List<PlacedBuilding> placedBuildings = new List<PlacedBuilding>();
        public BuildingCatalog Catalog => catalog;
        public IReadOnlyList<PlacedBuilding> PlacedBuildings => placedBuildings;
        public event Action<BuildingDefinition> BuildingPlaced;

        private void Awake()
        {
            if (player == null) player = transform;
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (survival == null) survival = GetComponent<SurvivalController>();
            if (archipelago == null) archipelago = FindFirstObjectByType<ArchipelagoManager>();
        }

        private void Start() => LoadBuildings();

        public bool TryBuild(BuildingDefinition definition)
        {
            if (definition == null || player == null || inventory == null ||
                (survival != null && survival.IsDead) || Time.timeScale <= 0f) return false;
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 groundPoint = player.position + forward * placementDistance;
            groundPoint.x = Mathf.Round(groundPoint.x / gridSize) * gridSize;
            groundPoint.z = Mathf.Round(groundPoint.z / gridSize) * gridSize;

            bool onLand = archipelago != null && archipelago.GetGroundHeight(groundPoint, out bool land) > -0.01f && land;
            float terrainHeight = 0f;
            if (onLand) terrainHeight = archipelago.GetGroundHeight(groundPoint, out _);
            float halfHeight = GetVerticalHalfHeight(definition);
            Vector3 center = new Vector3(groundPoint.x, terrainHeight + halfHeight, groundPoint.z);
            float distance = Vector3.Distance(player.position, center);
            Quaternion rotation = Quaternion.Euler(0f, Mathf.Round(player.eulerAngles.y / 15f) * 15f, 0f);
            Vector3 checkExtents = new Vector3(definition.size.x * 0.47f, halfHeight * 0.94f, definition.size.z * 0.47f);
            bool blocked = Physics.CheckBox(center, checkExtents, rotation,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (!CampaignProgressRules.IsValidPlacement(distance, placementDistance + definition.size.magnitude * 0.35f, onLand, blocked))
            {
                HudMessages.Toast("Không thể xây ở vị trí này. Hãy chọn nền đất trống.");
                return false;
            }

            BuildCost[] costs = definition.costs ?? Array.Empty<BuildCost>();
            for (int i = 0; i < costs.Length; i++)
            {
                BuildCost cost = costs[i];
                if (cost == null || cost.item == null || cost.amount <= 0 || inventory.Count(cost.item) < cost.amount)
                {
                    HudMessages.Toast("Thiếu nguyên liệu để xây " + definition.displayName);
                    return false;
                }
            }

            InventoryModel snapshot = inventory.Model.Clone();
            for (int i = 0; i < costs.Length; i++)
            {
                BuildCost cost = costs[i];
                if (!inventory.Model.TryRemove(cost.item, cost.amount))
                {
                    inventory.Model.RestoreFrom(snapshot);
                    inventory.NotifyChanged();
                    HudMessages.Toast("Không thể trừ nguyên liệu; thao tác đã được hoàn tác.");
                    return false;
                }
            }

            PlacedBuilding building = CreateBuilding(definition, center, rotation);
            if (building == null)
            {
                inventory.Model.RestoreFrom(snapshot);
                inventory.NotifyChanged();
                return false;
            }
            inventory.NotifyChanged();
            placedBuildings.Add(building);
            SaveBuildings();
            BuildingPlaced?.Invoke(definition);
            HudMessages.Toast("Đã xây: " + definition.displayName);
            return true;
        }

        private PlacedBuilding CreateBuilding(BuildingDefinition definition, Vector3 position, Quaternion rotation)
        {
            if (definition == null) return null;
            GameObject instance = GameObject.CreatePrimitive(definition.shape);
            instance.name = "Base_" + definition.id;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = definition.size;
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = definition.color;
            PlacedBuilding placed = instance.AddComponent<PlacedBuilding>();
            placed.Initialize(definition.id, definition.maxHealth);

            if (string.Equals(definition.id, "storage", StringComparison.Ordinal))
            {
                StorageContainer storage = instance.AddComponent<StorageContainer>();
                storage.ConfigurePersistence(BuildStorageKey(definition, position), persistentItemCatalog);
            }

            if (definition.providesWarmth)
            {
                WarmthSource warmth = instance.AddComponent<WarmthSource>();
                warmth.Configure(definition.warmthRadius, definition.warmthCelsius);
                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "FireGlow";
                flame.transform.SetParent(instance.transform, false);
                flame.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                flame.transform.localScale = Vector3.one * 0.35f;
                Collider flameCollider = flame.GetComponent<Collider>();
                if (flameCollider != null) Destroy(flameCollider);
                Renderer flameRenderer = flame.GetComponent<Renderer>();
                if (flameRenderer != null) flameRenderer.material.color = new Color(1f, 0.35f, 0.06f);
                Light light = flame.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = definition.warmthRadius;
                light.intensity = 2.2f;
                light.color = new Color(1f, 0.45f, 0.16f);
            }
            return placed;
        }


        private static float GetVerticalHalfHeight(BuildingDefinition definition)
        {
            // Unity's built-in Cylinder and Capsule meshes are twice as tall as their unit scale.
            return definition.shape == PrimitiveType.Cylinder || definition.shape == PrimitiveType.Capsule
                ? definition.size.y : definition.size.y * 0.5f;
        }

        private static string BuildStorageKey(BuildingDefinition definition, Vector3 position)
        {
            return definition == null ? string.Empty : BuildStorageKey(definition.id, position);
        }

        private static string BuildStorageKey(string definitionId, Vector3 position)
        {
            if (string.IsNullOrWhiteSpace(definitionId) ||
                float.IsNaN(position.x) || float.IsInfinity(position.x) ||
                float.IsNaN(position.z) || float.IsInfinity(position.z)) return string.Empty;
            return "DAO_SUONG_CHEST_V1_" + definitionId + "_" +
                   Mathf.RoundToInt(position.x * 10f) + "_" + Mathf.RoundToInt(position.z * 10f);
        }

        private void DeleteSavedChestPersistenceKeys()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            try
            {
                BuildingSave save = JsonUtility.FromJson<BuildingSave>(PlayerPrefs.GetString(SaveKey));
                if (save == null || save.version != 1 || save.buildings == null) return;
                for (int i = 0; i < save.buildings.Length; i++)
                {
                    BuildingRecord record = save.buildings[i];
                    if (record == null || !string.Equals(record.id, "storage", StringComparison.Ordinal)) continue;
                    string chestKey = BuildStorageKey(record.id, record.position);
                    if (!string.IsNullOrEmpty(chestKey)) PlayerPrefs.DeleteKey(chestKey);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Building] Không đọc được danh sách rương cũ khi reset: " + exception.Message, this);
            }
        }

        /// <summary>Clears placed structures and chest contents for a genuinely fresh campaign run.</summary>
        public void ResetForNewRun()
        {
            // Also clear keys for saved storage buildings that could not be re-instantiated
            // (for example, if a catalog definition is missing during load).
            DeleteSavedChestPersistenceKeys();
            for (int i = placedBuildings.Count - 1; i >= 0; i--)
            {
                PlacedBuilding building = placedBuildings[i];
                if (building == null) continue;

                StorageContainer storage = building.GetComponent<StorageContainer>();
                if (storage != null) storage.ClearPersistence();
                if (Application.isPlaying) Destroy(building.gameObject);
                else DestroyImmediate(building.gameObject);
            }

            placedBuildings.Clear();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        private void SaveBuildings()
        {
            List<BuildingRecord> records = new List<BuildingRecord>();
            for (int i = placedBuildings.Count - 1; i >= 0; i--)
            {
                PlacedBuilding building = placedBuildings[i];
                if (building == null) { placedBuildings.RemoveAt(i); continue; }
                records.Add(new BuildingRecord
                {
                    id = building.DefinitionId,
                    position = building.transform.position,
                    yaw = building.transform.eulerAngles.y
                });
            }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new BuildingSave { buildings = records.ToArray() }));
            PlayerPrefs.Save();
        }

        private void LoadBuildings()
        {
            if (catalog == null || !PlayerPrefs.HasKey(SaveKey)) return;
            try
            {
                BuildingSave save = JsonUtility.FromJson<BuildingSave>(PlayerPrefs.GetString(SaveKey));
                if (save == null || save.version != 1 || save.buildings == null) return;
                for (int i = 0; i < save.buildings.Length; i++)
                {
                    BuildingRecord record = save.buildings[i];
                    if (record == null) continue;
                    BuildingDefinition definition = catalog.Find(record.id);
                    if (definition == null) continue;
                    if (archipelago != null)
                    {
                        bool land;
                        archipelago.GetGroundHeight(record.position, out land);
                        if (!land) continue;
                    }
                    PlacedBuilding building = CreateBuilding(definition, record.position,
                        Quaternion.Euler(0f, record.yaw, 0f));
                    if (building != null) placedBuildings.Add(building);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Building] Save căn cứ không hợp lệ: " + exception.Message, this);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveBuildings();
        }

        private void OnApplicationQuit() => SaveBuildings();

        private void OnValidate()
        {
            placementDistance = Mathf.Clamp(placementDistance, 1f, 12f);
            gridSize = Mathf.Clamp(gridSize, 0.5f, 8f);
        }
    }
}

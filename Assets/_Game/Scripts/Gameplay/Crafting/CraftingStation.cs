using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CraftingStation : MonoBehaviour
    {
        [SerializeField, Range(1, 2)] private int tier = 1;
        [SerializeField] private string displayName = "Bàn chế tạo";
        [Header("Tùy chọn đặt thử cạnh spawn") ]
        [SerializeField] private bool placeNearPlayerSpawn;
        [SerializeField] private IslandGenerator island;
        [SerializeField] private Transform player;
        [SerializeField, Min(1f)] private float forwardOffset = 4f;
        [SerializeField] private float sideOffset;

        public int Tier => tier;
        public string DisplayName => displayName;

        private void Start()
        {
            if (!placeNearPlayerSpawn || island == null || player == null) return;
            if (island.IsGenerated) PlaceNearSpawn();
            else island.Generated += OnIslandGenerated;
        }

        private void OnDestroy()
        {
            if (island != null) island.Generated -= OnIslandGenerated;
        }

        private void OnIslandGenerated() => PlaceNearSpawn();

        private void PlaceNearSpawn()
        {
            if (island != null) island.Generated -= OnIslandGenerated;
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 point = player.position + forward * forwardOffset + right * sideOffset;
            float height = island.GetHeight(point.x, point.z);
            transform.position = new Vector3(point.x, height + 0.5f, point.z);
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        private void OnValidate()
        {
            tier = Mathf.Clamp(tier, 1, 2);
            if (string.IsNullOrWhiteSpace(displayName)) displayName = tier == 2 ? "Lò rèn" : "Bàn chế tạo";
        }
    }
}

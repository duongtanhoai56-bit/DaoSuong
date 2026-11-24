using System;
using Unity.Netcode;
using UnityEngine;
using Game.Gameplay;

namespace Game.Network
{
    /// <summary>
    /// A lightweight network proxy. The local scene Player remains the owning client's
    /// controller/camera/inventory; this proxy publishes its pose for remote teammates.
    /// The server validates incoming movement before changing the server-authoritative transform.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    public sealed class NetworkPlayerAvatar : NetworkBehaviour
    {
        [SerializeField, Range(5f, 30f)] private float sendRateHz = 20f;
        [SerializeField, Min(3f)] private float maxValidatedSpeed = 10f;
        [SerializeField, Min(0f)] private float speedTolerance = 2.5f;
        [SerializeField, Min(0f)] private float packetSlack = 0.05f;
        [SerializeField, Min(1f)] private float initialSpawnRadius = 14f;

        private Transform localPlayer;
        private float sendTimer;
        private uint nextSequence;
        private uint lastServerSequence;
        private double lastServerPacketTime;
        private Vector3 lastAcceptedPosition;
        private bool receivedFirstPose;
        private Renderer[] renderers;
        private IslandGenerator serverIsland;

        public override void OnNetworkSpawn()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            SetAvatarVisible(!IsOwner);
            if (IsOwner)
                TryFindLocalPlayer();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;
            if (localPlayer == null && !TryFindLocalPlayer()) return;

            sendTimer += Time.unscaledDeltaTime;
            float interval = 1f / Mathf.Max(5f, sendRateHz);
            if (sendTimer < interval) return;
            sendTimer %= interval;

            SubmitOwnerPoseServerRpc(localPlayer.position, localPlayer.rotation, nextSequence++);
        }

        private bool TryFindLocalPlayer()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null || playerObject == gameObject) return false;

            // Island generation moves the local controller to the deterministic beach spawn.
            IslandGenerator island = FindFirstObjectByType<IslandGenerator>();
            if (island != null && !island.IsGenerated) return false;
            localPlayer = playerObject.transform;
            return true;
        }

        [ServerRpc(RequireOwnership = true, Delivery = RpcDelivery.Unreliable)]
        private void SubmitOwnerPoseServerRpc(Vector3 position, Quaternion rotation, uint sequence)
        {
            if (!IsServer || !NetworkAvatarRules.IsFinite(position) || !NetworkAvatarRules.IsValidRotation(rotation))
                return;

            // Unreliable packets can be lost or arrive out of order. Never allow an old pose
            // to rewind the remote proxy after a newer pose has already been accepted.
            if (receivedFirstPose && !NetworkAvatarRules.IsNewerSequence(sequence, lastServerSequence))
                return;

            if (serverIsland == null)
                serverIsland = FindFirstObjectByType<IslandGenerator>();
            IslandGenerator island = serverIsland;
            // Do not fall back to guessed bounds or accept a first pose before the
            // authoritative island has generated its spawn. The owner will retry next tick.
            if (island == null || !island.IsGenerated)
                return;

            if (!NetworkAvatarRules.IsWithinWorldBounds(position, island.HalfSize, 8f, -15f, 100f))
                return;

            // The first pose must begin near the deterministic island spawn. This stops a
            // joining client from teleporting anywhere inside the world bounds on its first packet.
            // All clients must use the same island seed until Phase 19 synchronizes the seed.
            if (!receivedFirstPose)
            {
                Vector3 expectedSpawn = island.SpawnPoint + Vector3.up * 1.2f;
                if (!NetworkAvatarRules.IsValidInitialPose(true, position, expectedSpawn, initialSpawnRadius))
                    return;
            }

            // Use unscaled time because the movement validator must not become stricter
            // when gameplay timeScale is changed by UI, slow motion, or pause effects.
            double now = Time.unscaledTimeAsDouble;
            if (receivedFirstPose)
            {
                double elapsed = Math.Max(0.05d, now - lastServerPacketTime);
                if (!NetworkAvatarRules.IsWithinSpeed(lastAcceptedPosition, position, elapsed,
                        maxValidatedSpeed, speedTolerance, packetSlack))
                    return;
            }

            rotation = NetworkAvatarRules.NormalizeSafe(rotation);
            Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) return;

            lastAcceptedPosition = position;
            lastServerPacketTime = now;
            lastServerSequence = sequence;
            receivedFirstPose = true;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward.normalized, Vector3.up));
        }

        private void SetAvatarVisible(bool visible)
        {
            if (renderers == null) return;
            foreach (Renderer renderer in renderers)
                if (renderer != null) renderer.enabled = visible;
        }
    }
}

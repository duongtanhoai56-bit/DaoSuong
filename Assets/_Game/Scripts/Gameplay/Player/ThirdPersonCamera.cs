using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField, Min(0.1f)] private float distance = 5f;
        [SerializeField, Min(0f)] private float height = 1.6f;

        [Header("Look")]
        [SerializeField] private Vector2 pitchLimits = new Vector2(-20f, 60f);
        [SerializeField] private float initialPitch = 20f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.3f;
        [SerializeField, Min(0f)] private float collisionBuffer = 0.08f;

        private float yaw;
        private float pitch;
        private bool initializedYaw;

        private void Awake()
        {
            pitch = Mathf.Clamp(initialPitch, pitchLimits.x, pitchLimits.y);
            if (target != null)
                yaw = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            // Island generation places and rotates the player during Start. Read its final
            // heading on the first LateUpdate, after all Start methods have run.
            if (!initializedYaw)
            {
                yaw = target.eulerAngles.y;
                initializedYaw = true;
            }

            IPlayerInput input = InputService.Current;
            if (input != null)
            {
                Vector2 lookDelta = input.ConsumeLook();
                float sensitivity = ControlsSettingsStore.Current.lookSensitivity;
                yaw += lookDelta.x * sensitivity;
                pitch = Mathf.Clamp(pitch - lookDelta.y * sensitivity, pitchLimits.x, pitchLimits.y);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * height;
            Vector3 backward = rotation * Vector3.back;
            float resolvedDistance = distance;

            // Exclude the Player layer in the Inspector. SphereCast keeps the near plane
            // outside nearby walls instead of allowing the camera to pass through them.
            if (Physics.SphereCast(
                    pivot,
                    collisionRadius,
                    backward,
                    out RaycastHit hit,
                    distance,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                // Never force a minimum camera offset past a nearby wall. If an
                // obstruction is extremely close, move the camera closer to the pivot.
                resolvedDistance = Mathf.Clamp(hit.distance - collisionBuffer, 0f, distance);
            }

            transform.SetPositionAndRotation(pivot + backward * resolvedDistance, rotation);
        }

        /// <summary>Allows scene setup tools and tests to bind the target explicitly.</summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                initializedYaw = true;
            }
        }

        private void OnValidate()
        {
            distance = Mathf.Max(0.1f, distance);
            collisionRadius = Mathf.Max(0.05f, collisionRadius);
            float lowerPitch = Mathf.Min(pitchLimits.x, pitchLimits.y);
            float upperPitch = Mathf.Max(pitchLimits.x, pitchLimits.y);
            pitchLimits = new Vector2(lowerPitch, upperPitch);
        }
    }
}

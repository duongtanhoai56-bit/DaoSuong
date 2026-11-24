using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7f;
        [SerializeField] private SurvivalController survival;
        [SerializeField, Min(0f)] private float turnSpeed = 14f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedGravity = -2f;

        private CharacterController characterController;
        private float verticalVelocity;
        private bool wasDead;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            ResolveCamera();
        }

        private void Update()
        {
            if (cameraTransform == null)
                ResolveCamera();

            if (cameraTransform == null || characterController == null)
            {
                if (survival != null) survival.SetSprinting(false);
                return;
            }

            IPlayerInput input = InputService.Current;
            bool dead = survival != null && survival.IsDead;
            if (dead && !wasDead) input?.ClearPendingInput();
            wasDead = dead;
            Vector2 moveInput = !dead && input != null ? Vector2.ClampMagnitude(input.Move, 1f) : Vector2.zero;
            bool jumpPressed = !dead && input != null && input.ConsumeJump();

            Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (cameraForward.sqrMagnitude < 0.0001f)
                cameraForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            cameraForward.Normalize();

            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
            Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

            bool wantsSprint = !dead && input != null && input.SprintHeld && moveDirection.sqrMagnitude > 0.01f;
            bool sprinting = wantsSprint && survival != null && survival.TrySpendSprintStamina(Time.deltaTime);
            if (survival != null) survival.SetSprinting(sprinting);

            bool grounded = characterController.isGrounded;
            if (grounded && verticalVelocity < 0f)
                verticalVelocity = groundedGravity;

            // Jump is an edge-triggered command from PointerDown / Space, never a held button.
            if (jumpPressed && grounded)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = moveDirection * (sprinting ? sprintSpeed : moveSpeed);
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime);
            }
        }

        public void ResetMotion()
        {
            verticalVelocity = 0f;
            wasDead = survival != null && survival.IsDead;
            InputService.Current?.ClearPendingInput();
        }

        private void ResolveCamera()
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void OnDisable()
        {
            if (survival != null) survival.SetSprinting(false);
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            sprintSpeed = Mathf.Max(moveSpeed, sprintSpeed);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            jumpHeight = Mathf.Max(0f, jumpHeight);
            if (gravity >= -0.01f)
                gravity = -25f;
        }
    }
}

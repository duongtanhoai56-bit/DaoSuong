using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerGatherer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private IslandGenerator island;
        [SerializeField] private GatherSettings settings;
        [SerializeField] private Transform aimTransform;
        [SerializeField] private SurvivalController survival;

        [Header("Interaction")]
        [SerializeField, Min(0.5f)] private float reach = 2.4f;
        [SerializeField, Range(30f, 120f)] private float maximumAngle = 80f;
        [SerializeField, Min(0.1f)] private float handSwingInterval = 0.8f;

        [Header("Fishing")]
        [SerializeField, Min(0.1f)] private float fishingMinimumWait = 3f;
        [SerializeField, Min(0.1f)] private float fishingMaximumWait = 8f;
        [SerializeField, Min(0.1f)] private float fishingBiteWindow = 1.3f;

        private readonly Collider[] overlapBuffer = new Collider[64];
        private System.Random random;
        private FishingSession fishing;
        private float nextSwingTime;
        private bool biteBuzzed;

        private void Awake()
        {
            random = new System.Random(unchecked(GetInstanceID() * 397 ^ Environment.TickCount));
            fishing = new FishingSession(random, fishingMinimumWait, fishingMaximumWait, fishingBiteWindow);
            if (survival == null) survival = GetComponent<SurvivalController>();
            if (aimTransform == null && Camera.main != null)
                aimTransform = Camera.main.transform;
        }

        private void OnDisable()
        {
            if (fishing != null)
            {
                fishing.Cancel();
                fishing.Reset();
            }
            ClearHud();
        }

        private void Update()
        {
            if (GameplayInputGate.IsLocked)
            {
                ClearHud();
                return;
            }

            if (survival != null && survival.IsDead)
            {
                if (fishing.Phase == FishingPhase.Waiting || fishing.Phase == FishingPhase.Bite)
                {
                    fishing.Cancel();
                    fishing.Reset();
                }
                ClearHud();
                return;
            }

            IPlayerInput input = InputService.Current;
            if (input == null || inventory == null)
            {
                ClearHud();
                return;
            }

            bool actionPressed = input.ConsumeAction();
            bool actionHeld = input.ActionHeld || actionPressed;

            if (fishing.Phase != FishingPhase.Idle)
            {
                UpdateFishing(input, actionPressed);
                return;
            }

            GatherTarget target = FindTarget();
            WorldPickup pickup = target as WorldPickup;
            if (pickup != null)
            {
                HandlePickup(pickup, actionPressed);
                return;
            }

            CaveEntrance caveEntrance = target as CaveEntrance;
            if (caveEntrance != null)
            {
                HudMessages.SetPrompt("Chạm nút TÁC ĐỘNG để khám phá " + caveEntrance.Definition.displayName);
                HudMessages.SetProgress(-1f);
                HudMessages.SetTool(string.Empty, 0f);
                if (actionPressed) caveEntrance.Interact();
                return;
            }

            CaveExitPortal caveExit = target as CaveExitPortal;
            if (caveExit != null)
            {
                HudMessages.SetPrompt("Chạm nút TÁC ĐỘNG để trở lại mặt đất");
                HudMessages.SetProgress(-1f);
                HudMessages.SetTool(string.Empty, 0f);
                if (actionPressed) caveExit.Interact();
                return;
            }

            CaveTreasure treasure = target as CaveTreasure;
            if (treasure != null)
            {
                string actionText = treasure.Item != null && treasure.Amount > 0
                    ? "Chạm nút TÁC ĐỘNG để nhặt " + treasure.Item.displayName + " x" + treasure.Amount
                    : "Chạm nút TÁC ĐỘNG để khám phá bí mật";
                HudMessages.SetPrompt(actionText);
                HudMessages.SetProgress(-1f);
                HudMessages.SetTool(string.Empty, 0f);
                if (actionPressed) treasure.Interact(inventory);
                return;
            }

            ResourceNode node = target as ResourceNode;
            if (node != null)
            {
                HandleNode(node, actionHeld);
                return;
            }

            CaveManager activeCave = CaveManager.Instance;
            if (activeCave != null && activeCave.IsInsideCave)
            {
                HudMessages.SetPrompt("Đang khám phá hang động · tìm lối đi và vật phẩm ẩn");
                HudMessages.SetProgress(-1f);
                HudMessages.SetTool(string.Empty, 0f);
                return;
            }

            TryStartFishing(actionPressed);
        }

        private void HandlePickup(WorldPickup pickup, bool pressed)
        {
            HudMessages.SetPrompt($"Chạm nút Tác động để nhặt {pickup.Item.displayName} x{pickup.Amount}");
            HudMessages.SetProgress(-1f);
            HudMessages.SetTool(string.Empty, 0f);
            if (pressed) pickup.Collect(inventory);
        }

        private void HandleNode(ResourceNode node, bool held)
        {
            ResourceNodeData data = node.Data;
            ItemStack tool = data.requiredTool == ToolType.None
                ? null
                : inventory.PickTool(data.requiredTool, data.minTier);

            if (data.requiredTool != ToolType.None && tool == null)
            {
                HudMessages.SetPrompt($"Cần {ToolNames.Of(data.requiredTool)} (cấp {data.minTier}) để {data.verb} {data.displayName}");
                HudMessages.SetProgress(-1f);
                HudMessages.SetTool(string.Empty, 0f);
                return;
            }

            float power = tool != null ? tool.Tool.power : 1f;
            int hitsRemaining = GatherRules.HitsNeeded(node.Health, power);
            HudMessages.SetPrompt($"Giữ nút Tác động để {data.verb} {data.displayName} · còn {hitsRemaining} nhát");
            HudMessages.SetProgress(data.health > 0f ? node.Health / data.health : 0f);
            HudMessages.SetTool(tool != null ? tool.item.displayName : string.Empty,
                tool != null ? Durability01(tool) : 0f);

            if (!held || Time.time < nextSwingTime) return;
            nextSwingTime = Time.time + (tool != null ? tool.Tool.swingInterval : handSwingInterval);
            Face(node.transform.position);
            node.ApplyHit(power, inventory, random);
            if (tool != null) Wear(tool, data.durabilityCostPerHit);
        }

        private GatherTarget FindTarget()
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            int count = Physics.OverlapSphereNonAlloc(origin, reach, overlapBuffer, ~0,
                QueryTriggerInteraction.Collide);

            GatherTarget bestTarget = null;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = overlapBuffer[i];
                if (hitCollider == null) continue;
                GatherTarget candidate = hitCollider.GetComponentInParent<GatherTarget>();
                if (candidate == null || !candidate.IsAvailable) continue;

                Vector3 closest = hitCollider.ClosestPoint(origin);
                Vector3 flat = Vector3.ProjectOnPlane(closest - origin, Vector3.up);
                Vector3 aimForward = aimTransform != null ? aimTransform.forward : transform.forward;
                aimForward = Vector3.ProjectOnPlane(aimForward, Vector3.up).normalized;
                float angle = flat.sqrMagnitude < 0.04f
                    ? 0f
                    : Vector3.Angle(aimForward, flat);
                if (angle > maximumAngle) continue;

                float score = flat.magnitude + angle * 0.02f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }
            return bestTarget;
        }

        private void Face(Vector3 worldPosition)
        {
            Vector3 direction = worldPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void Wear(ItemStack stack, int cost)
        {
            if (stack == null || stack.Tool == null) return;
            stack.durability = GatherRules.WearDurability(stack.durability, cost);
            if (stack.durability > 0)
            {
                inventory.NotifyChanged();
                return;
            }

            HudMessages.Toast($"{stack.item.displayName} đã hỏng!");
            inventory.RemoveBroken();
        }

        private static float Durability01(ItemStack stack)
        {
            return stack != null && stack.Tool != null
                ? Mathf.Clamp01(stack.durability / (float)stack.Tool.maxDurability)
                : 0f;
        }

        private bool IsFacingWater()
        {
            if (island == null || !island.IsGenerated) return false;

            Vector3 position = transform.position;
            Vector3 forward = aimTransform != null ? aimTransform.forward : transform.forward;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            float nearHeight = island.GetHeight(position.x + forward.x * 2.5f,
                                                position.z + forward.z * 2.5f);
            float farHeight = island.GetHeight(position.x + forward.x * 4.5f,
                                               position.z + forward.z * 4.5f);
            return nearHeight < -0.3f || farHeight < -0.3f;
        }

        private void TryStartFishing(bool pressed)
        {
            ItemStack rod = inventory.PickTool(ToolType.FishingRod, 1);
            if (rod == null || !IsFacingWater())
            {
                ClearHud();
                return;
            }

            HudMessages.SetTool(rod.item.displayName, Durability01(rod));
            HudMessages.SetProgress(-1f);
            HudMessages.SetPrompt("Chạm nút Tác động để thả câu");
            if (!pressed) return;

            Wear(rod, 1);
            fishing.Cast();
            biteBuzzed = false;
            HudMessages.Toast("Đã thả câu...");
        }

        private void UpdateFishing(IPlayerInput input, bool pressed)
        {
            if ((fishing.Phase == FishingPhase.Waiting || fishing.Phase == FishingPhase.Bite) &&
                input.Move.sqrMagnitude > 0.09f)
            {
                fishing.Cancel();
            }
            else if (pressed)
            {
                // Process the current-frame tap before the timer expires on this frame.
                fishing.Press();
            }
            else
            {
                fishing.Tick(Time.deltaTime);
            }

            switch (fishing.Phase)
            {
                case FishingPhase.Waiting:
                    HudMessages.SetPrompt("Đang chờ cá cắn câu... (di chuyển để thu cần)");
                    HudMessages.SetProgress(-1f);
                    break;
                case FishingPhase.Bite:
                    HudMessages.SetPrompt("CÁ CẮN CÂU! BẤM TÁC ĐỘNG NGAY!");
                    HudMessages.SetProgress(fishing.BiteFraction);
                    if (!biteBuzzed)
                    {
                        biteBuzzed = true;
                        Vibrate();
                    }
                    break;
                case FishingPhase.Done:
                    FinishFishing();
                    break;
            }
        }

        private void FinishFishing()
        {
            switch (fishing.Result)
            {
                case FishingResult.Caught:
                    GiveFish();
                    break;
                case FishingResult.Escaped:
                    HudMessages.Toast("Cá chạy mất rồi!");
                    break;
                default:
                    HudMessages.Toast("Bạn đã thu cần câu về");
                    break;
            }
            fishing.Reset();
            biteBuzzed = false;
            ClearHud();
        }

        private void GiveFish()
        {
            if (settings == null || settings.fishing == null)
            {
                HudMessages.Toast("Chưa cấu hình bảng cá");
                return;
            }

            ItemData fish = settings.fishing.Roll(random, out int amount);
            if (fish == null || amount <= 0)
            {
                HudMessages.Toast("Cá tuột mất rồi!");
                return;
            }

            int left = inventory.Add(fish, amount);
            int received = amount - left;
            if (received > 0) HudMessages.Toast($"+{received} {fish.displayName}");
            if (left > 0)
                WorldPickup.Spawn(fish, left, transform.position + transform.forward + Vector3.up * 0.3f);
        }

        private static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private static void ClearHud()
        {
            HudMessages.SetPrompt(string.Empty);
            HudMessages.SetProgress(-1f);
            HudMessages.SetTool(string.Empty, 0f);
        }

        private void OnValidate()
        {
            reach = Mathf.Max(0.5f, reach);
            handSwingInterval = Mathf.Max(0.1f, handSwingInterval);
            fishingMinimumWait = Mathf.Max(0.1f, fishingMinimumWait);
            fishingMaximumWait = Mathf.Max(fishingMinimumWait, fishingMaximumWait);
            fishingBiteWindow = Mathf.Max(0.1f, fishingBiteWindow);
        }
    }
}

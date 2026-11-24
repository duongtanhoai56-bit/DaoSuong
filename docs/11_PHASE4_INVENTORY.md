# Phase 4 — Inventory, stacks, weight and storage

## Scope

This phase replaces the unbounded temporary backpack with a slot inventory while preserving the stable `IItemReceiver.Add(ItemData, int)` and `PlayerInventory.PickTool(ToolType, int)` contracts. It adds a lightweight chest container and runtime-generated touch UI. It does not yet add cloud persistence, trading, crafting transactions or multiplayer ownership; those belong to later milestones.

## Architecture

- `InventoryModel`: plain C# model. Slot count, weight capacity, stack merging, partial acceptance, split, move/swap, transfer and auto-sort live here. It is not a MonoBehaviour.
- `PlayerInventory`: MonoBehaviour adapter implementing `IItemReceiver`, exposes `PickTool`, `Count`, slots, total weight and events. A deprecated `SimpleBackpack` shim forwards old references during scene migration and is removed by the Editor upgrade menu.
- `StorageContainer`: same model for a world chest. A collider is required so InventoryHUD can detect nearby chests.
- `InventoryHUD`: runtime UI; no prefab is required. The `Game.UI` assembly references `Game.Gameplay`, while Gameplay does not reference UI.
- `InventorySlotView`: pointer handlers for drag/drop and tap-to-move.
- `GameplayInputGate`: shared modal-input lock in `Game.Core`; InventoryHUD and CraftingHUD acquire/release their own owner token.

## Default rules

- Player: 24 slots, 80 kg capacity.
- Chest: 36 slots, 160 kg capacity.
- Tools: one per slot, durability is kept per instance.
- Materials: stack up to `ItemData.maxStack`.
- `IItemReceiver.Add` returns the amount left over; callers can put leftovers back into a world pickup.
- Transfers preserve tool durability and move only as much as the target can accept.
- Auto-sort groups tools first, then item IDs in ordinal order; compatible material stacks merge.

## Integrate into the Phase 2/3 Scene

1. Open `SCN_IslandWorld` and ensure Player has `PlayerGatherer`, World has `IslandGenerator`, and Canvas has the Phase 1 touch controls / EventSystem. The migration tool creates or repairs `ResourceSpawner`.
2. Ensure the Phase 2 world materials exist (`M_Terrain.mat`) and run **Game → Giai đoạn 3 → Tạo dữ liệu mẫu** so `GatherSettings.asset` is present.
3. Run **Đảo Sương → Giai đoạn 3 → 1 - Tạo Action Button + Gather HUD**, then **2 - Kiểm tra Action + Gather HUD**. This assigns the touch action control used by `PlayerGatherer`.
4. Run **Đảo Sương → Giai đoạn 4 → 1 - Nâng cấp Inventory cho Scene đang mở** and confirm the scene migration dialog.
5. The tool adds/configures `PlayerInventory`, creates or repairs `ResourceSpawner`, assigns `PlayerGatherer` and `GatherDebugPanel`, adds `InventoryHUD` to Canvas, ensures `GraphicRaycaster` and `InputSystemUIInputModule` EventSystem, and creates/repairs `StorageChest_Test`.
6. The test chest subscribes to island generation and moves beside the beach spawn. InventoryHUD shows the `RƯƠNG` button within 2.8 m and pauses movement input while an inventory panel is open.
7. Run **Đảo Sương → Giai đoạn 4 → 2 - Kiểm tra Inventory Scene**.
8. Press Play. The **TÚI** button opens the inventory; the `I` key also works in Editor with the new Input System.

## UI interaction

- Tap an occupied slot once to select it; tap another slot to move, merge or swap.
- Drag an occupied slot and drop it on another slot to move/merge/swap.
- **TÁCH 1/2** splits half a material stack into the first empty slot. Durable tools cannot be split.
- **SẮP XẾP** sorts the player inventory. When a chest is open, use **SẮP XẾP RƯƠNG** for the chest.
- Walk near a `StorageContainer` with a Collider; tap **RƯƠNG** to open the second grid. Cross-container drag/tap transfer respects capacity and weight.
- The weight label shows `current / maximum kg`.
- Opening inventory/storage acquires the shared `GameplayInputGate`; joystick, look, jump and action are blocked until every modal owner releases its lock. The `MobileInput` component stays enabled and clears stale pointer state when the gate first locks.

## Add a real chest

1. Create a chest GameObject or prefab with a visible mesh and a BoxCollider.
2. Add `StorageContainer`. Configure `slotCount`, `maxWeight` and `displayName`.
3. Keep the collider enabled and non-trigger for ordinary physical collision.
4. InventoryHUD detects it automatically within `chestRange` (default 2.8 m).
5. Remove `StorageChest_Test` before creating a release build.

## EditMode tests

`Game.Gameplay.EditModeTests` includes 15 gathering/rules tests, 22 `InventoryModelTests`, 2 `GameplayInputGateTests`, and 10 crafting tests (49 total). Run **Window → General → Test Runner → EditMode → Run All**. Expected result: 49 passed, 0 failed. These tests have been authored but are not claimed as executed without Unity Editor.

## Test checklist

- [ ] Player starts with 24 slots and 80 kg capacity.
- [ ] Add material stacks; they merge up to `maxStack` and use a new slot after that.
- [ ] Fill the weight limit; extra items remain as leftovers and are dropped into world pickups by gathering/crafting callers.
- [ ] Add two tools of the same asset; they occupy separate slots and retain separate durability.
- [ ] Gather a tool, break it and verify auto tool selection skips it.
- [ ] Tap-select then tap another slot to move/merge/swap.
- [ ] Drag/drop works with touch on a phone/tablet.
- [ ] Split half of a material stack; total item count remains unchanged.
- [ ] Auto-sort merges compatible stacks without losing count or weight.
- [ ] Walk near `StorageChest_Test`; the RƯƠNG button appears. Open it and transfer in both directions.
- [ ] A transfer exceeding chest slot/weight capacity leaves the remainder in the source inventory.
- [ ] Close the inventory and verify joystick/look/action controls still receive touch.
- [ ] Test 16:9 and a tall phone aspect ratio; no panel controls should be clipped.

## Known boundaries

- Inventory is in-memory. Profile/cloud/world persistence is a later save-data milestone.
- The chest is local, not network-authoritative. Multiplayer locking/ownership and anti-duplication require server-authoritative inventory commands in Phase 19.
- The runtime HUD uses legacy uGUI Text for the prototype; switch to TextMeshPro and localization in the UI polish phase.
- `PlayerInventory` defaults are serialized on the component. Changing code defaults will not overwrite already serialized Inspector values.

# Phase 3 — Resource Gathering

## Scope

This phase originally introduced harvesting, pickups, fishing, tool durability, timed respawn and a temporary unbounded backpack. The current integrated project has completed the Phase 4 migration: `PlayerInventory` is now slot/weight based, while an obsolete `SimpleBackpack` forwarding shim exists only to migrate older Scenes safely.

## Runtime architecture

```text
Game.Core
  IPlayerInput.cs           Move / Look / Jump / ActionHeld / ConsumeAction
  HudMessages.cs            Prompt, progress, tool durability, toast events

Game.Gameplay
  Items/
    ItemData.cs
    ToolData.cs
    ItemStack.cs
    IItemReceiver.cs        Stable add-item boundary
    PlayerInventory.cs       Slot/weight storage + PickTool (Phase 4)
    SimpleBackpack.cs        Obsolete compatibility shim for old Phase 3 Scenes
  Gathering/
    ResourceNodeData.cs
    FishingTable.cs
    GatherSettings.cs
    GatherRules.cs          Plain C# damage/durability/fishing rules
    GatherTarget.cs
    ResourceNode.cs
    WorldPickup.cs
    ResourceSpawner.cs
  Player/
    PlayerGatherer.cs
    GatherDebugPanel.cs

Game.UI
  MobileInput.cs            Adds E / ActionButton input
  ActionButton.cs           PointerDown edge + held state
  GatherHud.cs              Subscribes to HudMessages

Editor-only (outside runtime asmdefs)
  Scripts/Editor/Stage3DataCreator.cs

EditMode tests
  Tests/EditMode/Game.Gameplay.EditModeTests.asmdef
  Tests/EditMode/GatherRulesTests.cs
```

`Game.Gameplay` references only `Game.Core`. In the integrated Phase 4 project, `Game.UI` references `Game.Core`, `Game.Gameplay`, `UnityEngine.UI`, and `Unity.InputSystem`. No new runtime assembly definition is introduced. The editor tool is under a folder named `Editor`, outside all three runtime assembly-definition roots.

## Important integration detail

Phase 2 tree and rock GameObjects do not own `MeshRenderer` components: their visuals are submitted by `IslandInstanceRenderer` through GPU instancing. Consequently, `ResourceNode` hides a depleted prop by deactivating its GameObject. The instanced renderer skips inactive transforms and Unity disables the prop collider; the spawner can later reactivate it. Disabling child `Renderer` components would not hide this geometry.

## Install the data assets

**Editor/Development debug input:** `GatherDebugPanel` uses IMGUI (`OnGUI`), like the Phase 2 world debug panel. Set `Edit → Project Settings → Player → Active Input Handling` to **Both** while testing debug panels in Editor/Development Build. The on-screen controls still use `InputSystemUIInputModule`. A Release build without the debug panels can use **Input System Package (New)**.

1. Open the project in Unity `6000.3.29f1` and wait for package import/compilation.
2. Confirm `SCN_IslandWorld` is the active scene and the Phase 2 `World`, `Player`, `Canvas`, and `Input` objects exist.
3. Run **Game → Giai đoạn 3 → Tạo dữ liệu mẫu**.
4. Verify the created assets under `Assets/_Game/Data/Gather/`:
   - `GatherSettings.asset`
   - `Node_Tree.asset`, `Node_Rock.asset`, `Node_RockMountain.asset`
   - `FishingTable.asset`
   - sample items and tools (`Wood`, `Stone`, `IronOre`, `Stick`, `Fiber`, `Pebble`, fish items, `StoneAxe`, `StonePickaxe`, `FishingRod`)

The data creator is non-destructive: it creates missing sample assets and leaves existing files unchanged. Delete a sample asset deliberately before rerunning if you want that particular asset recreated with defaults.

## Auto-create the Action button and Gather HUD (recommended)

After creating the Phase 3 sample data, run **Đảo Sương → Giai đoạn 3 → 1 - Tạo Action Button + Gather HUD**. This Editor tool reuses or creates the `ActionButton`, adds `ActionButton` + `HudElement`, assigns it to `MobileInput.actionButton`, creates a full-screen `GatherHud` root and builds the prompt/progress/tool/toast controls with decorative graphics set to `Raycast Target = Off`. Then run **2 - Kiểm tra Action + Gather HUD**. This removes the most error-prone manual wiring step. The detailed manual layout below remains available for custom UI.

## Wire the scene

### Player

1. Select `Player`; add `PlayerInventory`.
2. Add `PlayerGatherer` and assign:
   - **Inventory**: the `PlayerInventory` on Player.
   - **Island**: `World/IslandGenerator`.
   - **Settings**: `GatherSettings.asset`.
   - **Aim Transform**: `Main Camera` (the player can harvest/fish in the direction the camera is aimed, even while standing still).
3. Keep the existing `CharacterController` and `PlayerController` unchanged.

### World

1. Select `World`; add `ResourceSpawner` and assign:
   - **Island**: `IslandGenerator` on World.
   - **Settings**: `GatherSettings.asset`.
   - **Pickup Material**: `Assets/_Game/Art/Materials/M_Terrain.mat`.
2. Add `GatherDebugPanel` and assign:
   - **Inventory**: Player's `PlayerInventory`.
   - **Settings**: `GatherSettings.asset`.
   - **Spawner**: `ResourceSpawner` on World.

The spawner subscribes to `IslandGenerator.Generated`, so it attaches nodes after the deterministic tree/rock lists have been created. Node IDs are assigned in generation order: all trees, then all rocks. Equal seed, generator version, and island settings produce the same node index sequence. This is a local deterministic ID for Phase 3, not yet a trusted network identity.

### Action button and MobileInput

1. Under Canvas, create an Image named `ActionButton`.
2. Set anchors Min/Max to `(1, 0)`, pivot `(0.5, 0.5)`, position `(-470, 250)`, size `(220, 220)`.
3. Add `Game.UI.ActionButton` and `Game.UI.HudElement`. In HudElement, set ID to `action` and keep **Customizable** enabled. `CanvasGroup` is added automatically by HudElement.
4. Keep the ActionButton Image's **Raycast Target enabled**. Its child Text must have Raycast Target disabled.
5. Move ActionButton to a sibling order above `LookZone` (after LookZone in the Canvas hierarchy; keep it above the LookZone so it receives touch input).
6. Select `Input`; on `MobileInput`, assign the new ActionButton field. Existing joystick/look/jump references remain unchanged.

Controls: hold the button (or hold **E** in Editor) to repeatedly hit a tree/rock using the configured swing interval. A tap on a pickup collects it. At sea, tap to cast; do not press again while waiting; tap during `CÁ CẮN CÂU!` to catch. Moving cancels the cast during both the waiting and bite phases. Pressing too early cancels it.

### Gather HUD

Create a child object named `GatherHud` under Canvas and add `Game.UI.GatherHud`. Create and wire these children/references:

| Object | Type | Suggested layout | Inspector field |
|---|---|---|---|
| `PromptText` | Legacy UI Text + Outline | anchor `(0.5,0)`, pos `(0,150)`, size `900×60`, centered, font 36 | Prompt Text |
| `ProgressBar` | Image background | anchor `(0.5,0)`, pos `(0,110)`, size `400×20` | Progress Root |
| `ProgressBar/Fill` | Image | anchors min `(0,0)`, max `(1,1)`, pivot `(0,0.5)` | Progress Fill |
| `ToolInfo` | Image background | anchor `(0.5,1)`, pos `(0,-70)`, size `300×60` | Tool Root |
| `ToolInfo/Name` | Legacy UI Text | centered or left aligned | Tool Text |
| `ToolInfo/Bar/Fill` | Image | fill anchors `(0,0)` to `(1,1)`, pivot `(0,0.5)` | Tool Fill + Tool Fill Image |
| `ToastText` | Legacy UI Text | anchor `(1,0.5)`, pos `(-250,100)`, size `400×200`, right aligned, font 30 | Toast Text |

For every decorative Image and all HUD Text objects, turn **Raycast Target off**. The ActionButton's background Image is the exception and must keep raycasts enabled. The Progress Fill and Tool Fill should use left-to-right anchors as described so the HUD can change `anchorMax.x`.

Use a font with Vietnamese glyphs. LegacyRuntime works as the initial fallback; if it lacks glyphs on the chosen Unity install, assign a licensed TTF that contains Vietnamese characters. TextMeshPro migration is reserved for the UI/polish phase.

## EditMode tests

The original Phase 3 suite has 12 gathering tests. The integrated suite now has 15 gathering/rules tests, 22 inventory-model tests, 2 shared gameplay-input-gate tests and 10 crafting tests (49 total). The test assembly references `Game.Core` and `Game.Gameplay` and is Editor-only. `com.unity.test-framework` is set to `1.6.0` for this Unity 6.3 project.

Run **Window → General → Test Runner → EditMode → Run All**. Expected after execution: 49 passed, 0 failed. The original gathering coverage includes tool compatibility/tier, damage floor, hits-to-destroy rounding, durability floor, drop amounts, fishing catch/escape/cancel, weighted fish selection, and choosing the best unbroken tool. Additional audit tests cover non-finite damage/timer/drop-weight inputs.

## Test checklist

- [ ] Play starts on the Phase 2 island; no compile errors in Console.
- [ ] Debug panel shows **Nhận công cụ test** and adds an axe, pickaxe, and fishing rod.
- [ ] Near a tree without an axe: prompt says the required axe/tier.
- [ ] Hold Action / E near a tree: it shakes, takes hits, deactivates on depletion, and awards loot.
- [ ] Hold Action / E near a mountain rock: it depletes and may award iron ore.
- [ ] Pickup prompt appears; tap Action to collect it.
- [ ] Set durability to 3 in debug; three tool uses break it and remove it from tool selection.
- [ ] At the shoreline, face the sea and tap Action to cast. Catch during the bite window; wait too long and the fish escapes.
- [ ] Move while the line is waiting; the cast cancels.
- [ ] Press **Hồi tài nguyên ngay**; depleted tree/rock/pickup targets return.
- [ ] Change node `respawnSeconds` to 15 and verify automatic respawn after 15 seconds.
- [ ] Test on a real phone: hold joystick with one finger and Action with another. A desktop mouse cannot fully validate multitouch.

## Known boundaries / not yet implemented

- Phase 3 was first delivered with unbounded storage. Phase 4 now replaces it with 24 slots / 80 kg, stacking, weight checks, chest transfer and touch UI. Inventory save/cloud persistence remains later work.
- Harvest damage, drops, fishing rolls, and respawn are currently local host/client simulation. Phase 19 moves validation and mutation to the authoritative server; do not treat client-side harvest as anti-cheat secure.
- Resource IDs are stable for same seed/settings/generator version and deterministic enumeration. They are not stable across an algorithm or settings change; include a world-generation version in the network protocol.
- Tree and rock collision objects remain separate. Phase 2's GPU instancing still handles their visuals; props that are inactive are skipped until respawn.
- Fishing is intentionally a short timing interaction (3–8 seconds wait, 1.3 seconds bite window by default) for mobile playtests. Tune in `PlayerGatherer` after playtesting.
- Fishing is a simplified cast/timing/drop system. Rod animation, line rendering, water splash/VFX and audio are polish work, not part of this phase.

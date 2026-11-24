# Đảo Sương — Source Audit & Remediation Report

**Audit baseline:** Phase 5 project, Unity `6000.3.29f1`
**Scope:** runtime C# source, editor tooling, inventory transfer invariants, gathering data validation, modal UI input, assembly graph, package manifest, asset metadata, EditMode test declarations.

## Summary

The audit fixed issues that could cause duplicated items, stale gameplay input after closing a modal, invalid numeric data entering gameplay rules, and an incomplete Phase 3 HUD setup path. It also hardened HUD component disable/re-enable cleanup. The audit checked a suspected shader-instancing issue and found the required instancing directives already present, so that shader was not changed.

## Issues corrected

### 1. Oversized inventory transfer could duplicate items

`InventoryModel.TryTransferToAt` previously passed the caller's requested amount directly to the destination. If a caller requested more than the source stack owned, the destination could receive more than the source had before the source slot was removed. The public `AddStack` entry point also trusted the requested amount independently of the source stack.

**Fix:** both paths now clamp to `source.count`; tool stacks are still limited to one unit. `InventoryModel.Add` creates a request stack with the full amount so the clamp does not truncate legitimate additions before max-stack splitting.

**Regression tests:** `TransferToSpecificSlotNeverMovesMoreThanSourceStackOwns`, `AddStackNeverInsertsMoreThanSourceStackOwns`, and partial-target-capacity coverage.

### 2. Inventory and crafting panels could unlock gameplay while another modal remained open

Disabling `MobileInput` per-panel is not composable: the first panel closed could re-enable controls while another panel was still active. A pointer could also begin touching a joystick/action control outside a modal panel and leave stale held input after closing the modal.

**Fix:** `Game.Core.GameplayInputGate` tracks independent modal owners. Inventory and crafting acquire/release their own lock. `MobileInput` returns neutral input while locked and clears stale input on lock transition. Joystick, look-zone, jump and action pointer handlers reject new gameplay touches while a modal is open. Panels close cleanly on disable/re-enable conflicts.

**Regression tests:** two EditMode tests cover multiple owners, idempotent acquisition and safe release.

**Lifecycle hardening:** disabling `InventoryHUD` or `CraftingHUD` now hides its panel, clears chest/slot selection where applicable, unsubscribes events and releases its own lock. Disabling `MobileInput` clears held pointer state before it relinquishes the input service.

### 3. Non-finite asset values could poison gameplay calculations

NaN/Infinity values in item weight, tool power/swing interval, resource health/respawn/drop chance, fishing weights and timer arguments could create invalid UI values, broken hit calculations, invalid random selection or non-terminating-looking timers.

**Fix:** ScriptableObject `OnValidate` methods sanitize relevant asset fields; pure gather/fishing rules independently defend runtime entry points; inventory weight calculation rejects invalid item weights; wear calculation avoids integer overflow.

**Regression tests:** non-finite damage, fishing timer and fishing-table weight cases.

### 4. Phase 3 gathering HUD was not created by the normal Scene pipeline

Phase 2 creates the base Canvas and `MobileInput`, while Phase 4 creates inventory UI; neither step created the Phase 3 `ActionButton` and `GatherHud`. Following the existing setup steps without the manual wiring could therefore leave a playable scene without touch gathering controls.

**Fix:** added Editor menu `Đảo Sương → Giai đoạn 3 → 1 - Tạo Action Button + Gather HUD` and validator `2 - Kiểm tra Action + Gather HUD`. The tool creates/reuses the action control and HUD elements, assigns `MobileInput.actionButton`, disables Raycast Target on decorative UI, and saves the active Scene. Phase 4 validation now reports if these controls are missing.

## Suspected issue checked and not changed

`LowPolyVertexColor.shader` already contains `#pragma multi_compile_instancing`, `UNITY_VERTEX_INPUT_INSTANCE_ID` and `UNITY_SETUP_INSTANCE_ID`. No shader patch was needed. `IslandInstanceRenderer` caps submissions at 511 instances per draw, matching the documented default `RenderMeshInstanced` maximum when the default object-to-world matrix is included. Runtime GPU support and actual draw-call count still require device/Frame Debugger tests.

## Static verification performed in the packaging environment

The following checks passed after remediation:

- 5 JSON files parsed: package manifest + 4 assembly definitions.
- Assembly references resolve to known project assemblies or expected Unity package assemblies; no project assembly cycle detected.
- 77 C# files passed lexical delimiter and preprocessor-balance scanning.
- 126 non-meta asset files checked; 182 `.meta` files (including folder metadata) had 182 unique GUIDs; no missing `.meta` found.
- EditMode tests declared: 49 (`GatherRulesTests` 15, `InventoryModelTests` 22, `GameplayInputGateTests` 2, `CraftingRulesTests` 10).
- Test assembly includes `TestAssemblies` and references `Game.Core` + `Game.Gameplay`.
- Low-poly terrain shader instancing macros checked present.
- `git diff --check` passed.
- ZIP archive integrity checked after packaging.

These are source/structure checks, not a C# compile.

## Required Unity Editor acceptance steps

This repository package does not contain a generated `.unity` scene yet. The Phase 2 Editor tool creates `SCN_IslandWorld` and adds it to Build Settings. Also, `Packages/packages-lock.json` is generated when Unity resolves the package graph.

1. Open the project root in Unity `6000.3.29f1` and wait for Package Manager import/resolve. Resolve package errors before continuing.
2. Create and assign the URP Pipeline Asset using `docs/03_PROJECT_SETUP.md`.
3. For Editor/Development testing, set Active Input Handling to **Both** because the world/gather debug panels use IMGUI `OnGUI`; uGUI touch controls use `InputSystemUIInputModule`.
4. Run `Đảo Sương → Phase 2 → 1 - Create World Assets`, then `2 - Create Playable Island Scene`.
5. Run `Game → Giai đoạn 3 → Tạo dữ liệu mẫu`.
6. Run `Đảo Sương → Giai đoạn 3 → 1 - Tạo Action Button + Gather HUD`, then `2 - Kiểm tra Action + Gather HUD`.
7. Run `Đảo Sương → Giai đoạn 4 → 1 - Nâng cấp Inventory cho Scene đang mở`, then `2 - Kiểm tra Inventory Scene`.
8. Run `Đảo Sương → Giai đoạn 5 → 1 - Tạo dữ liệu công thức mẫu`, then `2 - Nâng cấp Scene chế tạo`, then `3 - Kiểm tra Scene chế tạo`.
9. Open **Window → General → Test Runner → EditMode** and run all tests. Expected: **49 passed, 0 failed**.
10. Press Play and check Console. Verify movement, gathering, respawn, inventory, chest transfer, crafting and modal input lock.
11. Build to a real Android device and test two-finger controls, Action while moving, inventory drag/drop, chest transfers and device performance. Desktop mouse emulation cannot prove multitouch.
12. After package resolution, commit the generated `Packages/packages-lock.json` and any required URP asset/configuration changes.

## Not verified in this environment

No Unity Editor or C# compiler is installed in the packaging runtime. The following have **not** been run and must not be treated as passed:

- Unity C# compilation and shader compilation.
- 49/49 EditMode tests.
- Play Mode / scene setup / runtime UI behavior.
- Android or iOS builds and touch testing.
- Actual FPS, batches, RAM, thermal and battery measurements.

The first red Console error or Test Runner failure should be fixed before marking this project accepted. Keep this report with the project so source-level checks are not confused with runtime acceptance.

# Phase 6 code audit report — Đảo Sương

## Scope reviewed

- Phase 6 survival model, tuning, consumable data, controller, HUD and Editor wiring.
- Existing Phase 3 gatherer/input boundary, Phase 4 inventory use path and existing Modal Input Gate.
- Test assembly and package/asmdef references.

## Findings corrected

1. **Consumable data authoring errors:** Coconut Water had hunger and thirst restore arguments swapped; Dried Fruit restored health instead of hunger. Both sample definitions corrected.
2. **Dead-player interaction:** Player movement was blocked, but gathering/fishing could still be invoked. PlayerGatherer now cancels fishing and stops gathering when dead.
3. **Stale input after death:** Added `ClearPendingInput()` to `IPlayerInput`, clears input on death and debug restore, and resets sprint when controllers are disabled.
4. **Invalid consumable slot:** `TryConsume` now validates the slot index before reading the inventory, returning `false` for out-of-range indices.
5. **Temperature tuning robustness:** thresholds are clamped to safe ranges; non-finite environment tuning values are replaced with defaults.
6. **HUD overlap:** increased survival panel height and moved the condition summary below all five bars.
7. **Inventory HUD bounds:** moved the `DÙNG` button inside the panel rectangle.
8. **Fishing table migration safety:** updates fish assets by stable item ID, preserving existing odds/counts and unrelated drop rows rather than replacing the entire table.
9. **Consumable model validation:** rejects negative restore values and out-of-domain multipliers/temperature shifts even if a caller bypasses ScriptableObject validation.
10. **Timed buff stacking:** a stronger buff replaces a weaker one; equal-strength buffs refresh remaining duration; weaker buffs do not extend stronger active effects.
11. **Sprint tuning:** a zero stamina-cost configuration now allows sprinting; positive costs account for the actual frame delta instead of undercharging on slow frames.
12. **Consumable validation inheritance:** `ConsumableData.OnValidate` now properly overrides and calls `ItemData.OnValidate`, so base item identity, stack and weight validation is retained.

## Static validation

- 85 C# source files scanned for balanced braces and `#if/#endif` blocks: passed.
- 5 JSON/asmdef files parsed: passed.
- 4 assembly definitions checked; no missing internal Game.* references or assembly dependency cycles.
- 192 `.meta` files / GUIDs under Assets: all unique; 0 missing `.meta` for source/asset types checked.
- `git diff --check`: passed.
- 79 EditMode test methods found in source.
- Project ZIP integrity verified at packaging time (see final handoff).

## Not verified

The environment has no `Unity`, `dotnet`, `csc` or `mcs` executable. Therefore this report does **not** claim:

- C# compilation in Unity;
- Shader compilation;
- Unity Test Runner result;
- Play Mode success;
- Android/iOS build success;
- Physical-device multi-touch/FPS/thermal performance.

Those items require Unity Editor and target devices. Run the exact sequence in `docs/15_PHASE6_SETUP_AND_AUDIT.md` before marking Phase 6 accepted.

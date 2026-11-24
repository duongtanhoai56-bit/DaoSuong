# Đảo Sương — Phase 6 setup & audit checklist

## Required editor

- Unity Editor: `6000.3.29f1`
- URP: `17.3.0`
- Input System: `1.20.1`
- uGUI: `2.0.0`
- Unity Test Framework: `1.6.0`

## Upgrade order

Use an existing Phase 2–5 scene and run the tools in this order. Do not create duplicate scenes or delete previously generated data.

1. Open the Unity project and wait for Package Manager/import to finish.
2. Create the Phase 2 world materials/asset and scene using `Đảo Sương > Phase 2 > 1 - Create World Assets` and `2 - Create Playable Island Scene`. Verify `Player`, `IslandGenerator`, `DayNightCycle`, `WeatherSystem`, `Canvas`, `EventSystem`, `MobileInput` and `PlayerController` exist.
3. Run `Game > Giai đoạn 3 > Tạo dữ liệu mẫu` to create `GatherSettings`, node data, tools and the original fishing table.
4. Run `Đảo Sương > Giai đoạn 3 > 1 - Tạo Action Button + Gather HUD` and then `2 - Kiểm tra Action + Gather HUD`.
5. Run `Đảo Sương > Giai đoạn 4 > 1 - Nâng cấp Inventory cho Scene đang mở` and `2 - Kiểm tra Inventory Scene`. This step creates `PlayerInventory`, `PlayerGatherer`, `ResourceSpawner`, and wires the inventory/gather references; it also creates the test chest.
6. Confirm the Player has `PlayerGatherer` and the World has `ResourceSpawner`; both must reference the Island and GatherSettings. See `docs/10_PHASE3_RESOURCE_GATHERING.md` for the component details.
7. Run `Đảo Sương > Giai đoạn 5 > 1 - Tạo dữ liệu công thức mẫu`, then `2 - Nâng cấp Scene chế tạo` and `3 - Kiểm tra Scene chế tạo`.
8. Run `Đảo Sương > Giai đoạn 6 > 1 - Tạo dữ liệu sinh tồn`. This creates a SurvivalSettings asset and eight consumable assets. It updates existing fish rows by item ID while preserving custom weights/counts and unrelated drops.
9. Run `Đảo Sương > Giai đoạn 6 > 2 - Nâng cấp Scene sinh tồn`, confirm the dialog, then run `3 - Kiểm tra Scene sinh tồn`.
10. Save the scene and commit the generated `.asset` files and scene changes if you intend to keep them in source control.

## Required wiring after Phase 6 upgrade

On Player:

- `PlayerInventory`
- `PlayerController` with `SurvivalController` assigned
- `PlayerGatherer` with `SurvivalController` assigned (the script also resolves it from the same GameObject during Awake)
- `SurvivalController` with `SurvivalSettings`, `IslandGenerator`, `DayNightCycle`, `WeatherSystem` and `PlayerInventory`

On Canvas:

- `MobileInput` must reference the `SprintButton`
- `InventoryHUD` must reference the same `SurvivalController`
- `SurvivalHUD` must reference the `SurvivalController`
- Canvas must have `GraphicRaycaster` and an EventSystem must use `InputSystemUIInputModule`

The Phase 6 Editor tool creates the sprint button and SurvivalHUD and wires the main references. It does not silently create missing prior-phase systems.

## Runtime test checklist

1. Start at full health/hunger/thirst/stamina and 37°C.
2. Stand idle for 60–120 seconds; hunger and thirst decrease slowly without sudden health loss.
3. Hold sprint while moving; speed increases, stamina decreases, and hunger/thirst drain faster.
4. Release sprint; stamina regenerates. At low stamina the HUD reports `Kiệt sức`.
5. Open Inventory, select Dried Fruit, press `DÙNG`: hunger should rise and one item should be consumed.
6. Select Coconut Water, press `DÙNG`: thirst should rise and one item should be consumed. Hunger should not rise from this item.
7. Use Herbal Bandage only while bleeding; use Herbal Tea while sick or when its warmth/restoration provides a benefit.
8. Apply debug damage/bleeding/sickness. Confirm bars/status update and effects expire.
9. Allow hunger or thirst to reach zero in a controlled Development test; health then decreases gradually.
10. Test a storm at night in a mountain biome. Body temperature should trend down gradually; ordinary tropical daylight should not instantly cause heat damage.
11. Force death with the Development panel. Movement, gathering and fishing should stop. A previous joystick/jump/action press must not replay after `RestoreForDebug`.
12. Open Inventory while Crafting is closed and vice versa; modal input locks must still behave correctly.
13. Test two-finger mobile input after closing all modal panels.

## Automated tests

Open `Window > General > Test Runner > EditMode` and run all tests.

The project currently contains 79 `[Test]` methods:

- Crafting: 10
- GameplayInputGate: 2
- Gathering: 15
- Inventory: 24
- Survival model: 22
- Survival controller: 6

Expected result: **79 Passed, 0 Failed**. This is the target, not a claimed result. The source-generation environment does not include Unity Editor or a C# compiler, so this test suite has not been executed in this environment.

## Review fixes included in this update

- Invalid inventory slot index on consumable use now returns `false`, not an index exception.
- Player gather/fishing interactions stop while the survival controller reports dead.
- Death clears pending touch/keyboard input; debug restore clears it again so a held joystick/action cannot replay after revival.
- Sprint state is reset when PlayerController/SurvivalController is disabled.
- Survival tuning thresholds are bounded and non-finite environment values fall back safely.
- Runtime HUD height/status placement no longer overlaps the temperature bar.
- Coconut Water restores thirst (not hunger); Dried Fruit restores hunger (not health).
- Fishing table migration updates matching fish item IDs and preserves custom weights, amounts and unrelated entries.
- Inventory use button remains inside the inventory panel bounds.
- Pure consumable validation rejects negative restores and out-of-range multipliers/temperature shifts even if a caller bypasses Inspector validation.
- Stronger timed buffs replace weaker ones; equal-strength buffs refresh duration; weaker buffs do not extend stronger active effects.
- A zero sprint-stamina drain setting allows sprinting without consuming stamina; positive drain uses the actual frame delta up to a two-second safety cap.

## Known limitations

- No Unity Editor compile, Play Mode, Android build or physical multi-touch test has been performed in the file-generation environment.
- Survival is a local simulation. Health/consumable changes are not server-authoritative until the networking phase.
- Death event/input gating are in place, but loot drop, teammate revive and base respawn are not implemented yet.
- Shelter/clothing exposure modifiers and offline/background survival simulation are not implemented.
- IMGUI debug buttons require `Active Input Handling = Both` in Editor/Development tests. Release builds should use the intended Input System configuration.

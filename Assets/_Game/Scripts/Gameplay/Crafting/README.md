# Crafting module (Phase 5)

- `RecipeData` and `CraftingCatalog` are ScriptableObject content definitions.
- `CraftingRules` is pure domain logic and works against `InventoryModel`.
- `CraftingStation` is a Unity scene adapter exposing a tier (0 hand, 1 workbench, 2 forge).
- `CraftingHUD` is runtime uGUI, uses `PlayerInventory`, and blocks mobile movement while open.

Crafting is synchronous in Phase 5. Timed queues, cooking buffs, fuel, and smelting loops are deliberately deferred to later stages.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Gameplay;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class Phase5CraftingTools
{
    private const string DataFolder = "Assets/_Game/Data/Crafting";
    private const string CatalogPath = DataFolder + "/CraftingCatalog.asset";

    [MenuItem("Đảo Sương/Giai đoạn 5/1 - Tạo dữ liệu công thức mẫu")]
    public static void CreateSampleData()
    {
        GatherSettings gather = AssetDatabase.LoadAssetAtPath<GatherSettings>("Assets/_Game/Data/Gather/GatherSettings.asset");
        if (gather == null)
        {
            EditorUtility.DisplayDialog("Đảo Sương", "Chưa có GatherSettings. Hãy chạy Game → Giai đoạn 3 → Tạo dữ liệu mẫu trước.", "OK");
            return;
        }

        ItemData wood = FindDrop(gather.tree, "wood");
        ItemData stick = FindDrop(gather.tree, "stick");
        ItemData fiber = FindDrop(gather.tree, "fiber");
        ItemData stone = FindDrop(gather.rockDefault, "stone");
        ItemData ironOre = FindDrop(gather.rockMountain, "iron_ore");
        ToolData axe = FindTool(gather, ToolType.Axe);
        ToolData pickaxe = FindTool(gather, ToolType.Pickaxe);
        ToolData rod = FindTool(gather, ToolType.FishingRod);
        if (wood == null || stick == null || fiber == null || stone == null || ironOre == null || axe == null || pickaxe == null || rod == null)
        {
            EditorUtility.DisplayDialog("Đảo Sương", "Dữ liệu tài nguyên Giai đoạn 3 thiếu vật phẩm/công cụ cần thiết. Chạy lại menu tạo dữ liệu Giai đoạn 3.", "OK");
            return;
        }

        EnsureFolder(DataFolder);
        ItemData rope = Item("Rope", "rope", "Dây thừng", 20, 0.2f, new Color(0.55f, 0.42f, 0.25f));
        ItemData plank = Item("WoodPlank", "wood_plank", "Ván gỗ", 30, 0.45f, new Color(0.62f, 0.43f, 0.27f));
        ItemData ironIngot = Item("IronIngot", "iron_ingot", "Thỏi sắt", 20, 1.2f, new Color(0.56f, 0.59f, 0.63f));
        ToolData ironAxe = Tool("IronAxe", "iron_axe", "Rìu sắt", ToolType.Axe, 2, 2.8f, 120, 2.0f);
        ToolData ironPickaxe = Tool("IronPickaxe", "iron_pickaxe", "Cuốc sắt", ToolType.Pickaxe, 2, 3.0f, 120, 2.2f);

        List<RecipeData> recipes = new List<RecipeData>();
        recipes.Add(Recipe("Recipe_Rope", "Dây thừng", new[] { Req(fiber, 3) }, rope, 1, 0));
        recipes.Add(Recipe("Recipe_WoodPlank", "Xẻ ván gỗ", new[] { Req(wood, 2) }, plank, 1, 1));
        recipes.Add(Recipe("Recipe_StoneAxe", "Rìu đá", new[] { Req(wood, 3), Req(stone, 2), Req(stick, 2) }, axe, 1, 1));
        recipes.Add(Recipe("Recipe_StonePickaxe", "Cuốc đá", new[] { Req(wood, 3), Req(stone, 3), Req(stick, 2) }, pickaxe, 1, 1));
        recipes.Add(Recipe("Recipe_FishingRod", "Cần câu", new[] { Req(stick, 3), Req(fiber, 2) }, rod, 1, 1));
        recipes.Add(Recipe("Recipe_IronIngot", "Luyện thỏi sắt", new[] { Req(ironOre, 2), Req(wood, 1) }, ironIngot, 1, 2));
        recipes.Add(Recipe("Recipe_IronAxe", "Rìu sắt", new[] { Req(plank, 2), Req(ironIngot, 3), Req(rope, 1) }, ironAxe, 1, 2));
        recipes.Add(Recipe("Recipe_IronPickaxe", "Cuốc sắt", new[] { Req(plank, 2), Req(ironIngot, 3), Req(rope, 1) }, ironPickaxe, 1, 2));

        CraftingCatalog catalog = AssetDatabase.LoadAssetAtPath<CraftingCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CraftingCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        catalog.recipes = recipes.ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Selection.activeObject = catalog;
        EditorUtility.DisplayDialog("Đảo Sương", $"Đã tạo {recipes.Count} công thức, 3 vật phẩm mới và 2 công cụ sắt tại {DataFolder}.", "OK");
    }

    [MenuItem("Đảo Sương/Giai đoạn 5/2 - Nâng cấp Scene chế tạo", true)]
    private static bool ValidateUpgrade() => !EditorApplication.isPlaying && EditorSceneManager.GetActiveScene().IsValid();

    [MenuItem("Đảo Sương/Giai đoạn 5/2 - Nâng cấp Scene chế tạo")]
    public static void UpgradeScene()
    {
        CraftingCatalog catalog = AssetDatabase.LoadAssetAtPath<CraftingCatalog>(CatalogPath);
        if (catalog == null || catalog.recipes == null || catalog.recipes.Length == 0)
        {
            EditorUtility.DisplayDialog("Đảo Sương", "Hãy chạy bước 1 để tạo CraftingCatalog trước.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Nâng cấp Scene", "Sẽ thêm CraftingHUD và hai trạm thử (Bàn chế tạo, Lò rèn) vào Scene đang mở. Scene sẽ được lưu. Tiếp tục?", "Nâng cấp", "Hủy")) return;

        GameObject player = GameObject.Find("Player");
        PlayerInventory inventory = player != null ? player.GetComponent<PlayerInventory>() : null;
        IslandGenerator island = UnityEngine.Object.FindFirstObjectByType<IslandGenerator>();
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (player == null || inventory == null || island == null || canvas == null)
        {
            EditorUtility.DisplayDialog("Đảo Sương", "Thiếu Player/PlayerInventory, IslandGenerator hoặc Canvas. Hãy hoàn tất Giai đoạn 4 trước.", "OK");
            return;
        }

        CraftingHUD hud = canvas.GetComponent<CraftingHUD>();
        if (hud == null) hud = Undo.AddComponent<CraftingHUD>(canvas.gameObject);
        Assign(hud, "catalog", catalog); Assign(hud, "inventory", inventory); Assign(hud, "playerRoot", player.transform);

        CreateStation("CraftingStation_Workbench_Test", "Bàn chế tạo", 1, island, player.transform, 3f, -1.2f, new Vector3(1.7f, 1.1f, 1.2f));
        CreateStation("CraftingStation_Forge_Test", "Lò rèn", 2, island, player.transform, 3f, 1.2f, new Vector3(1.8f, 1.25f, 1.3f));

        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        EditorUtility.DisplayDialog("Đảo Sương", "Đã thêm CraftingHUD và hai trạm chế tạo mẫu. Bấm CHẾ TẠO hoặc phím C để mở danh sách.", "OK");
    }

    [MenuItem("Đảo Sương/Giai đoạn 5/3 - Kiểm tra Scene chế tạo")]
    public static void ValidateScene()
    {
        List<string> issues = new List<string>();
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        CraftingCatalog catalog = AssetDatabase.LoadAssetAtPath<CraftingCatalog>(CatalogPath);
        if (catalog == null || catalog.recipes == null || catalog.recipes.Length == 0) issues.Add("Thiếu CraftingCatalog hoặc recipe.");
        if (canvas == null) issues.Add("Thiếu Canvas.");
        else
        {
            CraftingHUD hud = canvas.GetComponent<CraftingHUD>();
            if (hud == null) issues.Add("Canvas thiếu CraftingHUD.");
            else
            {
                if (GetReference(hud, "catalog") == null) issues.Add("CraftingHUD chưa gán Catalog.");
                if (GetReference(hud, "inventory") == null) issues.Add("CraftingHUD chưa gán PlayerInventory.");
            }
            if (canvas.GetComponent<GraphicRaycaster>() == null) issues.Add("Canvas thiếu GraphicRaycaster.");
        }
        GameObject player = GameObject.Find("Player");
        if (player == null || player.GetComponent<PlayerInventory>() == null) issues.Add("Thiếu PlayerInventory.");
        if (UnityEngine.Object.FindFirstObjectByType<CraftingStation>() == null) issues.Add("Chưa có trạm chế tạo.");
        EditorUtility.DisplayDialog("Đảo Sương - Crafting", issues.Count == 0 ? "Không phát hiện thiếu cấu hình cơ bản." : string.Join("\n", issues), "OK");
    }

    private static ItemData FindDrop(ResourceNodeData node, string id)
    {
        if (node == null || node.drops == null) return null;
        foreach (DropEntry drop in node.drops) if (drop != null && drop.item != null && drop.item.id == id) return drop.item;
        return null;
    }
    private static ToolData FindTool(GatherSettings settings, ToolType type)
    {
        if (settings.debugTools == null) return null;
        foreach (ToolData tool in settings.debugTools) if (tool != null && tool.toolType == type) return tool;
        return null;
    }
    private static RecipeIngredient Req(ItemData item, int amount) => new RecipeIngredient { item = item, amount = amount };

    private static RecipeData Recipe(string file, string label, RecipeIngredient[] ingredients, ItemData output, int amount, int stationTier)
    {
        string path = DataFolder + "/" + file + ".asset";
        RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (recipe == null) { recipe = ScriptableObject.CreateInstance<RecipeData>(); recipe.name = file; AssetDatabase.CreateAsset(recipe, path); }
        recipe.id = file.ToLowerInvariant(); recipe.displayName = label; recipe.ingredients = ingredients; recipe.output = output; recipe.outputAmount = amount; recipe.requiredStationTier = stationTier;
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    private static ItemData Item(string file, string id, string label, int stack, float weight, Color color)
    {
        string path = DataFolder + "/" + file + ".asset";
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<ItemData>(); item.name = file; AssetDatabase.CreateAsset(item, path); }
        item.id = id; item.displayName = label; item.maxStack = stack; item.weight = weight; item.color = color; EditorUtility.SetDirty(item); return item;
    }

    private static ToolData Tool(string file, string id, string label, ToolType type, int tier, float power, int durability, float weight)
    {
        string path = DataFolder + "/" + file + ".asset";
        ToolData item = AssetDatabase.LoadAssetAtPath<ToolData>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<ToolData>(); item.name = file; AssetDatabase.CreateAsset(item, path); }
        item.id = id; item.displayName = label; item.maxStack = 1; item.weight = weight; item.toolType = type; item.tier = tier; item.power = power; item.maxDurability = durability; EditorUtility.SetDirty(item); return item;
    }

    private static void CreateStation(string objectName, string label, int tier, IslandGenerator island, Transform player, float forward, float side, Vector3 scale)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null) { go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = objectName; Undo.RegisterCreatedObjectUndo(go, "Create " + label); }
        go.transform.localScale = scale;
        CraftingStation station = go.GetComponent<CraftingStation>();
        if (station == null) station = Undo.AddComponent<CraftingStation>(go);
        AssignInt(station, "tier", tier); AssignString(station, "displayName", label); AssignBool(station, "placeNearPlayerSpawn", true);
        Assign(station, "island", island); Assign(station, "player", player); AssignFloat(station, "forwardOffset", forward); AssignFloat(station, "sideOffset", side);
        EditorUtility.SetDirty(station);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/'); string current = parts[0];
        for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
    }

    private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedProperties(); } }
    private static UnityEngine.Object GetReference(UnityEngine.Object target, string field) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); return p != null ? p.objectReferenceValue : null; }
    private static void AssignString(UnityEngine.Object target, string field, string value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p != null) { p.stringValue = value; so.ApplyModifiedProperties(); } }
    private static void AssignBool(UnityEngine.Object target, string field, bool value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p != null) { p.boolValue = value; so.ApplyModifiedProperties(); } }
    private static void AssignInt(UnityEngine.Object target, string field, int value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p != null) { p.intValue = value; so.ApplyModifiedProperties(); } }
    private static void AssignFloat(UnityEngine.Object target, string field, float value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p != null) { p.floatValue = value; so.ApplyModifiedProperties(); } }
}
#endif

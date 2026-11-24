using System;
using Game.Gameplay;
using UnityEditor;
using UnityEngine;

/// <summary>Creates missing Phase 3 sample assets and preserves existing user edits.</summary>
public static class Stage3DataCreator
{
    private const string DataDirectory = "Assets/_Game/Data/Gather";

    [MenuItem("Game/Giai đoạn 3/Tạo dữ liệu mẫu")]
    public static void CreateSampleData()
    {
        EnsureFolder(DataDirectory);

        ItemData wood = CreateItem("Wood", "wood", "Gỗ", new Color(0.55f, 0.38f, 0.22f), 0.5f);
        ItemData stone = CreateItem("Stone", "stone", "Đá", new Color(0.55f, 0.55f, 0.58f), 0.6f);
        ItemData ironOre = CreateItem("IronOre", "iron_ore", "Quặng sắt", new Color(0.62f, 0.42f, 0.35f), 0.8f);
        ItemData stick = CreateItem("Stick", "stick", "Cành cây", new Color(0.45f, 0.32f, 0.2f), 0.2f);
        ItemData fiber = CreateItem("Fiber", "fiber", "Sợi cỏ", new Color(0.55f, 0.75f, 0.3f), 0.05f);
        ItemData pebble = CreateItem("Pebble", "pebble", "Đá nhỏ", new Color(0.7f, 0.7f, 0.72f), 0.15f);
        ItemData smallFish = CreateItem("FishSmall", "fish_small", "Cá nhỏ", new Color(0.55f, 0.75f, 0.9f), 0.3f);
        ItemData largeFish = CreateItem("FishBig", "fish_big", "Cá lớn", new Color(0.3f, 0.55f, 0.85f), 0.8f);
        ItemData eel = CreateItem("Eel", "eel", "Lươn", new Color(0.3f, 0.35f, 0.3f), 0.5f);

        ToolData stoneAxe = CreateTool("StoneAxe", "stone_axe", "Rìu đá", ToolType.Axe, 1, 1.5f, 60);
        ToolData stonePickaxe = CreateTool("StonePickaxe", "stone_pickaxe", "Cuốc đá", ToolType.Pickaxe, 1, 1.5f, 60);
        ToolData fishingRod = CreateTool("FishingRod", "fishing_rod", "Cần câu", ToolType.FishingRod, 1, 1f, 40);

        ResourceNodeData treeNode = Upsert<ResourceNodeData>("Node_Tree", (node) =>
        {
            node.displayName = "Cây";
            node.verb = "chặt";
            node.requiredTool = ToolType.Axe;
            node.minTier = 1;
            node.health = 6f;
            node.durabilityCostPerHit = 1;
            node.respawnSeconds = 420f;
            node.drops = new[]
            {
                new DropEntry { item = wood, min = 3, max = 5, chance = 1f },
                new DropEntry { item = stick, min = 1, max = 2, chance = 0.6f },
                new DropEntry { item = fiber, min = 1, max = 2, chance = 0.5f }
            };
        });

        ResourceNodeData rockNode = Upsert<ResourceNodeData>("Node_Rock", (node) =>
        {
            node.displayName = "Tảng đá";
            node.verb = "đập";
            node.requiredTool = ToolType.Pickaxe;
            node.minTier = 1;
            node.health = 8f;
            node.durabilityCostPerHit = 1;
            node.respawnSeconds = 600f;
            node.drops = new[]
            {
                new DropEntry { item = stone, min = 2, max = 4, chance = 1f },
                new DropEntry { item = pebble, min = 1, max = 2, chance = 0.5f }
            };
        });

        ResourceNodeData mountainRockNode = Upsert<ResourceNodeData>("Node_RockMountain", (node) =>
        {
            node.displayName = "Đá núi";
            node.verb = "đập";
            node.requiredTool = ToolType.Pickaxe;
            node.minTier = 1;
            node.health = 10f;
            node.durabilityCostPerHit = 1;
            node.respawnSeconds = 600f;
            node.drops = new[]
            {
                new DropEntry { item = stone, min = 2, max = 4, chance = 1f },
                new DropEntry { item = ironOre, min = 1, max = 2, chance = 0.35f }
            };
        });

        FishingTable fishingTable = Upsert<FishingTable>("FishingTable", table =>
        {
            table.drops = new[]
            {
                new FishDrop { item = smallFish, weight = 60f, min = 1, max = 2 },
                new FishDrop { item = largeFish, weight = 25f, min = 1, max = 1 },
                new FishDrop { item = eel, weight = 15f, min = 1, max = 1 }
            };
        });

        GatherSettings gatherSettings = Upsert<GatherSettings>("GatherSettings", settings =>
        {
            settings.tree = treeNode;
            settings.rockDefault = rockNode;
            settings.rockMountain = mountainRockNode;
            settings.fishing = fishingTable;
            settings.debugTools = new[] { stoneAxe, stonePickaxe, fishingRod };
            settings.pickups = new[]
            {
                new PickupRule { item = stick, biomes = new[] { Biome.Forest }, count = 40, minAmount = 1, maxAmount = 2, respawnSeconds = 300f },
                new PickupRule { item = fiber, biomes = new[] { Biome.Swamp, Biome.Forest }, count = 35, minAmount = 1, maxAmount = 2, respawnSeconds = 300f },
                new PickupRule { item = pebble, biomes = new[] { Biome.Beach, Biome.Mountain }, count = 40, minAmount = 1, maxAmount = 2, respawnSeconds = 300f }
            };
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = gatherSettings;
        Debug.Log("[Gather] Đã tạo hoặc kiểm tra dữ liệu mẫu tại " + DataDirectory);
    }

    private static ItemData CreateItem(string file, string id, string label, Color color, float weight)
    {
        return Upsert<ItemData>(file, item =>
        {
            item.id = id;
            item.displayName = label;
            item.maxStack = 50;
            item.weight = weight;
            item.color = color;
        });
    }

    private static ToolData CreateTool(string file, string id, string label, ToolType type,
        int tier, float power, int durability)
    {
        return Upsert<ToolData>(file, tool =>
        {
            tool.id = id;
            tool.displayName = label;
            tool.maxStack = 1;
            tool.weight = 1.5f;
            tool.color = new Color(0.58f, 0.58f, 0.62f);
            tool.toolType = type;
            tool.tier = tier;
            tool.power = power;
            tool.maxDurability = durability;
            tool.swingInterval = 0.6f;
        });
    }

    private static T Upsert<T>(string assetName, Action<T> configure) where T : ScriptableObject
    {
        string path = DataDirectory + "/" + assetName + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        asset.name = assetName;
        configure(asset);
        AssetDatabase.CreateAsset(asset, path);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        string[] segments = path.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = current + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[i]);
            current = next;
        }
    }
}

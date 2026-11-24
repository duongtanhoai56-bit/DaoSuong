using System.Collections.Generic;
using System.Reflection;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

public sealed class BaseBuildingManagerResetTests
{
    private const string ChestKey = "DAO_SUONG_TEST_RESTART_CHEST";
    private const string BaseSaveKey = "DAO_SUONG_BASE_V1";
    private const string OrphanChestKey = "DAO_SUONG_CHEST_V1_storage_40_80";
    private GameObject managerObject;
    private GameObject buildingObject;
    private GameObject orphanChestObject;
    private ItemData item;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(ChestKey);
        PlayerPrefs.DeleteKey(BaseSaveKey);
        PlayerPrefs.DeleteKey(OrphanChestKey);
        item = ScriptableObject.CreateInstance<ItemData>();
        item.id = "test_wood";
        item.displayName = "Gỗ thử";
        item.maxStack = 50;
        item.weight = 0.1f;

        managerObject = new GameObject("BaseBuildingManagerTest");
        managerObject.AddComponent<BaseBuildingManager>();
        buildingObject = new GameObject("TestStorageBuilding");
        PlacedBuilding placed = buildingObject.AddComponent<PlacedBuilding>();
        placed.Initialize("storage", 100f);
        StorageContainer chest = buildingObject.AddComponent<StorageContainer>();
        chest.ConfigurePersistence(ChestKey, new[] { item });
        Assert.AreEqual(0, chest.Add(item, 4));

        FieldInfo field = typeof(BaseBuildingManager).GetField("placedBuildings", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        var placedBuildings = (List<PlacedBuilding>)field.GetValue(managerObject.GetComponent<BaseBuildingManager>());
        placedBuildings.Add(placed);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(ChestKey);
        PlayerPrefs.DeleteKey(BaseSaveKey);
        PlayerPrefs.DeleteKey(OrphanChestKey);
        if (buildingObject != null) Object.DestroyImmediate(buildingObject);
        if (orphanChestObject != null) Object.DestroyImmediate(orphanChestObject);
        if (managerObject != null) Object.DestroyImmediate(managerObject);
        if (item != null) Object.DestroyImmediate(item);
    }

    [Test]
    public void NewRunRemovesPlacedBuildingsAndTheirStoredItems()
    {
        Assert.IsTrue(PlayerPrefs.HasKey(ChestKey));
        managerObject.GetComponent<BaseBuildingManager>().ResetForNewRun();
        Assert.IsFalse(PlayerPrefs.HasKey(ChestKey));
        Assert.IsTrue(buildingObject == null);
    }

    [Test]
    public void NewRunRemovesChestSaveForBuildingThatCouldNotBeLoaded()
    {
        // Simulate a persisted storage building whose catalog entry was unavailable at load time.
        PlayerPrefs.SetString(BaseSaveKey,
            "{\"version\":1,\"buildings\":[{\"id\":\"storage\",\"position\":{\"x\":4.0,\"y\":0.0,\"z\":8.0},\"yaw\":0.0}]}");
        orphanChestObject = new GameObject("OrphanStorageTest");
        StorageContainer orphanChest = orphanChestObject.AddComponent<StorageContainer>();
        orphanChest.ConfigurePersistence(OrphanChestKey, new[] { item });
        Assert.AreEqual(0, orphanChest.Add(item, 2));
        Assert.IsTrue(PlayerPrefs.HasKey(OrphanChestKey));

        managerObject.GetComponent<BaseBuildingManager>().ResetForNewRun();

        Assert.IsFalse(PlayerPrefs.HasKey(OrphanChestKey));
        Assert.IsFalse(PlayerPrefs.HasKey(BaseSaveKey));
    }
}

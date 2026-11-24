using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;

public sealed class StorageContainerPersistenceTests
{
    private const string TestKey = "DAO_SUONG_TEST_CHEST_PERSISTENCE";
    private GameObject containerObject;
    private StorageContainer container;
    private ItemData item;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(TestKey);
        item = ScriptableObject.CreateInstance<ItemData>();
        item.id = "test_ore";
        item.displayName = "Quặng thử";
        item.maxStack = 50;
        item.weight = 0.1f;
        containerObject = new GameObject("StorageContainerPersistenceTest");
        container = containerObject.AddComponent<StorageContainer>();
        container.ConfigurePersistence(TestKey, new[] { item });
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(TestKey);
        if (containerObject != null) Object.DestroyImmediate(containerObject);
        if (item != null) Object.DestroyImmediate(item);
    }

    [Test]
    public void ClearPersistenceDeletesSavedChestAndPreventsResave()
    {
        Assert.AreEqual(0, container.Add(item, 3));
        Assert.IsTrue(PlayerPrefs.HasKey(TestKey));

        container.ClearPersistence();

        Assert.IsFalse(PlayerPrefs.HasKey(TestKey));
        Assert.AreEqual(0, container.Add(item, 2));
        Assert.IsFalse(PlayerPrefs.HasKey(TestKey));
        Assert.AreEqual(2, container.Model.Count(item));
    }

    [Test]
    public void ConfigurePersistenceCanStartFreshAfterClear()
    {
        Assert.AreEqual(0, container.Add(item, 3));
        container.ClearPersistence();
        container.ConfigurePersistence(TestKey, new[] { item });

        Assert.AreEqual(0, container.Add(item, 1));
        Assert.AreEqual(1, container.Model.Count(item));
        Assert.IsTrue(PlayerPrefs.HasKey(TestKey));
    }
}

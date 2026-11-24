using Game.Core;
using UnityEngine;

namespace DaoSuong.Content
{
    [CreateAssetMenu(menuName = "Dao Suong/Items/Item Definition", fileName = "SO_Item_New")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string stableId;
        [SerializeField] private string displayNameKey;
        [SerializeField, Min(1)] private int maxStack = 20;
        [SerializeField, Min(0f)] private float weight = 0.1f;

        public StableId StableId => new StableId(stableId);
        public string DisplayNameKey => displayNameKey;
        public int MaxStack => maxStack;
        public float Weight => weight;
    }
}

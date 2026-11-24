using UnityEngine;

namespace DaoSuong.Content
{
    [CreateAssetMenu(menuName = "Dao Suong/Crafting/Recipe Definition", fileName = "SO_Recipe_New")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        [SerializeField] private string stableId;
        [SerializeField] private string displayNameKey;
        [SerializeField, Min(0.1f)] private float craftDurationSeconds = 1f;
        public string StableId => stableId;
        public string DisplayNameKey => displayNameKey;
        public float CraftDurationSeconds => craftDurationSeconds;
    }
}

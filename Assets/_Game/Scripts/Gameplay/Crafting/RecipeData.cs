using UnityEngine;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Crafting/Recipe", fileName = "Recipe_New")]
    public sealed class RecipeData : ScriptableObject
    {
        [Header("Định danh") ]
        public string id;
        public string displayName = "Công thức mới";
        [Header("Đầu vào") ]
        public RecipeIngredient[] ingredients;
        [Header("Đầu ra") ]
        public ItemData output;
        [Min(1)] public int outputAmount = 1;
        [Header("Bàn chế tạo") ]
        [Tooltip("0 = chế tạo tay, 1 = bàn chế tạo, 2 = lò rèn.")]
        [Range(0, 2)] public int requiredStationTier;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name.ToLowerInvariant().Replace(' ', '_');
            if (string.IsNullOrWhiteSpace(displayName)) displayName = name;
            outputAmount = Mathf.Max(1, outputAmount);
            requiredStationTier = Mathf.Clamp(requiredStationTier, 0, 2);
            if (ingredients == null) ingredients = System.Array.Empty<RecipeIngredient>();
            foreach (RecipeIngredient ingredient in ingredients)
                if (ingredient != null) ingredient.amount = Mathf.Max(1, ingredient.amount);
        }
    }
}

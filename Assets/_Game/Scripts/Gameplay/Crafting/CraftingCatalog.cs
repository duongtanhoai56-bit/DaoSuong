using UnityEngine;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Crafting/Catalog", fileName = "CraftingCatalog")]
    public sealed class CraftingCatalog : ScriptableObject
    {
        public RecipeData[] recipes;
    }
}

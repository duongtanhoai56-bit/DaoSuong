using System;
using UnityEngine;

namespace Game.Gameplay
{
    [Serializable]
    public sealed class RecipeIngredient
    {
        public ItemData item;
        [Min(1)] public int amount = 1;
    }
}

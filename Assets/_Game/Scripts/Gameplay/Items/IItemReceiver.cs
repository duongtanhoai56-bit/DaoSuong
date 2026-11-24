namespace Game.Gameplay
{
    /// <summary>Stable item-delivery contract shared by player inventory, chests, pickups and crafting.</summary>
    public interface IItemReceiver
    {
        /// <returns>Amount that could not be accepted.</returns>
        int Add(ItemData item, int amount);
    }
}

namespace Game.Systems.Gameplay
{
    /// <summary>What using one item does to a run.</summary>
    internal interface IItemEffect
    {
        /// <summary>True when the player picks a board cell before the item is used (a bomb).</summary>
        bool NeedsTarget { get; }

        /// <summary>True when using the item now would do something (a full health bar makes a potion useless).</summary>
        bool CanUse(Run run, float amount);

        /// <summary>Uses the item; the step is not done when nothing happened (the item is then not consumed).</summary>
        RunStep Use(Run run, float amount, int radius, Cell target);
    }
}

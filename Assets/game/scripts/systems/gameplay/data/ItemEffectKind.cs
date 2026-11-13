namespace Game.Systems.Gameplay
{
    /// <summary>What using an item does. A new kind needs an <see cref="IItemEffect"/> in <see cref="ItemEffects"/>.</summary>
    internal enum ItemEffectKind
    {
        /// <summary>Restores the hero's health.</summary>
        Heal = 0,

        /// <summary>Gives the hero armor at once.</summary>
        Armor = 1,

        /// <summary>Clears a square of gems around a cell the player picks.</summary>
        Bomb = 2,

        /// <summary>Shuffles the board.</summary>
        Shuffle = 3,
    }
}

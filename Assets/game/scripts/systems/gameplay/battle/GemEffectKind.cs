namespace Game.Systems.Gameplay
{
    /// <summary>What matching a gem does in battle. A new kind needs an <see cref="IGemEffect"/> in <see cref="GemEffects"/>.</summary>
    internal enum GemEffectKind
    {
        /// <summary>The hero strikes the monster.</summary>
        Attack = 0,

        /// <summary>The hero recovers health.</summary>
        Heal = 1,

        /// <summary>The hero gains armor that wears off over time and absorbs hits first.</summary>
        Defense = 2,

        /// <summary>Fills the healer's magic bar; a full bar casts the healer's skill.</summary>
        Magic = 3,
    }
}

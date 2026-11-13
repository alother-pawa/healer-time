namespace Game.Systems.Gameplay
{
    /// <summary>Battle numbers of the chosen hero.</summary>
    internal readonly struct HeroStats
    {
        internal HeroStats(float maxHealth, float attackDamage, float attackInterval, float strikeMultiplier)
        {
            MaxHealth = maxHealth;
            AttackDamage = attackDamage;
            AttackInterval = attackInterval;
            StrikeMultiplier = strikeMultiplier;
        }

        internal float MaxHealth { get; }

        /// <summary>Damage of the automatic attack.</summary>
        internal float AttackDamage { get; }

        /// <summary>Seconds between automatic attacks.</summary>
        internal float AttackInterval { get; }

        /// <summary>Multiplier of the damage of Attack gems.</summary>
        internal float StrikeMultiplier { get; }
    }
}

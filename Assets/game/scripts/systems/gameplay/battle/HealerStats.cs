namespace Game.Systems.Gameplay
{
    /// <summary>Battle numbers of the chosen healer.</summary>
    internal readonly struct HealerStats
    {
        internal HealerStats(float magicCapacity, float skillDamage, float skillHeal)
        {
            MagicCapacity = magicCapacity;
            SkillDamage = skillDamage;
            SkillHeal = skillHeal;
        }

        /// <summary>Magic needed for the skill.</summary>
        internal float MagicCapacity { get; }

        /// <summary>Damage of the skill on the monster (before the per-monster growth).</summary>
        internal float SkillDamage { get; }

        /// <summary>Health the skill gives the hero.</summary>
        internal float SkillHeal { get; }
    }
}

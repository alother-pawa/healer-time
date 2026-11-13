namespace Game.Systems.Gameplay
{
    /// <summary>What happened in a <see cref="BattleEvent"/>.</summary>
    internal enum BattleEventKind
    {
        /// <summary>A monster entered at the left edge (Amount: 1 for a boss).</summary>
        MonsterSpawned = 0,

        /// <summary>The hero's automatic attack hit (Amount: damage).</summary>
        HeroAttack = 1,

        /// <summary>Attack gems: the hero's strong strike hit (Amount: damage).</summary>
        HeroStrike = 2,

        /// <summary>The monster hit the hero (Amount: damage to health; Absorbed: damage taken by armor).</summary>
        MonsterAttack = 3,

        /// <summary>The hero recovered health (Amount: health actually gained).</summary>
        Healed = 4,

        /// <summary>The hero gained armor (Amount: armor actually gained).</summary>
        ArmorGained = 5,

        /// <summary>The healer's bar filled up (Amount: magic added).</summary>
        MagicCharged = 6,

        /// <summary>The healer's skill hit the monster (Amount: damage; Absorbed: health given to the hero).</summary>
        HealerSkill = 7,

        /// <summary>The monster died (Amount: coins earned; Value: score earned; Boss flag).</summary>
        MonsterDied = 8,

        /// <summary>The hero's health reached zero: the run is over unless the hero revives.</summary>
        HeroDied = 9,

        /// <summary>The hero came back with part of their health.</summary>
        Revived = 10,
    }
}

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

        /// <summary>The healer's skill hit the monster (Amount: damage; the health it gave is on its StrikeWindup or StrikeHeld).</summary>
        HealerSkill = 7,

        /// <summary>The monster died (Amount: coins earned; Value: score earned; Boss flag).</summary>
        MonsterDied = 8,

        /// <summary>The hero's health reached zero: the run is over.</summary>
        HeroDied = 9,

        /// <summary>The hero starts swinging; its <see cref="HeroAttack"/> lands <see cref="BattleTuning.AttackWindup"/> later.</summary>
        HeroWindup = 11,

        /// <summary>The monster starts swinging; its <see cref="MonsterAttack"/> lands <see cref="BattleTuning.AttackWindup"/> later.</summary>
        MonsterWindup = 12,

        /// <summary>
        /// A <see cref="HeroStrike"/> or <see cref="HealerSkill"/> was cast while no monster stood in reach (the next one still
        /// walking in): it waits and lands once that monster arrived (Amount: damage held; Absorbed: health the skill gave the
        /// hero at once; Value: the held kind).
        /// </summary>
        StrikeHeld = 13,

        /// <summary>
        /// Strikes start swinging (cast on a monster in reach, or held ones once the monster arrived); they land
        /// <see cref="BattleTuning.AttackWindup"/> later (Amount: damage; Absorbed: health a skill gave the hero at once;
        /// Value: 1 if a hero strike swings, plus 2 if a healer skill does).
        /// </summary>
        StrikeWindup = 14,
    }
}

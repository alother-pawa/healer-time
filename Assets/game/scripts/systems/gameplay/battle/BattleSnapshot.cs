namespace Game.Systems.Gameplay
{
    /// <summary>The values a view shows, at one moment of the battle.</summary>
    internal readonly struct BattleSnapshot
    {
        internal BattleSnapshot(float heroHealth, float heroMaxHealth, float armor, float magic, float magicCapacity,
            float monsterHealth, float monsterMaxHealth, int monsterIndex, int score, int kills, int coins)
        {
            HeroHealth = heroHealth;
            HeroMaxHealth = heroMaxHealth;
            Armor = armor;
            Magic = magic;
            MagicCapacity = magicCapacity;
            MonsterHealth = monsterHealth;
            MonsterMaxHealth = monsterMaxHealth;
            MonsterIndex = monsterIndex;
            Score = score;
            Kills = kills;
            Coins = coins;
        }

        internal float HeroHealth { get; }

        internal float HeroMaxHealth { get; }

        internal float Armor { get; }

        internal float Magic { get; }

        internal float MagicCapacity { get; }

        internal float MonsterHealth { get; }

        internal float MonsterMaxHealth { get; }

        /// <summary>Index of the monster on the field (0 = first of the run).</summary>
        internal int MonsterIndex { get; }

        internal int Score { get; }

        internal int Kills { get; }

        /// <summary>Coins earned this run.</summary>
        internal int Coins { get; }
    }
}

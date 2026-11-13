namespace Game.Systems.Gameplay
{
    /// <summary>Base numbers of one monster kind before the growth of later monsters.</summary>
    internal readonly struct MonsterTemplate
    {
        internal MonsterTemplate(float health, float damage, float attackInterval, int coins, int score, bool isBoss,
            int unlockAt, float weight)
        {
            Health = health;
            Damage = damage;
            AttackInterval = attackInterval;
            Coins = coins;
            Score = score;
            IsBoss = isBoss;
            UnlockAt = unlockAt;
            Weight = weight;
        }

        internal float Health { get; }

        internal float Damage { get; }

        internal float AttackInterval { get; }

        internal int Coins { get; }

        internal int Score { get; }

        /// <summary>Bosses appear only at every boss slot; other kinds never do.</summary>
        internal bool IsBoss { get; }

        /// <summary>First monster index (0-based) this kind can appear at.</summary>
        internal int UnlockAt { get; }

        /// <summary>Relative chance among the kinds unlocked at a slot.</summary>
        internal float Weight { get; }
    }
}

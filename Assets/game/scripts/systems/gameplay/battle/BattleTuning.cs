using System;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Run-wide balance numbers (serialized inside the balance config, so they are tuned in the Inspector; the defaults
    /// here are the shipped values).
    /// </summary>
    [Serializable]
    internal sealed class BattleTuning
    {
        [Tooltip("Armor lost per second.")]
        [Min(0f)] [SerializeField] private float _armorDecayPerSecond = 2.5f;
        [Tooltip("Most armor the hero holds, as a share of max health.")]
        [Min(0f)] [SerializeField] private float _armorCap = 1f;
        [Tooltip("Extra power of every combo step after the first (0.25 = +25% per step).")]
        [Min(0f)] [SerializeField] private float _comboBonus = 0.25f;
        [Tooltip("Extra power of every gem above three in one group (0.2 = +20% per gem).")]
        [Min(0f)] [SerializeField] private float _largeMatchBonus = 0.2f;
        [Tooltip("Seconds a new monster walks in before it fights.")]
        [Min(0f)] [SerializeField] private float _approachSeconds = 1.4f;
        [Tooltip("Health growth per monster index (0.12 = +12% per monster).")]
        [Min(0f)] [SerializeField] private float _healthGrowth = 0.12f;
        [Min(0f)] [SerializeField] private float _damageGrowth = 0.07f;
        [Tooltip("Attack speed growth per monster index (shorter intervals).")]
        [Min(0f)] [SerializeField] private float _speedGrowth = 0.02f;
        [Min(0.1f)] [SerializeField] private float _minAttackInterval = 0.8f;
        [Tooltip("Every Nth monster is a boss.")]
        [Min(2)] [SerializeField] private int _bossEvery = 5;
        [Tooltip("Extra coins per kill for every N monsters already beaten.")]
        [Min(1)] [SerializeField] private int _coinGrowthEvery = 5;
        [Min(0f)] [SerializeField] private float _scoreGrowth = 0.1f;
        [Min(0)] [SerializeField] private int _scorePerGem = 10;
        [Tooltip("Healer skill damage growth per monster index, so the skill keeps up with tougher monsters.")]
        [Min(0f)] [SerializeField] private float _skillGrowth = 0.08f;
        [Range(0.05f, 1f)] [SerializeField] private float _reviveHealth = 0.5f;
        [Min(0f)] [SerializeField] private float _reviveArmor = 30f;
        [Min(0)] [SerializeField] private int _maxRevives = 1;

        internal float ArmorDecayPerSecond => _armorDecayPerSecond;

        internal float ArmorCap => _armorCap;

        internal float ComboBonus => _comboBonus;

        internal float LargeMatchBonus => _largeMatchBonus;

        internal float ApproachSeconds => _approachSeconds;

        internal float HealthGrowth => _healthGrowth;

        internal float DamageGrowth => _damageGrowth;

        internal float SpeedGrowth => _speedGrowth;

        internal float MinAttackInterval => _minAttackInterval;

        internal int BossEvery => _bossEvery;

        internal int CoinGrowthEvery => _coinGrowthEvery;

        internal float ScoreGrowth => _scoreGrowth;

        internal int ScorePerGem => _scorePerGem;

        internal float SkillGrowth => _skillGrowth;

        internal float ReviveHealth => _reviveHealth;

        internal float ReviveArmor => _reviveArmor;

        internal int MaxRevives => _maxRevives;
    }
}

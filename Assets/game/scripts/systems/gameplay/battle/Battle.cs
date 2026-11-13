using System;
using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The automatic fight of one run (pure logic; time comes from <see cref="Tick"/>). The hero attacks on a timer; one
    /// monster at a time walks in from the left (<see cref="BattleTuning.ApproachSeconds"/>, no attacks either way meanwhile),
    /// fights, dies and is replaced by a stronger one; every Nth monster is a boss. Matched gems change the battle through
    /// <see cref="ApplyBoard"/>; items through <see cref="Heal"/> and <see cref="AddArmor"/>. Armor wears off over time and
    /// absorbs hits before health. A full magic bar casts the healer's skill. The run ends when the hero's health reaches
    /// zero (<see cref="IsOver"/>); <see cref="Revive"/> brings the hero back once per run. Every change is reported as
    /// <see cref="BattleEvent"/>s carrying the values after it.
    /// </summary>
    internal sealed class Battle
    {
        private readonly HeroStats _hero;
        private readonly HealerStats _healer;
        private readonly IReadOnlyList<MonsterTemplate> _monsters;
        private readonly IReadOnlyList<GemRule> _gems;
        private readonly BattleTuning _tuning;
        private readonly Random _random;
        private float _heroAttackTimer;
        private float _monsterAttackTimer;

        internal Battle(HeroStats hero, HealerStats healer, IReadOnlyList<MonsterTemplate> monsters, IReadOnlyList<GemRule> gems,
            BattleTuning tuning, int seed)
        {
            if (monsters == null || !HasKind(monsters, false))
            {
                throw new ArgumentException("The battle needs at least one monster kind that is not a boss.", nameof(monsters));
            }

            _hero = hero;
            _healer = healer;
            _monsters = monsters;
            _gems = gems;
            _tuning = tuning;
            _random = new Random(seed);
            HeroHealth = hero.MaxHealth;
            MonsterIndex = -1;
        }

        internal float HeroHealth { get; private set; }

        internal float HeroMaxHealth => _hero.MaxHealth;

        internal float Armor { get; private set; }

        internal float Magic { get; private set; }

        internal float MagicCapacity => _healer.MagicCapacity;

        /// <summary>Index of the monster on the field (0 = first of the run); -1 before <see cref="Start"/>.</summary>
        internal int MonsterIndex { get; private set; }

        /// <summary>Template index of the monster on the field (its look).</summary>
        internal int MonsterKind { get; private set; }

        internal bool MonsterIsBoss { get; private set; }

        internal float MonsterHealth { get; private set; }

        internal float MonsterMaxHealth { get; private set; }

        internal float MonsterDamage { get; private set; }

        internal float MonsterAttackInterval { get; private set; }

        /// <summary>Seconds left before the monster on the field reaches the hero.</summary>
        internal float ApproachLeft { get; private set; }

        /// <summary>0 when the monster just entered, 1 when it reached the hero.</summary>
        internal float ApproachProgress => _tuning.ApproachSeconds <= 0f ? 1f : 1f - ApproachLeft / _tuning.ApproachSeconds;

        internal bool IsFighting => ApproachLeft <= 0f && !IsOver;

        internal int Score { get; private set; }

        internal int Kills { get; private set; }

        /// <summary>Coins earned this run.</summary>
        internal int Coins { get; private set; }

        internal int RevivesUsed { get; private set; }

        internal bool IsOver { get; private set; }

        internal bool CanRevive => IsOver && RevivesUsed < _tuning.MaxRevives;

        internal BattleSnapshot Snapshot => new BattleSnapshot(HeroHealth, HeroMaxHealth, Armor, Magic, MagicCapacity, MonsterHealth,
            MonsterMaxHealth, MonsterIndex, Score, Kills, Coins);

        /// <summary>Sends in the first monster.</summary>
        internal void Start(List<BattleEvent> events)
        {
            if (MonsterIndex < 0)
            {
                SpawnNext(events);
            }
        }

        /// <summary>Advances time: armor wears off, the monster walks in or both sides attack on their timers.</summary>
        internal void Tick(float seconds, List<BattleEvent> events)
        {
            if (IsOver || MonsterIndex < 0 || seconds <= 0f)
            {
                return;
            }

            Armor = Math.Max(0f, Armor - _tuning.ArmorDecayPerSecond * seconds);
            if (ApproachLeft > 0f)
            {
                ApproachLeft = Math.Max(0f, ApproachLeft - seconds);
                return;
            }

            _heroAttackTimer += seconds;
            if (_heroAttackTimer >= _hero.AttackInterval)
            {
                _heroAttackTimer -= _hero.AttackInterval;
                DamageMonster(_hero.AttackDamage, BattleEventKind.HeroAttack, -1, 0f, events);
                if (ApproachLeft > 0f)
                {
                    return;
                }
            }

            _monsterAttackTimer += seconds;
            if (_monsterAttackTimer >= MonsterAttackInterval)
            {
                _monsterAttackTimer -= MonsterAttackInterval;
                HitHero(MonsterDamage, events);
            }
        }

        /// <summary>Applies a board action's matched gems, kind by kind in gem order, and adds their score.</summary>
        internal void ApplyBoard(BoardResolution resolution, List<BattleEvent> events)
        {
            if (IsOver || !resolution.Valid)
            {
                return;
            }

            Score += GemPayout.Score(resolution, _tuning);
            float[] powers = GemPayout.Powers(resolution, _gems.Count, _tuning);
            for (int gem = 0; gem < powers.Length && !IsOver; gem++)
            {
                if (powers[gem] > 0f)
                {
                    GemEffects.For(_gems[gem].Effect).Apply(this, gem, powers[gem] * _gems[gem].Power, events);
                }
            }
        }

        /// <summary>The hero's strong strike (Attack gems): <paramref name="amount"/> times the hero's strike multiplier.</summary>
        internal void Strike(float amount, int gem, List<BattleEvent> events)
        {
            DamageMonster(amount * _hero.StrikeMultiplier, BattleEventKind.HeroStrike, gem, 0f, events);
        }

        /// <summary>Restores health up to the maximum; returns the health gained.</summary>
        internal float Heal(float amount, int gem, List<BattleEvent> events)
        {
            if (IsOver || amount <= 0f)
            {
                return 0f;
            }

            float gained = Math.Min(amount, HeroMaxHealth - HeroHealth);
            HeroHealth += gained;
            events.Add(Event(BattleEventKind.Healed, gained, 0f, 0, gem));
            return gained;
        }

        /// <summary>Adds armor up to the cap; returns the armor gained.</summary>
        internal float AddArmor(float amount, int gem, List<BattleEvent> events)
        {
            if (IsOver || amount <= 0f)
            {
                return 0f;
            }

            float gained = Math.Min(amount, Math.Max(0f, HeroMaxHealth * _tuning.ArmorCap - Armor));
            Armor += gained;
            events.Add(Event(BattleEventKind.ArmorGained, gained, 0f, 0, gem));
            return gained;
        }

        /// <summary>Fills the magic bar; when it is full the healer casts and the bar goes back to zero.</summary>
        internal void ChargeMagic(float amount, int gem, List<BattleEvent> events)
        {
            if (IsOver || amount <= 0f)
            {
                return;
            }

            Magic = Math.Min(MagicCapacity, Magic + amount);
            events.Add(Event(BattleEventKind.MagicCharged, amount, 0f, 0, gem));
            if (Magic < MagicCapacity)
            {
                return;
            }

            Magic = 0f;
            float healed = Math.Min(_healer.SkillHeal, HeroMaxHealth - HeroHealth);
            HeroHealth += healed;
            float damage = _healer.SkillDamage * (1f + _tuning.SkillGrowth * Math.Max(0, MonsterIndex));
            DamageMonster(damage, BattleEventKind.HealerSkill, gem, healed, events);
        }

        /// <summary>Brings the hero back after the run ended (once per run): part of the health and some armor.</summary>
        internal bool Revive(List<BattleEvent> events)
        {
            if (!CanRevive)
            {
                return false;
            }

            RevivesUsed++;
            IsOver = false;
            HeroHealth = HeroMaxHealth * _tuning.ReviveHealth;
            Armor = Math.Min(HeroMaxHealth * _tuning.ArmorCap, _tuning.ReviveArmor);
            _monsterAttackTimer = 0f;
            events.Add(Event(BattleEventKind.Revived, HeroHealth, 0f, 0, -1));
            return true;
        }

        private void HitHero(float damage, List<BattleEvent> events)
        {
            float absorbed = Math.Min(Armor, damage);
            Armor -= absorbed;
            float taken = Math.Min(HeroHealth, damage - absorbed);
            HeroHealth -= taken;
            events.Add(Event(BattleEventKind.MonsterAttack, taken, absorbed, 0, -1));
            if (HeroHealth <= 0f)
            {
                HeroHealth = 0f;
                IsOver = true;
                events.Add(Event(BattleEventKind.HeroDied, 0f, 0f, 0, -1));
            }
        }

        private void DamageMonster(float damage, BattleEventKind kind, int gem, float absorbed, List<BattleEvent> events)
        {
            if (MonsterIndex < 0 || damage <= 0f)
            {
                return;
            }

            float dealt = Math.Min(MonsterHealth, damage);
            MonsterHealth -= dealt;
            events.Add(Event(kind, dealt, absorbed, 0, gem));
            if (MonsterHealth > 0f)
            {
                return;
            }

            MonsterTemplate template = _monsters[MonsterKind];
            int coins = template.Coins + MonsterIndex / Math.Max(1, _tuning.CoinGrowthEvery);
            int score = (int)Math.Round(template.Score * (1f + _tuning.ScoreGrowth * MonsterIndex));
            Kills++;
            Coins += coins;
            Score += score;
            events.Add(Event(BattleEventKind.MonsterDied, coins, 0f, score, gem));
            SpawnNext(events);
        }

        private void SpawnNext(List<BattleEvent> events)
        {
            MonsterIndex++;
            bool bossSlot = (MonsterIndex + 1) % Math.Max(2, _tuning.BossEvery) == 0 && HasKind(_monsters, true);
            MonsterKind = Pick(bossSlot);
            MonsterTemplate template = _monsters[MonsterKind];
            MonsterIsBoss = template.IsBoss;
            MonsterMaxHealth = template.Health * (1f + _tuning.HealthGrowth * MonsterIndex);
            MonsterHealth = MonsterMaxHealth;
            MonsterDamage = template.Damage * (1f + _tuning.DamageGrowth * MonsterIndex);
            MonsterAttackInterval = Math.Max(_tuning.MinAttackInterval,
                template.AttackInterval / (1f + _tuning.SpeedGrowth * MonsterIndex));
            ApproachLeft = _tuning.ApproachSeconds;
            _heroAttackTimer = 0f;
            _monsterAttackTimer = 0f;
            events.Add(Event(BattleEventKind.MonsterSpawned, MonsterIsBoss ? 1f : 0f, 0f, MonsterKind, -1));
        }

        // Weighted pick among the unlocked kinds of the slot (bosses for a boss slot); the earliest kind when none is unlocked.
        private int Pick(bool boss)
        {
            float total = 0f;
            int fallback = -1;
            for (int i = 0; i < _monsters.Count; i++)
            {
                if (_monsters[i].IsBoss != boss)
                {
                    continue;
                }

                if (fallback < 0 || _monsters[i].UnlockAt < _monsters[fallback].UnlockAt)
                {
                    fallback = i;
                }

                if (_monsters[i].UnlockAt <= MonsterIndex)
                {
                    total += Math.Max(0f, _monsters[i].Weight);
                }
            }

            double roll = _random.NextDouble() * total;
            for (int i = 0; i < _monsters.Count && total > 0f; i++)
            {
                if (_monsters[i].IsBoss != boss || _monsters[i].UnlockAt > MonsterIndex || _monsters[i].Weight <= 0f)
                {
                    continue;
                }

                roll -= _monsters[i].Weight;
                if (roll < 0d)
                {
                    return i;
                }
            }

            return fallback;
        }

        private BattleEvent Event(BattleEventKind kind, float amount, float absorbed, int value, int gem)
        {
            return new BattleEvent(kind, amount, absorbed, value, MonsterIsBoss, gem, Snapshot);
        }

        private static bool HasKind(IReadOnlyList<MonsterTemplate> monsters, bool boss)
        {
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i].IsBoss == boss)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

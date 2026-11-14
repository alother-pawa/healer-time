using System.Collections.Generic;
using System.Linq;
using Game.Systems.Gameplay;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    internal sealed class BattleTests
    {
        private const float Approach = 1.4f;
        private const float Windup = 0.3f;

        private static readonly MonsterTemplate Grunt = new MonsterTemplate(30f, 10f, 2f, 5, 100, false, 0, 1f);
        private static readonly MonsterTemplate Boss = new MonsterTemplate(100f, 20f, 2f, 20, 500, true, 0, 1f);

        private static readonly GemRule[] Gems =
        {
            new GemRule(GemEffectKind.Attack, 5f), new GemRule(GemEffectKind.Heal, 4f),
            new GemRule(GemEffectKind.Defense, 5f), new GemRule(GemEffectKind.Magic, 10f),
        };

        private readonly List<BattleEvent> _events = new List<BattleEvent>();

        [SetUp]
        public void SetUp()
        {
            _events.Clear();
        }

        [Test]
        public void TheFirstMonsterWalksInWithoutFighting()
        {
            Battle battle = NewBattle();
            Assert.That(_events.Single().Kind, Is.EqualTo(BattleEventKind.MonsterSpawned));
            battle.Tick(1f, _events);
            Assert.That(battle.IsFighting, Is.False);
            Assert.That(battle.MonsterHealth, Is.EqualTo(30f));
            Assert.That(battle.HeroHealth, Is.EqualTo(100f));
            Assert.That(battle.ApproachProgress, Is.EqualTo(1f / Approach).Within(0.01f));
        }

        [Test]
        public void TheHeroAttacksOnItsTimerOnceTheMonsterArrived()
        {
            Battle battle = NewBattle();
            battle.Tick(Approach, _events);
            Assert.That(battle.IsFighting, Is.True);
            battle.Tick(1f, _events);
            Assert.That(battle.MonsterHealth, Is.EqualTo(20f));
            Assert.That(_events.Last().Kind, Is.EqualTo(BattleEventKind.HeroAttack));
            Assert.That(_events.Last().After.MonsterHealth, Is.EqualTo(20f));
        }

        [Test]
        public void EverySwingStartsBeforeItsHitLands()
        {
            Battle battle = NewBattle();
            battle.Tick(Approach, _events);
            _events.Clear();
            battle.Tick(0.75f, _events);
            Assert.That(_events.Select(e => e.Kind), Is.EqualTo(new[] { BattleEventKind.HeroWindup }), "the hero swings 0.3 s before its hit");
            Assert.That(battle.MonsterHealth, Is.EqualTo(30f), "nothing is hit during the swing");
            battle.Tick(0.25f, _events);
            Assert.That(_events.Last().Kind, Is.EqualTo(BattleEventKind.HeroAttack));
            Assert.That(battle.MonsterHealth, Is.EqualTo(20f));
            battle.Tick(0.8f, _events);
            Assert.That(_events.Last().Kind, Is.EqualTo(BattleEventKind.MonsterWindup), "the monster swings before its hit at 2 s");
        }

        [Test]
        public void ArmorAbsorbsHitsBeforeHealthAndWearsOff()
        {
            Battle battle = NewBattle();
            battle.Tick(Approach, _events);
            battle.AddArmor(20f, -1, _events);
            battle.Tick(2f, _events);
            BattleEvent hit = _events.Last(e => e.Kind == BattleEventKind.MonsterAttack);
            // 20 armor - 2 s × 2.5 decay = 15 left when the 10 damage lands: all absorbed.
            Assert.That(hit.Absorbed, Is.EqualTo(10f).Within(0.01f));
            Assert.That(hit.Amount, Is.EqualTo(0f));
            Assert.That(battle.HeroHealth, Is.EqualTo(100f));
            Assert.That(battle.Armor, Is.EqualTo(5f).Within(0.01f));
        }

        [Test]
        public void ArmorIsCappedAtMaxHealth()
        {
            Battle battle = NewBattle();
            float gained = battle.AddArmor(500f, -1, _events);
            Assert.That(gained, Is.EqualTo(100f));
            Assert.That(battle.Armor, Is.EqualTo(100f));
        }

        [Test]
        public void KillingTheMonsterPaysAndSendsAStrongerOne()
        {
            Battle battle = NewBattle();
            battle.Tick(Approach, _events);
            battle.Strike(30f, 0, _events);
            Assert.That(battle.Kills, Is.EqualTo(0), "the strike swings first");
            battle.Tick(Windup, _events);
            Assert.That(battle.Kills, Is.EqualTo(1));
            Assert.That(battle.Coins, Is.EqualTo(5));
            Assert.That(battle.Score, Is.EqualTo(100));
            Assert.That(battle.MonsterIndex, Is.EqualTo(1));
            Assert.That(battle.MonsterMaxHealth, Is.GreaterThan(30f));
            Assert.That(battle.IsFighting, Is.False, "the next monster walks in");
            Assert.That(_events.Select(e => e.Kind), Is.EqualTo(new[]
            {
                BattleEventKind.MonsterSpawned, BattleEventKind.StrikeWindup, BattleEventKind.HeroStrike, BattleEventKind.MonsterDied,
                BattleEventKind.MonsterSpawned,
            }));
        }

        [Test]
        public void EveryFifthMonsterIsABoss()
        {
            Battle battle = NewBattle();
            for (int i = 0; i < 4; i++)
            {
                Assert.That(battle.MonsterIsBoss, Is.False, $"monster {i}");
                battle.Tick(Approach, _events);
                battle.Strike(10000f, 0, _events);
                battle.Tick(Windup, _events);
            }

            Assert.That(battle.MonsterIndex, Is.EqualTo(4));
            Assert.That(battle.MonsterIsBoss, Is.True);
            battle.Tick(Approach, _events);
            battle.Strike(10000f, 0, _events);
            battle.Tick(Windup, _events);
            Assert.That(battle.MonsterIsBoss, Is.False);
        }

        [Test]
        public void AFullMagicBarCastsTheSkillAndEmpties()
        {
            Battle battle = NewBattle();
            battle.Tick(Approach, _events);
            battle.ChargeMagic(60f, 3, _events);
            Assert.That(battle.Magic, Is.EqualTo(60f));
            battle.ChargeMagic(60f, 3, _events);
            Assert.That(battle.Magic, Is.EqualTo(0f));
            battle.Tick(Windup, _events);
            BattleEvent skill = _events.Single(e => e.Kind == BattleEventKind.HealerSkill);
            Assert.That(skill.Amount, Is.EqualTo(30f), "the skill deals 50 but the monster only had 30");
            Assert.That(battle.Kills, Is.EqualTo(1));
        }

        [Test]
        public void StrikesCastBeforeTheMonsterArrivedWaitForIt()
        {
            Battle battle = NewBattle();
            battle.Strike(10f, 0, _events);
            battle.ChargeMagic(100f, 3, _events);
            Assert.That(battle.MonsterHealth, Is.EqualTo(30f), "nothing lands on a monster still walking in");
            Assert.That(battle.HeldStrikes, Is.EqualTo(2));
            Assert.That(_events.Count(e => e.Kind == BattleEventKind.StrikeHeld), Is.EqualTo(2));

            _events.Clear();
            battle.Tick(Approach, _events);
            Assert.That(_events.Single().Kind, Is.EqualTo(BattleEventKind.StrikeWindup));
            Assert.That(_events.Single().Value, Is.EqualTo(3), "a hero strike and a healer skill are held");
            Assert.That(battle.MonsterHealth, Is.EqualTo(30f), "the held strikes are still swinging");

            battle.Tick(0.3f, _events);
            Assert.That(_events.Select(e => e.Kind), Is.EqualTo(new[]
            {
                BattleEventKind.StrikeWindup, BattleEventKind.HeroStrike, BattleEventKind.HealerSkill, BattleEventKind.MonsterDied,
                BattleEventKind.MonsterSpawned,
            }), "the strike hits 10, the skill finishes the grunt");
            Assert.That(battle.HeldStrikes, Is.EqualTo(0));
        }

        [Test]
        public void StrikesLeftAfterAKillWaitForTheNextMonster()
        {
            Battle battle = NewBattle();
            battle.Strike(30f, 0, _events);
            battle.Strike(5f, 0, _events);
            battle.Tick(Approach, _events);
            battle.Tick(0.3f, _events);
            Assert.That(battle.Kills, Is.EqualTo(1));
            Assert.That(battle.HeldStrikes, Is.EqualTo(1), "the second strike waits for the next monster");
            float health = battle.MonsterHealth;
            battle.Tick(Approach, _events);
            battle.Tick(0.3f, _events);
            Assert.That(battle.MonsterHealth, Is.EqualTo(health - 5f).Within(0.001f));
        }

        [Test]
        public void TheRunEndsWhenTheHeroFalls()
        {
            Battle battle = NewBattle();
            // Monsters grow stronger every kill, so the hero falls eventually.
            while (!battle.IsOver)
            {
                battle.Tick(2f, _events);
            }

            Assert.That(_events.Last().Kind, Is.EqualTo(BattleEventKind.HeroDied));
            Assert.That(battle.HeroHealth, Is.EqualTo(0f));
        }

        [Test]
        public void NothingHappensAfterTheRunEnded()
        {
            Battle battle = NewBattle();
            battle.Tick(Approach, _events);
            while (!battle.IsOver)
            {
                battle.Tick(2f, _events);
            }

            int count = _events.Count;
            battle.Tick(5f, _events);
            battle.Heal(10f, -1, _events);
            battle.AddArmor(10f, -1, _events);
            Assert.That(_events.Count, Is.EqualTo(count));
        }

        [Test]
        public void EveryCascadeStepAppliesAsItPopsWithComboAndSizeBonus()
        {
            Battle battle = NewBattle(heroHealth: 100f);
            battle.Tick(Approach, _events);
            _events.Clear();
            var cells = new List<Cell> { new Cell(0, 0), new Cell(0, 1), new Cell(0, 2) };
            var four = new List<Cell> { new Cell(1, 0), new Cell(1, 1), new Cell(1, 2), new Cell(1, 3) };
            var first = new CascadeStep(0, new[] { new GemGroup(2, cells, 3), new GemGroup(0, four, 4) }, new GemMove[0]);
            var combo = new CascadeStep(1, new[] { new GemGroup(0, cells, 3) }, new GemMove[0]);
            var tuning = new BattleTuning();
            Assert.That(GemPayout.Powers(first, 4, tuning)[0], Is.EqualTo(4f * (1f + tuning.LargeMatchBonus)).Within(0.001f));
            Assert.That(GemPayout.Powers(first, 4, tuning)[2], Is.EqualTo(3f));
            Assert.That(GemPayout.Powers(combo, 4, tuning)[0], Is.EqualTo(3f * (1f + tuning.ComboBonus)).Within(0.001f));

            battle.ApplyStep(first, _events);
            Assert.That(_events[0].Kind, Is.EqualTo(BattleEventKind.StrikeWindup), "Attack (gem 0) applies before Defense (gem 2)");
            Assert.That(_events[0].Gem, Is.EqualTo(0));
            Assert.That(_events.Any(e => e.Kind == BattleEventKind.ArmorGained && e.Gem == 2 && e.Amount == 15f), "armor at once");
            battle.ApplyStep(combo, _events);
            battle.Tick(Windup, _events);
            Assert.That(_events.Count(e => e.Kind == BattleEventKind.HeroStrike), Is.EqualTo(2), "one strike per step");
            Assert.That(battle.Kills, Is.EqualTo(1), "the two strikes kill the 30-health grunt");
            Assert.That(battle.Score, Is.EqualTo(GemPayout.Score(first, tuning) + GemPayout.Score(combo, tuning) + Grunt.Score));
        }

        private Battle NewBattle(float heroHealth = 100f)
        {
            var battle = new Battle(new HeroStats(heroHealth, 10f, 1f, 1f), new HealerStats(100f, 50f, 10f),
                new[] { Grunt, Boss }, Gems, new BattleTuning(), 1);
            battle.Start(_events);
            return battle;
        }
    }
}

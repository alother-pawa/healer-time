using System.Linq;
using Game.Contracts.Roster;

namespace Game.Systems.Gameplay
{
    /// <summary>Builds a <see cref="Run"/> from the balance config and the chosen characters.</summary>
    internal static class RunFactory
    {
        internal static Run Create(BalanceConfig balance, HeroDefinition hero, HealerDefinition healer, int seed)
        {
            float[] weights = balance.Gems.Select(gem => gem.SpawnWeight).ToArray();
            GemRule[] rules = balance.Gems.Select(gem => new GemRule(gem.Effect, gem.Power)).ToArray();
            MonsterTemplate[] monsters = balance.Monsters.Select(monster => monster.Template).ToArray();
            var engine = new Match3Engine(balance.Rows, balance.Columns, new GemSpawner(weights, seed));
            var battle = new Battle(new HeroStats(hero.MaxHealth, hero.AttackDamage, hero.AttackInterval, hero.StrikeMultiplier),
                new HealerStats(healer.MagicCapacity, healer.SkillDamage, healer.SkillHeal), monsters, rules, balance.Tuning, seed + 1);
            return new Run(engine, battle);
        }
    }
}

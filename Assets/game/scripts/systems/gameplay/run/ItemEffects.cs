using System;
using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>The effect of every <see cref="ItemEffectKind"/>; a new kind registers its effect here.</summary>
    internal static class ItemEffects
    {
        private static readonly Dictionary<ItemEffectKind, IItemEffect> Effects = new Dictionary<ItemEffectKind, IItemEffect>
        {
            { ItemEffectKind.Heal, new HealEffect() },
            { ItemEffectKind.Armor, new ArmorEffect() },
            { ItemEffectKind.Bomb, new BombEffect() },
            { ItemEffectKind.Shuffle, new ShuffleEffect() },
        };

        internal static IItemEffect For(ItemEffectKind kind)
        {
            return Effects.TryGetValue(kind, out IItemEffect effect)
                ? effect
                : throw new InvalidOperationException($"No item effect for {kind}.");
        }

        private sealed class HealEffect : IItemEffect
        {
            public bool NeedsTarget => false;

            public bool CanUse(Run run, float amount)
            {
                return !run.IsOver && run.Battle.HeroHealth < run.Battle.HeroMaxHealth;
            }

            public RunStep Use(Run run, float amount, int radius, Cell target)
            {
                var events = new List<BattleEvent>();
                if (CanUse(run, amount))
                {
                    run.Battle.Heal(amount, -1, events);
                }

                return new RunStep(null, events);
            }
        }

        private sealed class ArmorEffect : IItemEffect
        {
            public bool NeedsTarget => false;

            public bool CanUse(Run run, float amount)
            {
                return !run.IsOver && run.Battle.Armor < run.Battle.ArmorCap;
            }

            public RunStep Use(Run run, float amount, int radius, Cell target)
            {
                var events = new List<BattleEvent>();
                if (CanUse(run, amount))
                {
                    run.Battle.AddArmor(amount, -1, events);
                }

                return new RunStep(null, events);
            }
        }

        private sealed class BombEffect : IItemEffect
        {
            public bool NeedsTarget => true;

            public bool CanUse(Run run, float amount)
            {
                return !run.IsOver;
            }

            public RunStep Use(Run run, float amount, int radius, Cell target)
            {
                return run.Blast(target, radius);
            }
        }

        private sealed class ShuffleEffect : IItemEffect
        {
            public bool NeedsTarget => false;

            public bool CanUse(Run run, float amount)
            {
                return !run.IsOver;
            }

            public RunStep Use(Run run, float amount, int radius, Cell target)
            {
                return run.Shuffle();
            }
        }
    }
}

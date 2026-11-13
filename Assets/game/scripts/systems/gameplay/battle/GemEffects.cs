using System;
using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>The effect of every <see cref="GemEffectKind"/>; a new kind registers its effect here.</summary>
    internal static class GemEffects
    {
        private static readonly Dictionary<GemEffectKind, IGemEffect> Effects = new Dictionary<GemEffectKind, IGemEffect>
        {
            { GemEffectKind.Attack, new AttackEffect() },
            { GemEffectKind.Heal, new HealEffect() },
            { GemEffectKind.Defense, new DefenseEffect() },
            { GemEffectKind.Magic, new MagicEffect() },
        };

        internal static IGemEffect For(GemEffectKind kind)
        {
            return Effects.TryGetValue(kind, out IGemEffect effect)
                ? effect
                : throw new InvalidOperationException($"No gem effect for {kind}.");
        }

        private sealed class AttackEffect : IGemEffect
        {
            public void Apply(Battle battle, int gem, float amount, List<BattleEvent> events)
            {
                battle.Strike(amount, gem, events);
            }
        }

        private sealed class HealEffect : IGemEffect
        {
            public void Apply(Battle battle, int gem, float amount, List<BattleEvent> events)
            {
                battle.Heal(amount, gem, events);
            }
        }

        private sealed class DefenseEffect : IGemEffect
        {
            public void Apply(Battle battle, int gem, float amount, List<BattleEvent> events)
            {
                battle.AddArmor(amount, gem, events);
            }
        }

        private sealed class MagicEffect : IGemEffect
        {
            public void Apply(Battle battle, int gem, float amount, List<BattleEvent> events)
            {
                battle.ChargeMagic(amount, gem, events);
            }
        }
    }
}

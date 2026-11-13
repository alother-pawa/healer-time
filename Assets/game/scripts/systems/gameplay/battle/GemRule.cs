namespace Game.Systems.Gameplay
{
    /// <summary>The battle meaning of one gem kind: its effect and how much one gem is worth.</summary>
    internal readonly struct GemRule
    {
        internal GemRule(GemEffectKind effect, float power)
        {
            Effect = effect;
            Power = power;
        }

        internal GemEffectKind Effect { get; }

        /// <summary>Amount one plain gem gives (damage, health, armor or magic).</summary>
        internal float Power { get; }
    }
}

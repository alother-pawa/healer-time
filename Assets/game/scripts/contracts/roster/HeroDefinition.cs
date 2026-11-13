using UnityEngine;

namespace Game.Contracts.Roster
{
    /// <summary>A hero: fights on the battlefield on its own; matched gems make it stronger.</summary>
    [CreateAssetMenu(menuName = "Game/Roster/Hero", fileName = "Hero")]
    public sealed class HeroDefinition : CharacterDefinition
    {
        [Min(1f)] [SerializeField] private float _maxHealth = 120f;
        [Tooltip("Damage of the automatic attack.")]
        [Min(0f)] [SerializeField] private float _attackDamage = 8f;
        [Tooltip("Seconds between automatic attacks.")]
        [Min(0.1f)] [SerializeField] private float _attackInterval = 1.2f;
        [Tooltip("Multiplier of the damage of Attack gems.")]
        [Min(0f)] [SerializeField] private float _strikeMultiplier = 1f;

        public float MaxHealth => _maxHealth;

        public float AttackDamage => _attackDamage;

        public float AttackInterval => _attackInterval;

        public float StrikeMultiplier => _strikeMultiplier;
    }
}

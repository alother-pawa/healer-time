using UnityEngine;

namespace Game.Contracts.Roster
{
    /// <summary>A healer: stands behind the board; Magic gems fill its bar and a full bar casts its skill.</summary>
    [CreateAssetMenu(menuName = "Game/Roster/Healer", fileName = "Healer")]
    public sealed class HealerDefinition : CharacterDefinition
    {
        [Tooltip("Magic needed for the skill.")]
        [Min(1f)] [SerializeField] private float _magicCapacity = 100f;
        [Tooltip("Damage of the skill on the monster (grows with later monsters).")]
        [Min(0f)] [SerializeField] private float _skillDamage = 45f;
        [Tooltip("Health the skill gives the hero.")]
        [Min(0f)] [SerializeField] private float _skillHeal = 10f;

        public float MagicCapacity => _magicCapacity;

        public float SkillDamage => _skillDamage;

        public float SkillHeal => _skillHeal;
    }
}

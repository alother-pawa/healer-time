using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// One gem kind of the board: its picture, what matching it does (<see cref="GemEffectKind"/>), how much one gem is
    /// worth and how often it drops. A new gem kind is a new asset in the balance config's gem list.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Gameplay/Gem", fileName = "Gem")]
    internal sealed class GemDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;
        [Tooltip("Caption over the board while this gem's effect plays ({0} = the amount).")]
        [SerializeField] private string _showcaseKey;
        [SerializeField] private Sprite _sprite;
        [Tooltip("Colour of the gem's effects (bursts, numbers).")]
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private GemEffectKind _effect;
        [Tooltip("Amount one plain gem gives: damage, health, armor or magic.")]
        [Min(0f)] [SerializeField] private float _power = 5f;
        [Tooltip("Relative chance of dropping this gem.")]
        [Min(0f)] [SerializeField] private float _spawnWeight = 1f;

        internal string Id => _id;

        internal string NameKey => _nameKey;

        internal string ShowcaseKey => _showcaseKey;

        internal Sprite Sprite => _sprite;

        internal Color Color => _color;

        internal GemEffectKind Effect => _effect;

        internal float Power => _power;

        internal float SpawnWeight => _spawnWeight;
    }
}

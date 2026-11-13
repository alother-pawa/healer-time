using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// One monster kind: its look (animator of a sheet, tint, scale) and base numbers before later monsters grow stronger.
    /// Bosses appear only at boss slots. A new monster kind is a new asset in the balance config's monster list.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Gameplay/Monster", fileName = "Monster")]
    internal sealed class MonsterDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;
        [SerializeField] private RuntimeAnimatorController _controller;
        [SerializeField] private Color _tint = Color.white;
        [Min(0.1f)] [SerializeField] private float _scale = 1f;
        [Min(1f)] [SerializeField] private float _health = 30f;
        [Min(0f)] [SerializeField] private float _damage = 6f;
        [Min(0.1f)] [SerializeField] private float _attackInterval = 2f;
        [Min(0)] [SerializeField] private int _coins = 3;
        [Min(0)] [SerializeField] private int _score = 100;
        [SerializeField] private bool _isBoss;
        [Tooltip("First monster index (0-based) this kind can appear at.")]
        [Min(0)] [SerializeField] private int _unlockAt;
        [Min(0f)] [SerializeField] private float _weight = 1f;

        internal string Id => _id;

        internal string NameKey => _nameKey;

        internal RuntimeAnimatorController Controller => _controller;

        internal Color Tint => _tint;

        internal float Scale => _scale;

        internal bool IsBoss => _isBoss;

        internal MonsterTemplate Template =>
            new MonsterTemplate(_health, _damage, _attackInterval, _coins, _score, _isBoss, _unlockAt, _weight);
    }
}

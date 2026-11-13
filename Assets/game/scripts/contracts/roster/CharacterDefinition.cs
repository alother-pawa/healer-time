using UnityEngine;

namespace Game.Contracts.Roster
{
    /// <summary>
    /// What every playable character shares: a stable id (saved as the selection), name and description keys, the animator
    /// of its sheet for the battlefield, idle frames for menu previews, and a tint and scale over the sheet (so one sheet
    /// can serve several characters). Subclasses add the battle numbers.
    /// </summary>
    public abstract class CharacterDefinition : ScriptableObject
    {
        [Tooltip("Stable id, saved as the player's selection.")]
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;
        [SerializeField] private string _descriptionKey;
        [Tooltip("Animator of the character's sheet (states named after the sheet's tags: idle, attack, hurt...).")]
        [SerializeField] private RuntimeAnimatorController _controller;
        [Tooltip("Idle frames shown in menus, played in a loop.")]
        [SerializeField] private Sprite[] _previewFrames = new Sprite[0];
        [Min(0.02f)] [SerializeField] private float _previewFrameSeconds = 0.12f;
        [SerializeField] private Color _tint = Color.white;
        [Min(0.1f)] [SerializeField] private float _scale = 1f;

        public string Id => _id;

        public string NameKey => _nameKey;

        public string DescriptionKey => _descriptionKey;

        public RuntimeAnimatorController Controller => _controller;

        public Sprite[] PreviewFrames => _previewFrames;

        public float PreviewFrameSeconds => _previewFrameSeconds;

        public Color Tint => _tint;

        public float Scale => _scale;
    }
}

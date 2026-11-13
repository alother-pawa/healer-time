using DG.Tweening;
using UnityEngine;

namespace Game.Foundation.UI
{
    /// <summary>Timing of a toast: slide/fade in, stay readable, slide/fade out.</summary>
    [CreateAssetMenu(menuName = "Game/UI/Toast Animation", fileName = "ToastAnimation")]
    public sealed class ToastAnimationConfig : ScriptableObject
    {
        [Min(0f)] [SerializeField] private float _inSeconds = 0.2f;
        [Tooltip("How long the message stays fully visible; applies with animations disabled too.")]
        [Min(0f)] [SerializeField] private float _holdSeconds = 1.6f;
        [Min(0f)] [SerializeField] private float _outSeconds = 0.2f;
        [Tooltip("Vertical offset the toast slides in from, in canvas units.")]
        [SerializeField] private float _slideOffset = 60f;
        [SerializeField] private Ease _ease = Ease.OutCubic;

        /// <summary>Duration of the entrance, unscaled seconds.</summary>
        public float InSeconds => _inSeconds;

        /// <summary>Time fully visible, unscaled seconds.</summary>
        public float HoldSeconds => _holdSeconds;

        /// <summary>Duration of the exit, unscaled seconds.</summary>
        public float OutSeconds => _outSeconds;

        /// <summary>Vertical slide distance, canvas units.</summary>
        public float SlideOffset => _slideOffset;

        /// <summary>Ease of entrance and exit.</summary>
        public Ease Ease => _ease;
    }
}

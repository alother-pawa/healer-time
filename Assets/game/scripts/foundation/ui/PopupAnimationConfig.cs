using DG.Tweening;
using UnityEngine;

namespace Game.Foundation.UI
{
    /// <summary>Timing and shape of a popup's show/hide animation; shared by every popup that references it.</summary>
    [CreateAssetMenu(menuName = "Game/UI/Popup Animation", fileName = "PopupAnimation")]
    public sealed class PopupAnimationConfig : ScriptableObject
    {
        [Min(0f)] [SerializeField] private float _showSeconds = 0.2f;
        [Min(0f)] [SerializeField] private float _hideSeconds = 0.15f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InQuad;
        [Tooltip("Panel scale the show animation starts from.")]
        [SerializeField] private float _hiddenScale = 0.9f;

        /// <summary>Duration of the show animation, unscaled seconds.</summary>
        public float ShowSeconds => _showSeconds;

        /// <summary>Duration of the hide animation, unscaled seconds.</summary>
        public float HideSeconds => _hideSeconds;

        /// <summary>Ease of the show animation.</summary>
        public Ease ShowEase => _showEase;

        /// <summary>Ease of the hide animation.</summary>
        public Ease HideEase => _hideEase;

        /// <summary>Panel scale while hidden (start of show, end of hide).</summary>
        public float HiddenScale => _hiddenScale;
    }
}

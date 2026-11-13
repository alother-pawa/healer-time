using DG.Tweening;
using Game.Contracts.Presentation;
using Game.Foundation.Tweening;
using Paowa.Core.Foundation.Services;
using Paowa.UI;
using UnityEngine;

namespace Game.Foundation.UI
{
    /// <summary>
    /// Shared look and motion of every popup, kept on the root of the base popup prefab so variants inherit it: fades the
    /// root group, scales the panel, and only accepts input while shown (pointer and navigation: a hiding popup's buttons
    /// are not interactable, so the focus service cannot select them). Core plays it after <c>OnShow</c> and before
    /// deactivating on hide. The interactive state is written before the tween starts; with animations disabled the popup
    /// appears and disappears in the same frame.
    /// REFERENCES: <c>_canvasGroup</c> is the full-screen root group (backdrop included); <c>_panel</c> is the frame that
    /// scales; <c>_animation</c> is the shared timing asset.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PopupFrame : MonoBehaviour, IUIViewTransition
    {
        private const float VisibleAlpha = 1f;
        private const float HiddenAlpha = 0f;
        private const float VisibleScale = 1f;

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private PopupAnimationConfig _animation;

        private IAnimationSettings _animationSettings;
        private Sequence _sequence;
        private bool _isVisible;

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _animationSettings);
        }

        public Awaitable PlayShowAsync()
        {
            _sequence.KillIfActive();
            SetInteractive(true);
            if (!_isVisible)
            {
                _canvasGroup.alpha = HiddenAlpha;
                _panel.localScale = Vector3.one * _animation.HiddenScale;
            }

            _isVisible = true;
            return Play(VisibleAlpha, VisibleScale, _animation.ShowSeconds, _animation.ShowEase);
        }

        public Awaitable PlayHideAsync()
        {
            _sequence.KillIfActive();

            // No double presses while the hide animation plays.
            SetInteractive(false);
            _isVisible = false;
            return Play(HiddenAlpha, _animation.HiddenScale, _animation.HideSeconds, _animation.HideEase);
        }

        /// <summary>
        /// Jumps the running tween to its end pose (shown or hidden) and completes its awaitable; called by the UI manager
        /// when transitions are turned off mid-animation.
        /// </summary>
        public void Complete()
        {
            if (_sequence != null && _sequence.IsActive())
            {
                _sequence.Complete(true);
            }
        }

        private Awaitable Play(float alpha, float scale, float seconds, Ease ease)
        {
            Sequence sequence = DOTween.Sequence()
                .Join(_canvasGroup.DOFade(alpha, seconds))
                .Join(_panel.DOScale(scale, seconds).SetEase(ease))
                .SetUpdate(true)
                .SetLink(gameObject);
            sequence.OnKill(() =>
            {
                if (_sequence == sequence)
                {
                    _sequence = null;
                }
            });
            _sequence = sequence;
            return sequence.Gate(_animationSettings).AsAwaitable();
        }

        private void SetInteractive(bool interactive)
        {
            _canvasGroup.interactable = interactive;
            _canvasGroup.blocksRaycasts = interactive;
        }
    }
}

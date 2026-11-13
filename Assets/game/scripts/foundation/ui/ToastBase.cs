using DG.Tweening;
using Game.Contracts.Presentation;
using Game.Foundation.Tweening;
using Paowa.Core.Foundation.Services;
using TMPro;
using UnityEngine;

namespace Game.Foundation.UI
{
    /// <summary>
    /// Base of toast prefabs: shows one message, keeps it readable for the configured time, then hides. The hold time is
    /// reading time rather than animation, so it also applies with animations disabled. Variants restyle the prefab or
    /// override <see cref="ApplyMessage"/>.
    /// REFERENCES: <c>_canvasGroup</c> fades the whole toast; <c>_body</c> is the rect that slides; <c>_label</c> shows the
    /// message; <c>_animation</c> is the shared timing asset.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastBase : MonoBehaviour
    {
        private const float VisibleAlpha = 1f;
        private const float HiddenAlpha = 0f;

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _body;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private ToastAnimationConfig _animation;

        private IAnimationSettings _animationSettings;
        private Vector2 _restPosition;

        protected virtual void Awake()
        {
            CoreServiceLocator.TryGet(out _animationSettings);
            _restPosition = _body.anchoredPosition;
            _canvasGroup.alpha = HiddenAlpha;
            _canvasGroup.blocksRaycasts = false;
        }

        /// <summary>Plays the whole toast for <paramref name="message"/>; completes once it is hidden again.</summary>
        public async Awaitable PlayAsync(string message)
        {
            ApplyMessage(message);
            Vector2 hiddenPosition = _restPosition + Vector2.down * _animation.SlideOffset;

            _body.anchoredPosition = hiddenPosition;
            await Fade(VisibleAlpha, _restPosition, _animation.InSeconds).AsAwaitable();
            await WaitUnscaledAsync(_animation.HoldSeconds);
            await Fade(HiddenAlpha, hiddenPosition, _animation.OutSeconds).AsAwaitable();
        }

        /// <summary>Writes the message into the toast.</summary>
        protected virtual void ApplyMessage(string message)
        {
            _label.text = message;
        }

        private Tween Fade(float alpha, Vector2 position, float seconds)
        {
            return DOTween.Sequence()
                .Join(_canvasGroup.DOFade(alpha, seconds))
                .Join(_body.DOAnchorPos(position, seconds).SetEase(_animation.Ease))
                .SetUpdate(true)
                .SetLink(gameObject)
                .Gate(_animationSettings);
        }

        private async Awaitable WaitUnscaledAsync(float seconds)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end)
            {
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
        }
    }
}

using Game.Contracts.Presentation;
using Paowa.Core.Foundation.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Foundation.UI
{
    /// <summary>
    /// Plays a loop of sprites on a UI <see cref="Image"/> (a character's idle frames in a menu). With animations off it
    /// shows the first frame. <see cref="Play"/> sets the frames, the time per frame and a tint.
    /// REFERENCES: <c>_image</c> is the image drawn.
    /// </summary>
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private Image _image;

        private Sprite[] _frames = new Sprite[0];
        private float _frameSeconds = 0.12f;
        private float _time;
        private IAnimationSettings _animations;

        public void Play(Sprite[] frames, float frameSeconds, Color tint)
        {
            _frames = frames ?? new Sprite[0];
            _frameSeconds = Mathf.Max(0.02f, frameSeconds);
            _time = 0f;
            _image.color = tint;
            _image.enabled = _frames.Length > 0;
            Show(0);
        }

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _animations);
        }

        private void Update()
        {
            if (_frames.Length < 2)
            {
                return;
            }

            if (_animations != null && !_animations.Enabled)
            {
                Show(0);
                return;
            }

            _time += Time.unscaledDeltaTime;
            Show((int)(_time / _frameSeconds) % _frames.Length);
        }

        private void Show(int index)
        {
            if (index < _frames.Length && _image.sprite != _frames[index])
            {
                _image.sprite = _frames[index];
            }
        }
    }
}

using Game.Contracts.Presentation;
using Paowa.Core.Foundation.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Foundation.UI
{
    /// <summary>
    /// Plays a loop of sprites on a UI <see cref="Image"/> (a character's idle frames in a menu). The image takes the size the
    /// sprite has in the world (its pixels per unit, at <c>_pixelsPerUnit</c> reference pixels per world unit) times
    /// <c>_sizeScale</c> and the character's own scale, and its pivot is the sprite's, so the rect's position is where the
    /// character's feet stand. With animations off it shows the first frame.
    /// REFERENCES: <c>_image</c> is the image drawn.
    /// </summary>
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [Tooltip("Reference-resolution pixels per world unit (the game camera shows 128).")]
        [SerializeField] private float _pixelsPerUnit = 128f;
        [Tooltip("Extra size over the in-game size (a small card preview uses less than 1).")]
        [SerializeField] private float _sizeScale = 1f;

        private Sprite[] _frames = new Sprite[0];
        private float _frameSeconds = 0.12f;
        private float _scale = 1f;
        private float _time;
        private IAnimationSettings _animations;

        public void Play(Sprite[] frames, float frameSeconds, Color tint, float scale = 1f)
        {
            _frames = frames ?? new Sprite[0];
            _frameSeconds = Mathf.Max(0.02f, frameSeconds);
            _scale = scale;
            _time = 0f;
            _image.color = tint;
            _image.enabled = _frames.Length > 0;
            if (_frames.Length > 0)
            {
                Sprite first = _frames[0];
                RectTransform rect = _image.rectTransform;
                rect.pivot = new Vector2(first.pivot.x / first.rect.width, first.pivot.y / first.rect.height);
                rect.sizeDelta = first.rect.size * (_pixelsPerUnit / first.pixelsPerUnit) * _sizeScale * _scale;
            }

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

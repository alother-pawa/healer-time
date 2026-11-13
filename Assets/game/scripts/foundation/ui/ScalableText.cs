using System;
using Game.Contracts.Settings;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using TMPro;
using UnityEngine;

namespace Game.Foundation.UI
{
    /// <summary>
    /// Sizes a TextMeshPro text by the text size setting (<see cref="IGameSettings.TextScale"/>): the size saved in the
    /// prefab is the base, the shown size is base × multiplier (<see cref="TextScaling"/>), auto-size limits included. The
    /// size is applied from the current setting on enable and again on every <see cref="TextScaleChangedEvent"/>, so a
    /// text enabled after a change is right too. The builders add it to every text they create.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class ScalableText : MonoBehaviour
    {
        private TMP_Text _text;
        private float _baseSize;
        private float _baseMinSize;
        private float _baseMaxSize;
        private IDisposable _binding;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _baseSize = _text.fontSize;
            _baseMinSize = _text.fontSizeMin;
            _baseMaxSize = _text.fontSizeMax;
        }

        private void OnEnable()
        {
            if (CoreServiceLocator.TryGet(out IEventBus events))
            {
                _binding = events.Subscribe<TextScaleChangedEvent>(evt => Apply(evt.Scale));
            }

            Apply(CoreServiceLocator.TryGet(out IGameSettings settings) ? settings.TextScale : 1f);
        }

        private void OnDisable()
        {
            _binding?.Dispose();
            _binding = null;
        }

        private void Apply(float scale)
        {
            _text.fontSize = TextScaling.Size(_baseSize, scale);
            if (_text.enableAutoSizing)
            {
                _text.fontSizeMin = TextScaling.Size(_baseMinSize, scale);
                _text.fontSizeMax = TextScaling.Size(_baseMaxSize, scale);
            }
        }
    }
}

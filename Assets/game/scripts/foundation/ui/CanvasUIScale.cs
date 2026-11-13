using System;
using Game.Contracts.Settings;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Foundation.UI
{
    /// <summary>
    /// Applies the player's UI scale (<see cref="IGameSettings.UIScale"/>) to the <see cref="CanvasScaler"/> on this canvas:
    /// the reference resolution it had at load is divided by the scale, so 1.2 draws every element 20% larger at any window
    /// size and aspect. Reads the committed value each time the canvas is enabled and follows
    /// <see cref="UIScaleChangedEvent"/>, so a canvas loaded later starts at the right size. Without
    /// <see cref="IGameSettings"/> the canvas keeps its own reference resolution.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class CanvasUIScale : MonoBehaviour
    {
        // Guards against a zero or absurd scale from a hand-edited save; the settings slider stays well inside it.
        private const float MinScale = 0.5f;
        private const float MaxScale = 2f;

        private CanvasScaler _scaler;
        private Vector2 _referenceResolution;
        private IDisposable _binding;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            _referenceResolution = _scaler.referenceResolution;
        }

        private void OnEnable()
        {
            if (CoreServiceLocator.TryGet(out IEventBus events))
            {
                _binding = events.Subscribe<UIScaleChangedEvent>(evt => Apply(evt.Scale));
            }

            Apply(CoreServiceLocator.TryGet(out IGameSettings settings) ? settings.UIScale : 1f);
        }

        private void OnDisable()
        {
            _binding?.Dispose();
            _binding = null;
        }

        private void Apply(float scale)
        {
            _scaler.referenceResolution = _referenceResolution / Mathf.Clamp(scale, MinScale, MaxScale);
        }
    }
}

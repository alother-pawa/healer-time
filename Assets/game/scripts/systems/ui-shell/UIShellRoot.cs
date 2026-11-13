using System;
using Paowa.Core.Foundation.Services;
using Paowa.UI;
using UnityEngine;

namespace Game.Systems.UIShell
{
    /// <summary>
    /// Hands the UI scene's canvas layers to Core's <see cref="IUIManager"/>, so every popup and HUD view of the game is
    /// parented here instead of under Core's own canvas.
    /// REFERENCES: <c>_hudLayer</c> holds HUD views; <c>_popupLayer</c> sits above it and holds popups.
    /// </summary>
    internal sealed class UIShellRoot : MonoBehaviour
    {
        [SerializeField] private RectTransform _hudLayer;
        [SerializeField] private RectTransform _popupLayer;

        private IDisposable _attachment;

        private void Awake()
        {
            _attachment = CoreServiceLocator.Get<IUIManager>().AttachRoot(_hudLayer, _popupLayer);
        }

        private void OnDestroy()
        {
            _attachment?.Dispose();
        }
    }
}

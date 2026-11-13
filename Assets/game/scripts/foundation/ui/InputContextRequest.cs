using System;
using Game.Contracts.Interaction;
using Paowa.Core.Foundation.Services;
using Paowa.Input;
using UnityEngine;

namespace Game.Foundation.UI
{
    /// <summary>
    /// Keeps an input context enabled while this object is active (a menu screen or a popup that switches tabs with
    /// <c>Menu/TabPrevious</c>/<c>Menu/TabNext</c>), through a counted <see cref="IInputService.RequestContext"/> handle, so a
    /// popup closing over a menu does not disable the menu's actions. Without the input package's module nothing happens.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputContextRequest : MonoBehaviour
    {
        [Tooltip("Input context (action map) to enable, e.g. Menu. Never the UI map.")]
        [SerializeField] private string _context = InputContextIds.Menu;

        private IDisposable _request;

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(_context) && CoreServiceLocator.TryGet(out IInputService input))
            {
                _request = input.RequestContext(_context);
            }
        }

        private void OnDisable()
        {
            _request?.Dispose();
            _request = null;
        }
    }
}

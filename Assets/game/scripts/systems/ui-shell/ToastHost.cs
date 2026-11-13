using Game.Contracts.Presentation;
using Game.Foundation.UI;
using Paowa.Core.Foundation.Services;
using UnityEngine;

namespace Game.Systems.UIShell
{
    /// <summary>
    /// Shows the game's toasts in the UI scene.
    /// REFERENCES: <c>_toast</c> is the toast instance (a <see cref="ToastBase"/> prefab or variant) placed in the scene.
    /// </summary>
    internal sealed class ToastHost : MonoBehaviour
    {
        [SerializeField] private ToastBase _toast;

        private ToastService _service;

        private void Start()
        {
            // Start rather than Awake: the toast's own Awake must have run before the first message plays.
            if (CoreServiceLocator.TryGet(out IToastService service) && service is ToastService toasts)
            {
                _service = toasts;
                _service.Attach(this);
            }
        }

        private void OnDestroy()
        {
            _service?.Detach(this);
        }

        internal Awaitable PlayAsync(string message)
        {
            return _toast.PlayAsync(message);
        }
    }
}

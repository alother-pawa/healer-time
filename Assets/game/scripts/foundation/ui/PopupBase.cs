using System;
using Paowa.Core.Flow.Timing;
using Paowa.Core.Foundation.Services;
using Paowa.UI;
using UnityEngine;

namespace Game.Foundation.UI
{
    /// <summary>
    /// Behaviour of one popup, on the root of a variant of the base popup prefab next to its <see cref="PopupFrame"/> and
    /// its <c>FocusScope</c>. Registered with <see cref="IUIManager"/> under <see cref="ViewId"/>; subclasses fill content
    /// in <see cref="OnShown"/> (state first: everything visible is written there, before any animation) and release it in
    /// <see cref="OnHidden"/>, which runs before the hide animation.
    /// </summary>
    public abstract class PopupBase : MonoBehaviour, IUIView
    {
        [Tooltip("Id the popup is registered and shown under; unique per popup prefab.")]
        [SerializeField] private string _viewId;
        [Tooltip("Pause game time while the popup is open (settings or pause during gameplay).")]
        [SerializeField] private bool _pausesGame;

        private IUIManager _ui;
        private ITimeScaleService _timeScale;
        private IDisposable _pause;

        /// <summary>Id under which the popup is registered with <see cref="IUIManager"/>.</summary>
        public UIViewId ViewId => new UIViewId(_viewId);

        /// <summary>Core UI manager, resolved in <c>Awake</c>.</summary>
        protected IUIManager UI => _ui;

        protected virtual void Awake()
        {
            _ui = CoreServiceLocator.Get<IUIManager>();
            CoreServiceLocator.TryGet(out _timeScale);
        }

        protected virtual void OnDestroy()
        {
            ReleasePause();
        }

        void IUIView.OnShow(object payload)
        {
            if (_pausesGame && _timeScale != null && _pause == null)
            {
                _pause = _timeScale.Pause();
            }

            OnShown(payload);
        }

        void IUIView.OnHide()
        {
            ReleasePause();
            OnHidden();
        }

        /// <summary>Fills the popup from <paramref name="payload"/>; the popup is already active.</summary>
        protected abstract void OnShown(object payload);

        /// <summary>Releases what <see cref="OnShown"/> acquired (subscriptions, handles).</summary>
        protected virtual void OnHidden()
        {
        }

        /// <summary>Hides this popup.</summary>
        protected void Close()
        {
            _ui.HidePopup(ViewId);
        }

        private void ReleasePause()
        {
            _pause?.Dispose();
            _pause = null;
        }
    }
}

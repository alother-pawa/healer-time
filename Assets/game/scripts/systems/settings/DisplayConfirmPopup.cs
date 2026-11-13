using Game.Foundation.Localization;
using Game.Foundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Settings
{
    /// <summary>
    /// "Keep these display settings? (N)": Keep confirms, Revert (the first selected control, the safe choice) or
    /// Cancel/Back restores the previous mode, and the countdown reverts on its own. Everything shown is read from the
    /// <see cref="DisplayConfirmPresenter"/> payload every frame (the countdown is unscaled time inside the display service),
    /// so the popup is right with animations on or off and follows a newer change at once.
    /// REFERENCES: <c>_message</c> shows <c>_messageKey</c> formatted with the seconds left; <c>_keepButton</c> and
    /// <c>_revertButton</c> answer.
    /// </summary>
    internal sealed class DisplayConfirmPopup : PopupBase
    {
        [SerializeField] private LocalizedLabel _message;
        [SerializeField] private string _messageKey;
        [SerializeField] private Button _keepButton;
        [SerializeField] private Button _revertButton;

        private DisplayConfirmPresenter _presenter;
        private int _shownSeconds = -1;

        protected override void Awake()
        {
            base.Awake();
            _keepButton.onClick.AddListener(() => _presenter?.Keep());
            _revertButton.onClick.AddListener(() => _presenter?.Revert());
        }

        protected override void OnShown(object payload)
        {
            _presenter = payload as DisplayConfirmPresenter;
            if (_presenter == null)
            {
                Debug.LogError($"[Game] {name} opened without a display change; closing it.", this);
                Close();
                return;
            }

            _shownSeconds = -1;
            ShowCountdown();
        }

        protected override void OnHidden()
        {
            DisplayConfirmPresenter presenter = _presenter;
            _presenter = null;
            presenter?.OnViewClosed();
        }

        private void Update()
        {
            if (_presenter != null)
            {
                ShowCountdown();
            }
        }

        private void ShowCountdown()
        {
            int seconds = _presenter.SecondsLeft;
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                _message.SetFormat(_messageKey, seconds);
            }
        }
    }
}

using Game.Contracts.Presentation;
using Game.Foundation.Localization;
using Game.Foundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.UIShell
{
    /// <summary>
    /// The confirmation popup shown by <see cref="ConfirmDialogService"/>: title, message, confirm and cancel buttons, all
    /// written from the session's request when it opens (the labels follow language changes while open). The root's
    /// <c>FocusScope</c> starts on the cancel button, the safe choice; a notice without a cancel key hides that button,
    /// centers the confirm button and focus falls to it. Closing the popup in any other way (Cancel/Back, every popup
    /// hidden) answers false; the service delivers every answer on a later frame, and Core queues a popup change requested
    /// from inside another, so nothing re-enters Core's show or hide.
    /// REFERENCES: <c>_title</c> is the base popup's title label; <c>_message</c>, <c>_confirmLabel</c> and
    /// <c>_cancelLabel</c> show the request's keys; <c>_confirmButton</c>/<c>_cancelButton</c> answer true/false.
    /// </summary>
    internal sealed class ConfirmDialogPopup : PopupBase
    {
        [SerializeField] private LocalizedLabel _title;
        [SerializeField] private LocalizedLabel _message;
        [SerializeField] private LocalizedLabel _confirmLabel;
        [SerializeField] private LocalizedLabel _cancelLabel;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private ConfirmDialogSession _session;
        private Vector2 _confirmPosition;

        protected override void Awake()
        {
            base.Awake();
            _confirmPosition = ((RectTransform)_confirmButton.transform).anchoredPosition;
            _confirmButton.onClick.AddListener(() => Answer(true));
            _cancelButton.onClick.AddListener(() => Answer(false));
        }

        protected override void OnShown(object payload)
        {
            _session = payload as ConfirmDialogSession;
            if (_session == null)
            {
                Debug.LogError($"[Game] {name} opened without a confirmation session; closing it.", this);
                Close();
                return;
            }

            ConfirmRequest request = _session.Request;
            _title.SetKey(request.TitleKey);
            if (request.MessageArguments != null)
            {
                _message.SetFormat(request.MessageKey, request.MessageArguments);
            }
            else
            {
                _message.SetKey(request.MessageKey);
            }

            _confirmLabel.SetKey(request.ConfirmKey);
            _cancelButton.gameObject.SetActive(request.HasCancel);
            ((RectTransform)_confirmButton.transform).anchoredPosition =
                request.HasCancel ? _confirmPosition : new Vector2(0f, _confirmPosition.y);
            if (request.HasCancel)
            {
                _cancelLabel.SetKey(request.CancelKey);
            }
        }

        protected override void OnHidden()
        {
            Answer(false);
        }

        private void Answer(bool confirmed)
        {
            ConfirmDialogSession session = _session;
            _session = null;
            if (session != null)
            {
                session.Answer(confirmed);
            }
        }
    }
}

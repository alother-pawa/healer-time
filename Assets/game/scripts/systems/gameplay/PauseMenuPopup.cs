using System;
using Game.Foundation.UI;
using Paowa.Core.Foundation.Services;
using Paowa.Desktop;
using Paowa.Input;
using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The pause menu: Resume, Save, Settings, Quit to main menu, Quit to desktop. A view only: every button forwards to the
    /// <see cref="IPauseMenuHost"/> payload (the gameplay screen), which owns the pause and the flows. While shown it requests
    /// the Menu cursor policy over the gameplay one, and <c>_resumeAction</c> (<c>Menu/Pause</c>: Escape, Start) resumes
    /// while the menu's scope has owned focus since the previous frame (not while Settings or a dialog is on top, nor in the
    /// frame the Escape that closed one of them arrives); Cancel/Back closes it through the
    /// desktop popup handler. Hiding by any path reports to the host, so the pause is always released.
    /// REFERENCES: the five buttons; the root carries the <c>FocusScope</c> (Resume first) and an <c>InputContextRequest</c>
    /// for the Menu context.
    /// </summary>
    internal sealed class PauseMenuPopup : PopupBase
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitToMenuButton;
        [SerializeField] private Button _quitToDesktopButton;
        [SerializeField] private string _resumeAction = "Menu/Pause";

        private IPauseMenuHost _host;
        private IInputService _input;
        private IUIFocusService _focus;
        private ICursorService _cursor;
        private IDisposable _cursorRequest;
        private bool _resumeValid;
        private bool _ownedFocusLastFrame;

        protected override void Awake()
        {
            base.Awake();
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _focus);
            CoreServiceLocator.TryGet(out _cursor);
            _resumeValid = _input != null && _input.HasAction(_resumeAction);
            _resumeButton.onClick.AddListener(() => Forward(host => host.Resume()));
            _saveButton.onClick.AddListener(() => Forward(host => host.Save()));
            _settingsButton.onClick.AddListener(() => Forward(host => host.OpenSettings()));
            _quitToMenuButton.onClick.AddListener(() => Forward(host => host.QuitToMenu()));
            _quitToDesktopButton.onClick.AddListener(() => Forward(host => host.QuitToDesktop()));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _cursorRequest?.Dispose();
            _cursorRequest = null;
        }

        protected override void OnShown(object payload)
        {
            _host = payload as IPauseMenuHost;
            if (_host == null)
            {
                Debug.LogError($"[Game] {name} opened without a gameplay screen; closing it.", this);
                Close();
                return;
            }

            _ownedFocusLastFrame = false;
            if (_cursor != null && _cursorRequest == null)
            {
                _cursorRequest = _cursor.Request(CursorPolicy.Menu);
            }
        }

        protected override void OnHidden()
        {
            _cursorRequest?.Dispose();
            _cursorRequest = null;
            IPauseMenuHost host = _host;
            _host = null;
            if (host != null)
            {
                host.OnPauseMenuHidden();
            }
        }

        private void Update()
        {
            // Focus must have been ours since the previous frame: the Escape that closes Settings or a dialog on top (UI/Cancel)
            // is also Menu/Pause, and the closed popup's scope is gone within the same frame.
            bool owns = OwnsFocus();
            if (_host != null && _resumeValid && owns && _ownedFocusLastFrame && _input.WasPressedThisFrame(_resumeAction))
            {
                _host.OnResumePressed();
            }

            _ownedFocusLastFrame = owns;
        }

        private void Forward(Action<IPauseMenuHost> action)
        {
            if (_host != null)
            {
                action(_host);
            }
        }

        private bool OwnsFocus()
        {
            if (_focus == null)
            {
                return true;
            }

            FocusScope top = _focus.TopScope;
            return top != null && top.transform.IsChildOf(transform);
        }
    }
}

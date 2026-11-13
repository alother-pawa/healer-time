using System;
using System.Collections.Generic;
using Game.Contracts.Interaction;
using Game.Contracts.Navigation;
using Game.Contracts.Presentation;
using Paowa.Core.Foundation.Lifecycle;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Input;
using Paowa.Pause;
using Paowa.UI;
using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The gameplay scene host: while enabled it keeps the Gameplay input context on, allows automatic pauses and keeps uGUI
    /// navigation events off (the board is played with the pointer, no focus scope is active while playing).
    /// Pause (<see cref="PauseController"/>): <c>_pauseAction</c> (Escape, Start) or a focus/controller-lost pause opens the
    /// pause menu popup, which holds the pause; navigation events turn on while it is open. The popup forwards its buttons
    /// here (<see cref="IPauseMenuHost"/>): Resume, Settings (<see cref="ISettingsMenu"/> over the pause menu), Quit to main
    /// menu / Quit to desktop (asked through <see cref="IConfirmDialog"/>; a run is never saved, so the question says the
    /// run ends).
    /// REFERENCES: <c>_config</c> is the gameplay config (pause popup, menu scene); the <c>*Key</c> fields are the texts of the
    /// questions.
    /// </summary>
    internal sealed class GameplayScreen : MonoBehaviour, IPauseMenuHost
    {
        [SerializeField] private GameplayConfig _config;
        [SerializeField] private string _pauseAction = "Gameplay/Pause";
        [SerializeField] private string _quitToMenuTitleKey;
        [SerializeField] private string _quitToMenuMessageKey;
        [SerializeField] private string _quitToMenuConfirmKey;
        [SerializeField] private string _quitToDesktopTitleKey;
        [SerializeField] private string _quitToDesktopMessageKey;
        [SerializeField] private string _quitToDesktopConfirmKey;
        [SerializeField] private string _cancelKey;

        private readonly List<IDisposable> _handles = new List<IDisposable>();
        private IInputService _input;
        private IPauseService _pause;
        private IGameNavigator _navigator;
        private IAppLifecycleService _lifecycle;
        private IConfirmDialog _confirm;
        private IEventBus _events;
        private PauseController _controller;
        private bool _pauseValid;
        private bool _leaving;
        private bool _asking;
        private bool _navigationOpened;

        /// <summary>True while the pause menu is open or the game is paused for any reason.</summary>
        internal bool IsPaused => _controller.IsMenuOpen || (_pause != null && _pause.IsPaused);

        /// <summary>True once the scene is being left; nothing new should start.</summary>
        internal bool IsLeaving => _leaving;

        public void Resume()
        {
            _controller.Resume();
        }

        public void OnResumePressed()
        {
            _controller.OnResumePressed();
        }

        public void OpenSettings()
        {
            if (CoreServiceLocator.TryGet(out ISettingsMenu settings))
            {
                settings.Open();
            }
            else
            {
                Debug.LogWarning("[Game] The settings module is not running; Settings does nothing.", this);
            }
        }

        public void QuitToMenu()
        {
            _ = AskThenAsync(new ConfirmRequest(_quitToMenuTitleKey, _quitToMenuMessageKey, _quitToMenuConfirmKey, _cancelKey),
                () => _ = LeaveToMenuAsync());
        }

        public void QuitToDesktop()
        {
            _ = AskThenAsync(new ConfirmRequest(_quitToDesktopTitleKey, _quitToDesktopMessageKey, _quitToDesktopConfirmKey, _cancelKey),
                QuitApplication);
        }

        public void OnPauseMenuHidden()
        {
            _controller.OnMenuHidden();
        }

        /// <summary>When true the pause menu does not open (the run is over and its popups own the screen).</summary>
        internal bool PauseBlocked { get; set; }

        /// <summary>The on-screen pause button: opens the pause menu like the pause key.</summary>
        internal void RequestPause()
        {
            if (_pauseValid && !PauseBlocked && !_leaving && (_navigator == null || !_navigator.IsNavigating))
            {
                _controller.OnPausePressed();
            }
        }

        /// <summary>Leaves for the main menu without asking (the result popup's Menu button).</summary>
        internal void GoToMenu()
        {
            _ = LeaveToMenuAsync();
        }

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _pause);
            CoreServiceLocator.TryGet(out _navigator);
            CoreServiceLocator.TryGet(out _lifecycle);
            CoreServiceLocator.TryGet(out _confirm);
            CoreServiceLocator.TryGet(out _events);
            CoreServiceLocator.TryGet(out IUIManager ui);

            _pauseValid = _input != null && _input.HasAction(_pauseAction) && ui != null && _config != null && _config.PausePopup != null;
            IPauseMenuView view = _pauseValid ? new PopupPauseMenuView(ui, _config.PausePopup.ViewId, this) : null;
            _controller = new PauseController(_pause, view, () => Time.frameCount);
            if (!_pauseValid)
            {
                Debug.LogWarning("[Game] The pause menu is unavailable (no pause action, UI manager or gameplay config); "
                    + "the game cannot pause.", this);
            }
        }

        private void OnEnable()
        {
            _leaving = false;
            _navigationOpened = false;
            if (_input != null)
            {
                _handles.Add(_input.RequestContext(InputContextIds.Gameplay));
            }

            if (_events != null)
            {
                _handles.Add(_events.Subscribe<PauseChangedEvent>(OnPauseChanged));
            }

            if (_pauseValid)
            {
                _controller.Enter();
            }
        }

        private void OnDisable()
        {
            _controller.Exit();
            foreach (IDisposable handle in _handles)
            {
                handle.Dispose();
            }

            _handles.Clear();

            // Leaving gameplay hands navigation back to the menus (the loading screen owns it while it covers).
            if (_navigator == null || !_navigator.IsNavigating)
            {
                SetNavigation(true);
            }
        }

        private void Update()
        {
            // While a scene switch runs (loading cover) an automatic pause request waits: the menu opens once it ends.
            if (!_leaving && (_navigator == null || !_navigator.IsNavigating))
            {
                if (!PauseBlocked)
                {
                    _controller.Tick();
                }

                if (_pauseValid && !PauseBlocked && _input.WasPressedThisFrame(_pauseAction))
                {
                    _controller.OnPausePressed();
                }
            }
        }

        // Navigation events: off while playing unless a popup owns focus (enforced every frame, also after the loading
        // screen restores its value), switched on once when the pause menu opens and then left to the menus.
        private void LateUpdate()
        {
            if (_leaving || (_navigator != null && _navigator.IsNavigating))
            {
                return;
            }

            if (_controller.IsMenuOpen || PopupOpen())
            {
                if (!_navigationOpened)
                {
                    _navigationOpened = true;
                    SetNavigation(true);
                }

                return;
            }

            _navigationOpened = false;
            SetNavigation(false);
        }

        private static bool PopupOpen()
        {
            return CoreServiceLocator.TryGet(out IUIFocusService focus) && focus.TopScope != null;
        }

        private void OnPauseChanged(PauseChangedEvent evt)
        {
            _controller.OnPauseChanged(evt.Reason, evt.ReasonActive);
        }

        // Without the dialog the button press itself is the answer.
        private async Awaitable AskThenAsync(ConfirmRequest question, Action then)
        {
            if (_asking || _leaving)
            {
                return;
            }

            _asking = true;
            try
            {
                if (_confirm == null || await _confirm.ConfirmAsync(question))
                {
                    then();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                _asking = false;
            }
        }

        // No pause or menu may outlive the scene; navigation goes back on before the loading screen records it. When the
        // switch does not start, gameplay carries on paused.
        private async Awaitable LeaveToMenuAsync()
        {
            if (_leaving)
            {
                return;
            }

            BeginLeaving();
            bool left = false;
            try
            {
                left = _navigator != null && _config != null && await _navigator.GoToAsync(_config.MenuScene);
                if (!left)
                {
                    Debug.LogWarning("[Game] The main menu could not open (another scene switch is running?); staying in gameplay.", this);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                if (!left && this != null && isActiveAndEnabled)
                {
                    _leaving = false;
                    if (_pauseValid)
                    {
                        _controller.Enter();
                        _controller.Reopen();
                    }
                }
            }
        }

        private void QuitApplication()
        {
            BeginLeaving();
            if (_lifecycle != null)
            {
                _lifecycle.Quit();
            }
            else
            {
                Application.Quit();
            }
        }

        private void BeginLeaving()
        {
            _leaving = true;
            _controller.Exit();
            SetNavigation(true);
        }

        private static void SetNavigation(bool enabled)
        {
            EventSystem current = EventSystem.current;
            if (current != null && current.sendNavigationEvents != enabled)
            {
                current.sendNavigationEvents = enabled;
            }
        }

        /// <summary>The pause menu popup on the UI manager's popup stack, with this screen as its payload.</summary>
        private sealed class PopupPauseMenuView : IPauseMenuView
        {
            private readonly IUIManager _ui;
            private readonly UIViewId _id;
            private readonly IPauseMenuHost _host;

            internal PopupPauseMenuView(IUIManager ui, UIViewId id, IPauseMenuHost host)
            {
                _ui = ui;
                _id = id;
                _host = host;
            }

            public bool Show()
            {
                _ui.ShowPopup(_id, _host);
                return _ui.IsPopupOpen(_id);
            }

            public void Hide()
            {
                if (_ui.IsPopupOpen(_id))
                {
                    _ui.HidePopup(_id);
                }
            }
        }
    }
}

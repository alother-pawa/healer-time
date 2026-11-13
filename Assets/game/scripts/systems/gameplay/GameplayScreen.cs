using System;
using System.Collections.Generic;
using Game.Contracts.Interaction;
using Game.Contracts.Navigation;
using Game.Contracts.Presentation;
using Game.Contracts.Save;
using Game.Contracts.Settings;
using Game.Foundation.Localization;
using Paowa.Core.Foundation.Lifecycle;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Desktop;
using Paowa.Gamepad;
using Paowa.Input;
using Paowa.Pause;
using Paowa.Save;
using Paowa.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The gameplay scene of the demo: while enabled it keeps the Gameplay input context on, requests the config's cursor
    /// policy (Locked by default), counts playtime, allows automatic pauses and keeps uGUI navigation events off, so WASD,
    /// Space, the stick and South never drive a UI selection while playing (no focus scope is active either). The tiles draw
    /// themselves from <see cref="IInputActivity"/>.
    /// Pause (<see cref="PauseController"/>): <c>_pauseAction</c> (Escape, Start) or a focus/controller-lost pause opens the
    /// pause menu popup, which holds the pause; navigation events turn on while it is open. The popup forwards its buttons
    /// here (<see cref="IPauseMenuHost"/>): Resume, Save (<see cref="ICheckpoint.Save"/>), Settings (<see cref="ISettingsMenu"/>
    /// over the pause menu), Quit to main menu / Quit to desktop (<see cref="GameplayExitFlow"/>: ask, save, then leave).
    /// Attack plays the config's rumble through the gamepad package's <see cref="IRumbleService"/> (which applies the
    /// vibration setting and intensity and stops on pause, focus loss, device switch and quit); leaving stops it.
    /// Sprint and Crouch follow the hold/toggle setting (<see cref="IGameSettings.ToggleSprint"/>/<c>ToggleCrouch</c>) through
    /// a <see cref="HoldToggle"/> each, ticked here every frame and shown by <c>_stance</c>; leaving resets them.
    /// Development builds also request the Debug context: <c>_checkpointAction</c> (F5, d-pad down) saves a checkpoint with a
    /// thumbnail, and the dev hints show.
    /// REFERENCES: <c>_config</c> is the gameplay config (pause popup, menu scene, cursor, look, rumble); <c>_devHints</c> is
    /// the prompt row of the development keys; <c>_stance</c> shows the sprint/crouch states; the <c>*Key</c> fields are the checkpoint label and the texts of the
    /// questions and notices.
    /// </summary>
    internal sealed class GameplayScreen : MonoBehaviour, IPauseMenuHost
    {
        [SerializeField] private GameplayConfig _config;
        [SerializeField] private GameObject _devHints;
        [SerializeField] private StanceView _stance;
        [SerializeField] private string _sprintAction = "Gameplay/Sprint";
        [SerializeField] private string _crouchAction = "Gameplay/Crouch";
        [SerializeField] private string _pauseAction = "Gameplay/Pause";
        [SerializeField] private string _attackAction = "Gameplay/Attack";
        [SerializeField] private string _checkpointAction = "Debug/Checkpoint";
        [SerializeField] private string _checkpointLabelKey;
        [SerializeField] private string _checkpointSavedKey;
        [SerializeField] private string _savedKey;
        [SerializeField] private string _saveFailedKey;
        [SerializeField] private string _noSlotKey;
        [SerializeField] private string _quitToMenuTitleKey;
        [SerializeField] private string _quitToMenuMessageKey;
        [SerializeField] private string _quitToMenuConfirmKey;
        [SerializeField] private string _quitToDesktopTitleKey;
        [SerializeField] private string _quitToDesktopMessageKey;
        [SerializeField] private string _quitToDesktopConfirmKey;
        [SerializeField] private string _unsavedTitleKey;
        [SerializeField] private string _unsavedMessageKey;
        [SerializeField] private string _unsavedConfirmKey;
        [SerializeField] private string _cancelKey;

        private readonly List<IDisposable> _handles = new List<IDisposable>();
        private readonly HoldToggle _sprint = new HoldToggle();
        private readonly HoldToggle _crouch = new HoldToggle();
        private IGameSettings _settings;
        private IInputService _input;
        private IPauseService _pause;
        private ICursorService _cursor;
        private IGameNavigator _navigator;
        private IPlaytime _playtime;
        private ICheckpoint _checkpoint;
        private IAppLifecycleService _lifecycle;
        private IRumbleService _rumble;
        private IEventBus _events;
        private PauseController _controller;
        private GameplayExitFlow _exit;
        private bool _devTools;
        private bool _pauseValid;
        private bool _checkpointValid;
        private bool _checkpointing;
        private bool _leaving;
        private bool _navigationOpened;

        public void Resume()
        {
            _controller.Resume();
        }

        public void OnResumePressed()
        {
            _controller.OnResumePressed();
        }

        public void Save()
        {
            _exit.SaveNow();
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
            _ = RunAsync(_exit.QuitToMenuAsync(LeaveToMenuAsync));
        }

        public void QuitToDesktop()
        {
            _ = RunAsync(_exit.QuitToDesktopAsync(QuitApplication));
        }

        public void OnPauseMenuHidden()
        {
            _controller.OnMenuHidden();
        }

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _pause);
            CoreServiceLocator.TryGet(out _cursor);
            CoreServiceLocator.TryGet(out _navigator);
            CoreServiceLocator.TryGet(out _playtime);
            CoreServiceLocator.TryGet(out _checkpoint);
            CoreServiceLocator.TryGet(out _lifecycle);
            CoreServiceLocator.TryGet(out _rumble);
            CoreServiceLocator.TryGet(out _events);
            CoreServiceLocator.TryGet(out _settings);
            CoreServiceLocator.TryGet(out ISaveService save);
            CoreServiceLocator.TryGet(out IConfirmDialog confirm);
            CoreServiceLocator.TryGet(out IUIManager ui);

            _devTools = Debug.isDebugBuild;
            if (_devHints != null)
            {
                _devHints.SetActive(_devTools);
            }

            _pauseValid = _input != null && _input.HasAction(_pauseAction) && ui != null && _config != null && _config.PausePopup != null;
            _checkpointValid = _devTools && _input != null && _input.HasAction(_checkpointAction) && _checkpoint != null;
            IPauseMenuView view = _pauseValid ? new PopupPauseMenuView(ui, _config.PausePopup.ViewId, this) : null;
            _controller = new PauseController(_pause, view, () => Time.frameCount);
            _exit = new GameplayExitFlow(_checkpoint, _playtime, () => save != null ? save.CurrentSlotId : null,
                confirm != null ? (Func<ConfirmRequest, Awaitable<bool>>)confirm.ConfirmAsync : null, Notify, ExitKeys());
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
                if (_devTools && _input.HasAction(_checkpointAction))
                {
                    _handles.Add(_input.RequestContext(InputContextIds.Debug));
                }
            }

            if (_cursor != null && _config != null)
            {
                _handles.Add(_cursor.Request(_config.CursorPolicy));
            }

            if (_events != null)
            {
                _handles.Add(_events.Subscribe<PauseChangedEvent>(OnPauseChanged));
                _handles.Add(_events.Subscribe<InputActivityRecordedEvent>(OnInputRecorded));
            }

            if (_playtime != null)
            {
                _playtime.StartCounting();
            }

            if (_pauseValid)
            {
                _controller.Enter();
            }
        }

        private void OnDisable()
        {
            _sprint.Reset();
            _crouch.Reset();
            _controller.Exit();
            StopRumble();
            if (_playtime != null)
            {
                _playtime.StopCounting();
            }

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
            if (!_leaving && !_checkpointing && (_navigator == null || !_navigator.IsNavigating))
            {
                _controller.Tick();
                if (_pauseValid && _input.WasPressedThisFrame(_pauseAction))
                {
                    _controller.OnPausePressed();
                }
            }

            TickHoldToggle(_sprint, _sprintAction, _settings != null && _settings.ToggleSprint);
            TickHoldToggle(_crouch, _crouchAction, _settings != null && _settings.ToggleCrouch);

            bool paused = _controller.IsMenuOpen || (_pause != null && _pause.IsPaused);
            if (_checkpointValid && !paused && !_leaving && !_checkpointing && _input.WasPressedThisFrame(_checkpointAction))
            {
                _ = CheckpointAsync();
            }
        }

        // Navigation events: off while playing (enforced every frame, also after the loading screen restores its value),
        // switched on once when the pause menu opens and then left to the menus (Settings turns them off during a rebind).
        private void LateUpdate()
        {
            if (_stance != null)
            {
                _stance.Render(_sprint.IsActive, _settings != null && _settings.ToggleSprint, _crouch.IsActive,
                    _settings != null && _settings.ToggleCrouch);
            }

            if (_leaving || (_navigator != null && _navigator.IsNavigating))
            {
                return;
            }

            if (_controller.IsMenuOpen)
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

        // While paused or locked the action reads released: hold mode turns off, toggle mode keeps its state.
        private void TickHoldToggle(HoldToggle state, string action, bool toggleMode)
        {
            bool valid = _input != null && _input.HasAction(action);
            state.Tick(toggleMode, valid && _input.WasPressedThisFrame(action), valid && _input.IsPressed(action));
        }

        private void OnPauseChanged(PauseChangedEvent evt)
        {
            _controller.OnPauseChanged(evt.Reason, evt.ReasonActive);
        }

        private void OnInputRecorded(InputActivityRecordedEvent evt)
        {
            if (evt.Entry.Phase == InputActivityPhase.Pressed && evt.Entry.Action == _attackAction && _config != null
                && _rumble != null && !_leaving)
            {
                RumbleTuning rumble = _config.AttackRumble;
                _rumble.Play(rumble.LowFrequency, rumble.HighFrequency, rumble.DurationSeconds);
            }
        }

        // Development checkpoint: saved before the end-of-frame capture, and no pause opens until it finished, so the
        // thumbnail shows the game.
        private async Awaitable CheckpointAsync()
        {
            _checkpointing = true;
            try
            {
                bool saved = await _checkpoint.SaveCheckpointAsync(_checkpointLabelKey);
                Notify(saved ? _checkpointSavedKey : _saveFailedKey);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                _checkpointing = false;
            }
        }

        // Leaving for the main menu after the save: no pause, menu or rumble may outlive the scene; navigation goes back
        // on before the loading screen records it. When the switch does not start, gameplay carries on.
        private async Awaitable<bool> LeaveToMenuAsync()
        {
            BeginLeaving();
            bool left = false;
            try
            {
                left = _navigator != null && _config != null && await _navigator.GoToAsync(_config.MenuScene);
                if (!left)
                {
                    Debug.LogWarning("[Game] The main menu could not open (another scene switch is running?); staying in gameplay.", this);
                }

                return left;
            }
            finally
            {
                // Still here: back to the paused state the player left from.
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
            StopRumble();
            SetNavigation(true);
        }

        private async Awaitable RunAsync(Awaitable<bool> flow)
        {
            try
            {
                await flow;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void Notify(string key)
        {
            if (!string.IsNullOrEmpty(key) && CoreServiceLocator.TryGet(out IToastService toasts))
            {
                toasts.Show(Loc.Get(key));
            }
        }

        private GameplayExitKeys ExitKeys()
        {
            return new GameplayExitKeys
            {
                QuitToMenuTitle = _quitToMenuTitleKey,
                QuitToMenuMessage = _quitToMenuMessageKey,
                QuitToMenuConfirm = _quitToMenuConfirmKey,
                QuitToDesktopTitle = _quitToDesktopTitleKey,
                QuitToDesktopMessage = _quitToDesktopMessageKey,
                QuitToDesktopConfirm = _quitToDesktopConfirmKey,
                UnsavedTitle = _unsavedTitleKey,
                UnsavedMessage = _unsavedMessageKey,
                UnsavedConfirm = _unsavedConfirmKey,
                Cancel = _cancelKey,
                Saved = _savedKey,
                SaveFailed = _saveFailedKey,
                NoSlot = _noSlotKey,
            };
        }

        private void StopRumble()
        {
            if (_rumble != null)
            {
                _rumble.Stop();
            }
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

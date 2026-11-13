using System;
using Game.Contracts.Navigation;
using Game.Contracts.Presentation;
using Game.Contracts.Session;
using Game.Foundation.Localization;
using Paowa.Core.Foundation.Services;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace Game.Systems.Splash
{
    /// <summary>
    /// The splash scene: plays the studio logo through a <see cref="SplashSequence"/> while preloading (first language
    /// ready, optional shader warm-up), then goes to the next scene. Core has finished bootstrapping before any scene loads,
    /// so its services are ready here. The sequence starts once the switch into this scene is fully revealed; any key,
    /// gamepad button or mouse button requests a skip, which shortens the animation but never the preload (a small loading
    /// indicator shows while the preload is still running). Modifier keys alone and keys pressed with Alt or the system
    /// key held (Alt+Tab) are not skips. The view is redrawn from the sequence every frame. While <see cref="IFirstLaunch"/>
    /// is pending the splash goes to the first-launch scene instead of the next scene.
    /// REFERENCES: <c>_logoGroup</c> fades the logo; <c>_loadingIndicator</c> shows while waiting for the preload;
    /// <c>_config</c> holds timing, preload and destination.
    /// </summary>
    internal sealed class SplashScreen : MonoBehaviour
    {
        // A long first frame (scene load hitch) must not eat the fade in.
        private const float MaxStepSeconds = 0.1f;

        // Pause before trying the switch to the next scene again after it failed.
        private const float RetrySeconds = 1f;

        [SerializeField] private SplashConfig _config;
        [SerializeField] private CanvasGroup _logoGroup;
        [SerializeField] private GameObject _loadingIndicator;

        private SplashSequence _sequence;
        private IGameNavigator _navigator;
        private IAnimationSettings _animationSettings;
        private IDisposable _anyButton;
        private bool _preloadDone;
        private bool _leaving;
        private float _retryAt;

        private bool AnimationsEnabled => _animationSettings == null || _animationSettings.Enabled;

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _navigator);
            CoreServiceLocator.TryGet(out _animationSettings);
            _sequence = new SplashSequence(_config.FadeInSeconds, _config.HoldSeconds, _config.FadeOutSeconds,
                _config.SkipDelaySeconds);
            Render();
        }

        private void OnEnable()
        {
            _anyButton = InputSystem.onAnyButtonPress.Call(control =>
            {
                if (IsSkipPress(control))
                {
                    _sequence.RequestSkip();
                }
            });
        }

        private void OnDisable()
        {
            _anyButton?.Dispose();
            _anyButton = null;
        }

        private void Start()
        {
            _ = PreloadAsync();
        }

        private void Update()
        {
            // While the switch into this scene is still uncovering it, the logo waits (and presses are not skips yet).
            if (_leaving || (_navigator != null && _navigator.IsNavigating))
            {
                return;
            }

            _sequence.Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds), _preloadDone, AnimationsEnabled);
            Render();
            if (_sequence.IsFinished && Time.unscaledTime >= _retryAt)
            {
                _leaving = true;
                _ = LeaveAsync();
            }
        }

        private void Render()
        {
            _logoGroup.alpha = _sequence.LogoAlpha;
            if (_loadingIndicator.activeSelf != _sequence.IsWaitingForPreload)
            {
                _loadingIndicator.SetActive(_sequence.IsWaitingForPreload);
            }
        }

        private static bool IsSkipPress(InputControl control)
        {
            if (!(control.device is Keyboard keyboard))
            {
                return true;
            }

            if (keyboard.altKey.isPressed || keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed)
            {
                return false;
            }

            if (!(control is KeyControl key))
            {
                return true;
            }

            switch (key.keyCode)
            {
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftMeta:
                case Key.RightMeta:
                case Key.ContextMenu:
                case Key.CapsLock:
                case Key.NumLock:
                    return false;
                default:
                    return true;
            }
        }

        private async Awaitable PreloadAsync()
        {
            try
            {
                float deadline = Time.unscaledTime + _config.LocalizationTimeoutSeconds;
                while (!Loc.IsReady && Time.unscaledTime < deadline)
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }

                if (!Loc.IsReady)
                {
                    Debug.LogWarning("[Game] The first language is still not loaded; the splash continues without it.", this);
                }

                if (_config.ShaderVariants != null)
                {
                    // TODO: WarmUp compiles every variant in one frame; switch to a progressive warm-up if the collection
                    // grows enough to stall the logo.
                    _config.ShaderVariants.WarmUp();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                // A failed preload step must not keep the player on the splash.
                Debug.LogException(exception, this);
            }

            _preloadDone = true;
        }

        private async Awaitable LeaveAsync()
        {
            SceneKey next = _config.NextScene;
            if (_config.FirstLaunchScene != null && CoreServiceLocator.TryGet(out IFirstLaunch firstLaunch) && firstLaunch.IsPending)
            {
                next = _config.FirstLaunchScene;
            }

            if (_navigator == null || next == null)
            {
                Debug.LogError("[Game] The splash has no navigator or no next scene; it stays here.", this);
                return;
            }

            try
            {
                // False while another switch runs; try again next frame.
                while (!await _navigator.GoToAsync(next))
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // The scene was unloaded while waiting.
            }
            catch (Exception exception)
            {
                // A failed switch must not strand the player: log it and let Update try again after a pause.
                Debug.LogException(exception, this);
                if (this != null)
                {
                    _retryAt = Time.unscaledTime + RetrySeconds;
                    _leaving = false;
                }
            }
        }
    }
}

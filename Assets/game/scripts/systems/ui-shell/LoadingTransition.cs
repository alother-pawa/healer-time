using System;
using DG.Tweening;
using Game.Contracts.Navigation;
using Game.Contracts.Presentation;
using Game.Foundation.Tweening;
using Paowa.Core.Flow.Scenes;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Input;
using Paowa.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Systems.UIShell
{
    /// <summary>
    /// Loading screen of the UI scene, installed as Core's <see cref="ISceneTransition"/> while enabled: fades in over the
    /// old scene, shows load progress, fades out over the new one. Whenever it is not fully hidden it blocks every input
    /// path: pointer (its group catches raycasts), keyboard/gamepad navigation, submit and cancel of the UI input module
    /// (the current EventSystem stops sending navigation events), Core contexts such as Menu and Gameplay (a global input
    /// lock) and the desktop Cancel stack (a top-priority handler consumes every press). Everything comes back when the
    /// screen is hidden or disabled. A switch to a scene whose key asks for a plain transition shows the background only.
    /// With animations disabled it covers and uncovers in the same frame.
    /// REFERENCES: <c>_group</c> is the full-screen group (sorted above popups); <c>_details</c> holds the title, progress
    /// bar and caption; <c>_progressFill</c> is an Image of type Filled; <c>_config</c> holds the timing.
    /// </summary>
    internal sealed class LoadingTransition : MonoBehaviour, ISceneTransition
    {
        private const float CoveredAlpha = 1f;
        private const float HiddenAlpha = 0f;
        private const float EmptyProgress = 0f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private GameObject _details;
        [SerializeField] private Image _progressFill;
        [SerializeField] private LoadingTransitionConfig _config;

        private readonly CancelBlocker _cancelBlocker = new CancelBlocker();
        private IAnimationSettings _animationSettings;
        private IInputService _input;
        private ICancelStack _cancelStack;
        private IDisposable _installation;
        private IDisposable _switchBinding;
        private IDisposable _inputLock;
        private bool _cancelBlocked;
        private Tween _fade;
        private float _coveredAt;

        // EventSystem whose navigation events are switched off while blocking, and its setting before that.
        private EventSystem _blockedEventSystem;
        private bool _previousSendNavigationEvents;

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _animationSettings);
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _cancelStack);
            SetBlocking(false);
            _group.alpha = HiddenAlpha;
        }

        private void OnEnable()
        {
            _installation = CoreServiceLocator.Get<ISceneService>().SetTransition(this);
            if (CoreServiceLocator.TryGet(out IEventBus events))
            {
                _switchBinding = events.Subscribe<SceneSwitchStartingEvent>(OnSwitchStarting);
            }
        }

        private void OnDisable()
        {
            _installation?.Dispose();
            _installation = null;
            _switchBinding?.Dispose();
            _switchBinding = null;
            Unblock();
        }

        public async Awaitable CoverAsync()
        {
            SetBlocking(true);
            _progressFill.fillAmount = EmptyProgress;
            await FadeTo(CoveredAlpha, _config.CoverSeconds).AsAwaitable();
            _coveredAt = Time.unscaledTime;
        }

        public void ReportProgress(float progress)
        {
            _progressFill.fillAmount = progress;
        }

        public async Awaitable RevealAsync()
        {
            // Animations turned off during the wait end it at once.
            float showUntil = _coveredAt + _config.MinimumCoveredSeconds;
            while (AnimationsEnabled && Time.unscaledTime < showUntil)
            {
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }

            await FadeTo(HiddenAlpha, _config.RevealSeconds).AsAwaitable();
            SetBlocking(false);

            // The next switch shows the details unless its destination asks for a plain transition.
            ShowDetails(true);
        }

        private bool AnimationsEnabled => _animationSettings == null || _animationSettings.Enabled;

        private void OnSwitchStarting(SceneSwitchStartingEvent evt)
        {
            ShowDetails(evt.Scene == null || !evt.Scene.PlainTransition);
        }

        private void ShowDetails(bool show)
        {
            if (_details != null)
            {
                _details.SetActive(show);
            }
        }

        private Tween FadeTo(float alpha, float seconds)
        {
            _fade.KillIfActive();
            Tween fade = _group.DOFade(alpha, seconds)
                .SetEase(_config.Ease)
                .SetUpdate(true)
                .SetLink(gameObject);
            fade.OnKill(() =>
            {
                if (_fade == fade)
                {
                    _fade = null;
                }
            });
            _fade = fade;
            return fade.Gate(_animationSettings);
        }

        private void SetBlocking(bool blocking)
        {
            _group.blocksRaycasts = blocking;
            _group.interactable = blocking;
            if (blocking)
            {
                Block();
            }
            else
            {
                Unblock();
            }
        }

        private void Block()
        {
            EventSystem current = EventSystem.current;
            if (_blockedEventSystem == null && current != null)
            {
                _blockedEventSystem = current;
                _previousSendNavigationEvents = current.sendNavigationEvents;
                current.sendNavigationEvents = false;
            }

            if (_inputLock == null && _input != null)
            {
                _inputLock = _input.Lock();
            }

            if (!_cancelBlocked && _cancelStack != null)
            {
                _cancelStack.Push(_cancelBlocker);
                _cancelBlocked = true;
            }
        }

        private void Unblock()
        {
            if (_blockedEventSystem != null)
            {
                _blockedEventSystem.sendNavigationEvents = _previousSendNavigationEvents;
            }

            _blockedEventSystem = null;
            _inputLock?.Dispose();
            _inputLock = null;
            if (_cancelBlocked)
            {
                _cancelStack.Remove(_cancelBlocker);
                _cancelBlocked = false;
            }
        }

        /// <summary>Consumes Cancel/Back while the loading screen covers, ahead of every other handler.</summary>
        private sealed class CancelBlocker : IUICancelHandler
        {
            public int Priority => int.MaxValue;

            public bool TryCancel()
            {
                return true;
            }
        }
    }
}

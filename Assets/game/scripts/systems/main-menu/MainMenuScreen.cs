using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Contracts.Navigation;
using Game.Contracts.Presentation;
using Game.Contracts.Roster;
using Game.Foundation.Tweening;
using Game.Foundation.UI;
using Paowa.Audio;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Lifecycle;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Desktop;
using Paowa.Economy;
using Paowa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Systems.MainMenu
{
    /// <summary>
    /// The main menu, two pages side by side. Home: the chosen hero and healer in the middle, the coins owned, Play (starts a
    /// run through <see cref="IGameNavigator"/>), Characters, Settings (<see cref="ISettingsMenu"/>) and Quit (asks, then
    /// quits through <see cref="IAppLifecycleService"/>; leaves Play Mode in the Editor). Characters: the
    /// <see cref="CharacterPage"/>, sliding in from the right; Back (or Cancel) slides home back in from the left. The open
    /// page is state (<c>_onCharacters</c>): positions are set from it at once with animations off. Cancel on the home
    /// page opens the same Quit question (the screen is a Cancel handler below the UI package's popup handler). While shown
    /// it requests the Menu cursor policy, plays the menu music and shows Core's <see cref="BuildInfo.DisplayText"/>.
    /// REFERENCES: <c>_pages</c> holds both pages (home over its full width, characters one width to the right) and slides
    /// by its anchors; the buttons and labels; <c>_coin</c> is the currency shown; <c>_gameplayScene</c> is the scene Play
    /// opens; <c>_music</c> and <c>_swipe</c> are sounds; the <c>*Key</c> fields are the texts of the Quit question.
    /// </summary>
    internal sealed class MainMenuScreen : MonoBehaviour, IUICancelHandler
    {
        // Below the UI package's popup handler (0): runs only when no popup is open and no focus scope consumed the press.
        private const int QuitCancelPriority = CancelHandlerPriorities.Popup - 100;
        private const float SwipeSeconds = 0.35f;

        [SerializeField] private RectTransform _pages;
        [SerializeField] private CharacterPage _characters;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _charactersButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private SpriteFlipbook _heroPreview;
        [SerializeField] private SpriteFlipbook _healerPreview;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private TMP_Text _versionText;
        [SerializeField] private CurrencyDefinition _coin;
        [SerializeField] private SceneKey _gameplayScene;
        [SerializeField] private AudioClip _music;
        [SerializeField] private AudioClip _swipe;
        [SerializeField] private string _quitTitleKey;
        [SerializeField] private string _quitMessageKey;
        [SerializeField] private string _quitConfirmKey;
        [SerializeField] private string _quitCancelKey;

        private readonly List<IDisposable> _handles = new List<IDisposable>();
        private ICancelStack _cancelStack;
        private ICursorService _cursor;
        private IRoster _roster;
        private IWallet _wallet;
        private IAnimationSettings _animations;
        private ISfxService _sfx;
        private IMusicService _musicService;
        private IEventBus _events;
        private bool _confirmingQuit;
        private bool _starting;
        private bool _onCharacters;

        int IUICancelHandler.Priority => QuitCancelPriority;

        bool IUICancelHandler.TryCancel()
        {
            if (_onCharacters)
            {
                ShowPage(false);
                return true;
            }

            // Without the dialog a stray Escape must never quit (only the Quit button does then); while a Quit confirmation
            // is still being answered the press is not ours.
            if (_confirmingQuit || !CoreServiceLocator.IsRegistered<IConfirmDialog>())
            {
                return false;
            }

            _ = QuitAsync();
            return true;
        }

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _cancelStack);
            CoreServiceLocator.TryGet(out _cursor);
            CoreServiceLocator.TryGet(out _roster);
            CoreServiceLocator.TryGet(out _wallet);
            CoreServiceLocator.TryGet(out _animations);
            CoreServiceLocator.TryGet(out _sfx);
            CoreServiceLocator.TryGet(out _musicService);
            CoreServiceLocator.TryGet(out _events);
            _playButton.onClick.AddListener(OpenPlay);
            _charactersButton.onClick.AddListener(() => ShowPage(true));
            _backButton.onClick.AddListener(() => ShowPage(false));
            _settingsButton.onClick.AddListener(OpenSettings);
            _quitButton.onClick.AddListener(() => _ = QuitAsync());
            if (_roster != null)
            {
                _characters.Init(_roster);
            }
        }

        private void OnEnable()
        {
            _versionText.text = BuildInfo.Current.DisplayText;
            if (_cursor != null)
            {
                _handles.Add(_cursor.Request(CursorPolicy.Menu));
            }

            if (_cancelStack != null)
            {
                _cancelStack.Push(this);
            }

            if (_events != null)
            {
                _handles.Add(_events.Subscribe<RosterSelectionChangedEvent>(_ => RenderHome()));
                _handles.Add(_events.Subscribe<CurrencyChangedEvent>(_ => RenderCoins()));
            }

            if (_musicService != null && _music != null)
            {
                _musicService.PlayMusic(_music, 1f);
            }

            _onCharacters = false;
            PlacePages(false);
            RenderHome();
            RenderCoins();
        }

        private void OnDisable()
        {
            foreach (IDisposable handle in _handles)
            {
                handle.Dispose();
            }

            _handles.Clear();
            if (_cancelStack != null)
            {
                _cancelStack.Remove(this);
            }
        }

        private void RenderHome()
        {
            if (_roster == null)
            {
                return;
            }

            HeroDefinition hero = _roster.SelectedHero;
            HealerDefinition healer = _roster.SelectedHealer;
            _heroPreview.Play(hero.PreviewFrames, hero.PreviewFrameSeconds, hero.Tint, hero.Scale);
            _healerPreview.Play(healer.PreviewFrames, healer.PreviewFrameSeconds, healer.Tint, healer.Scale);
        }

        private void RenderCoins()
        {
            _coinsText.text = _wallet != null && _coin != null ? _wallet.Get(_coin).ToString() : "0";
        }

        // State first: the open page changes, then the pages slide (or jump with animations off) to where it puts them.
        private void ShowPage(bool characters)
        {
            if (_onCharacters == characters)
            {
                return;
            }

            _onCharacters = characters;
            if (characters)
            {
                _characters.Render();
            }

            if (_sfx != null && _swipe != null)
            {
                _sfx.PlaySfx(_swipe, AudioCategory.UI);
            }

            PlacePages(_animations == null || _animations.Enabled);
            Selectable first = characters ? _characters.FirstSelectable : _playButton;
            if (EventSystem.current != null && first != null)
            {
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }

        // The pages slide by whole screen widths through their container's anchors (0: home shown, -1: characters shown),
        // so the result does not depend on the window size.
        private void PlacePages(bool animate)
        {
            float target = _onCharacters ? -1f : 0f;
            DOTween.Kill(_pages);
            if (!animate)
            {
                SetPagesOffset(target);
                return;
            }

            DOTween.To(() => _pages.anchorMin.x, SetPagesOffset, target, SwipeSeconds).SetEase(Ease.OutCubic).SetTarget(_pages)
                .SetLink(gameObject).Gate(_animations);
        }

        private void SetPagesOffset(float offset)
        {
            _pages.anchorMin = new Vector2(offset, 0f);
            _pages.anchorMax = new Vector2(offset + 1f, 1f);
            _pages.offsetMin = Vector2.zero;
            _pages.offsetMax = Vector2.zero;
        }

        private void OpenPlay()
        {
            if (_starting || _gameplayScene == null || !CoreServiceLocator.TryGet(out IGameNavigator navigator))
            {
                return;
            }

            _ = StartRunAsync(navigator);
        }

        // One press starts one switch; a refused switch (another one running) lets Play work again.
        private async Awaitable StartRunAsync(IGameNavigator navigator)
        {
            _starting = true;
            try
            {
                await navigator.GoToAsync(_gameplayScene);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                _starting = false;
            }
        }

        private void OpenSettings()
        {
            if (CoreServiceLocator.TryGet(out ISettingsMenu settings))
            {
                settings.Open();
            }
            else
            {
                Debug.LogWarning("[Game] The settings module is not running; the Settings button does nothing.", this);
            }
        }

        private async Awaitable QuitAsync()
        {
            if (_confirmingQuit)
            {
                return;
            }

            _confirmingQuit = true;
            try
            {
                bool quit = true;
                if (CoreServiceLocator.TryGet(out IConfirmDialog confirm))
                {
                    quit = await confirm.ConfirmAsync(
                        new ConfirmRequest(_quitTitleKey, _quitMessageKey, _quitConfirmKey, _quitCancelKey));
                }

                if (quit)
                {
                    Quit();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                _confirmingQuit = false;
            }
        }

        private static void Quit()
        {
            if (CoreServiceLocator.TryGet(out IAppLifecycleService lifecycle))
            {
                lifecycle.Quit();
            }
            else
            {
                Application.Quit();
            }
        }
    }
}

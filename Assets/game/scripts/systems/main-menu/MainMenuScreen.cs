using System;
using Game.Contracts.Navigation;
using Game.Contracts.Presentation;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Lifecycle;
using Paowa.Core.Foundation.Services;
using Paowa.Desktop;
using Paowa.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.MainMenu
{
    /// <summary>
    /// The main menu: Play opens <see cref="IPlayMenu"/> (a notice until the save-slot flow registers it), Settings opens
    /// <see cref="ISettingsMenu"/>, Achievements opens <see cref="IAchievementsMenu"/>, Credits opens
    /// <see cref="ICreditsMenu"/>, Quit asks for confirmation and then quits through <see cref="IAppLifecycleService"/> (leaves Play Mode in the Editor). Cancel/Back with nothing else
    /// to close opens the same Quit confirmation: the screen is a Cancel handler below the UI package's popup handler. While shown
    /// it requests the Menu cursor policy (visible in Pointer mode, hidden in Navigation mode). The version label shows
    /// Core's <see cref="BuildInfo.DisplayText"/> ("v1.2.0 (build 128, commit 3f2a9c1)", "v1.2.0 (dev)" without a stamp).
    /// REFERENCES: the five buttons; <c>_versionText</c>; the <c>*Key</c> fields are the localization keys of the dialogs.
    /// </summary>
    internal sealed class MainMenuScreen : MonoBehaviour, IUICancelHandler
    {
        // Below the UI package's popup handler (0): runs only when no popup is open and no focus scope consumed the press.
        private const int QuitCancelPriority = CancelHandlerPriorities.Popup - 100;

        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _achievementsButton;
        [SerializeField] private Button _creditsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TMP_Text _versionText;
        [SerializeField] private string _quitTitleKey;
        [SerializeField] private string _quitMessageKey;
        [SerializeField] private string _quitConfirmKey;
        [SerializeField] private string _quitCancelKey;
        [SerializeField] private string _playNoticeTitleKey;
        [SerializeField] private string _playNoticeMessageKey;
        [SerializeField] private string _playNoticeConfirmKey;

        private ICancelStack _cancelStack;
        private ICursorService _cursor;
        private IDisposable _cursorRequest;
        private bool _confirmingQuit;

        int IUICancelHandler.Priority => QuitCancelPriority;

        bool IUICancelHandler.TryCancel()
        {
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
            _playButton.onClick.AddListener(OpenPlay);
            _settingsButton.onClick.AddListener(OpenSettings);
            _achievementsButton.onClick.AddListener(ShowAchievements);
            _creditsButton.onClick.AddListener(ShowCredits);
            _quitButton.onClick.AddListener(() => _ = QuitAsync());
        }

        private void OnEnable()
        {
            _versionText.text = BuildInfo.Current.DisplayText;
            if (_cursor != null)
            {
                _cursorRequest = _cursor.Request(CursorPolicy.Menu);
            }

            if (_cancelStack != null)
            {
                _cancelStack.Push(this);
            }
        }

        private void OnDisable()
        {
            _cursorRequest?.Dispose();
            _cursorRequest = null;
            if (_cancelStack != null)
            {
                _cancelStack.Remove(this);
            }
        }

        private void OpenPlay()
        {
            if (CoreServiceLocator.TryGet(out IPlayMenu play))
            {
                play.Open();
                return;
            }

            if (CoreServiceLocator.TryGet(out IConfirmDialog confirm))
            {
                // A notice: the answer does not matter, so nothing awaits it.
                _ = confirm.ConfirmAsync(new ConfirmRequest(_playNoticeTitleKey, _playNoticeMessageKey, _playNoticeConfirmKey, null));
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

        private void ShowAchievements()
        {
            if (CoreServiceLocator.TryGet(out IAchievementsMenu achievements))
            {
                achievements.Open();
            }
            else
            {
                Debug.LogWarning("[Game] The achievements module is not running; the Achievements button does nothing.", this);
            }
        }

        private void ShowCredits()
        {
            if (CoreServiceLocator.TryGet(out ICreditsMenu credits))
            {
                credits.Open();
            }
            else
            {
                Debug.LogWarning("[Game] The credits module is not running; the Credits button does nothing.", this);
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

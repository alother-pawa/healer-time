using System;
using System.Collections.Generic;
using Game.Contracts.Navigation;
using Game.Contracts.Session;
using Game.Contracts.Settings;
using Game.Foundation.Localization;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Desktop;
using Paowa.Localization;
using Paowa.Settings;
using Paowa.UI;
using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.FirstLaunch
{
    /// <summary>
    /// The first-launch screen, shown once after the splash: language (each language's own name), interface size and
    /// brightness quick choices, the photosensitivity notice, Continue. Choices go through the settings service (the same
    /// values as the Settings popup; interface size is applied at once). Continue, or Cancel (Esc, B) as a skip, completes
    /// <see cref="IFirstLaunch"/> and goes to the main menu; the screen never shows again on this install. The choice rows
    /// are built in <c>Awake</c> from the settings (a row whose setting is not in the catalog is left out), navigation is
    /// explicit (left/right in a row, up/down between rows), and the "chosen" markers are drawn from the settings every
    /// frame, so they are right whatever changed the values. No animation.
    /// REFERENCES: <c>_config</c> (destination, quick choices); <c>_scope</c> is the screen's focus scope; the three
    /// <c>*Row</c> objects hold the choice buttons; <c>_choiceTemplate</c> is an inactive choice outside them;
    /// <c>_continueButton</c>.
    /// </summary>
    internal sealed class FirstLaunchScreen : MonoBehaviour, IUICancelHandler
    {
        // Below the UI package's popup handler (0): runs only when no popup is open and no focus scope consumed the press.
        private const int SkipCancelPriority = CancelHandlerPriorities.Popup - 100;

        [SerializeField] private FirstLaunchConfig _config;
        [SerializeField] private FocusScope _scope;
        [SerializeField] private RectTransform _languageRow;
        [SerializeField] private RectTransform _uiScaleRow;
        [SerializeField] private RectTransform _brightnessRow;
        [SerializeField] private FirstLaunchChoice _choiceTemplate;
        [SerializeField] private Button _continueButton;

        private readonly List<Row> _rows = new List<Row>();
        private ISettingsService _settings;
        private ILocalizationService _localization;
        private IFirstLaunch _firstLaunch;
        private IGameNavigator _navigator;
        private ICursorService _cursor;
        private ICancelStack _cancelStack;
        private IDisposable _cursorRequest;
        private IDisposable _languageBinding;
        private bool _leaving;

        int IUICancelHandler.Priority => SkipCancelPriority;

        bool IUICancelHandler.TryCancel()
        {
            if (_leaving)
            {
                return false;
            }

            _ = FinishAsync();
            return true;
        }

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _settings);
            CoreServiceLocator.TryGet(out _localization);
            CoreServiceLocator.TryGet(out _firstLaunch);
            CoreServiceLocator.TryGet(out _navigator);
            CoreServiceLocator.TryGet(out _cursor);
            CoreServiceLocator.TryGet(out _cancelStack);
            _continueButton.onClick.AddListener(() => _ = FinishAsync());
            BuildRows();
            LinkNavigation();
        }

        private void OnEnable()
        {
            if (_cursor != null)
            {
                _cursorRequest = _cursor.Request(CursorPolicy.Menu);
            }

            if (_cancelStack != null)
            {
                _cancelStack.Push(this);
            }

            if (CoreServiceLocator.TryGet(out IEventBus events))
            {
                _languageBinding = events.Subscribe<LanguageChangedEvent>(_ => Relabel());
            }

            Relabel();
            Draw();
        }

        private void OnDisable()
        {
            _cursorRequest?.Dispose();
            _cursorRequest = null;
            _languageBinding?.Dispose();
            _languageBinding = null;
            if (_cancelStack != null)
            {
                _cancelStack.Remove(this);
            }
        }

        private void LateUpdate()
        {
            Draw();
        }

        private void BuildRows()
        {
            if (_settings == null)
            {
                Debug.LogWarning("[Game] The settings service is not running; the first-launch screen shows only Continue.", this);
                return;
            }

            if (_settings.TryGetDefinition(SettingIds.Language, out _))
            {
                var options = new List<SettingOption>();
                _settings.GetOptions(SettingIds.Language, options);
                var values = new List<string>();
                var labels = new List<string>();
                foreach (SettingOption option in options)
                {
                    values.Add(option.Value);
                    labels.Add(option.LabelIsKey ? option.Label : null);
                }

                AddRow(_languageRow, values.Count, i => _settings.SetString(SettingIds.Language, values[i]),
                    i => values[i] == CurrentLanguage(), i => labels[i] != null ? Loc.Get(labels[i]) : values[i]);
            }

            AddSliderRow(_uiScaleRow, GameSettingIds.UIScale, _config != null ? _config.UIScaleChoices : new float[0]);
            AddSliderRow(_brightnessRow, DesktopSettingIds.DisplayBrightness, _config != null ? _config.BrightnessChoices : new float[0]);

            // Start on the language in use: the first thing to pick.
            Row first = _rows.Count > 0 ? _rows[0] : null;
            Selectable start = first != null && first.Parent == _languageRow ? first.Choices[Mathf.Max(0, first.ChosenIndex())].Button : null;
            _scope.FirstSelected = start != null ? start : _continueButton;
        }

        private void AddSliderRow(RectTransform parent, string id, float[] choices)
        {
            if (!_settings.TryGetDefinition(id, out SettingDefinition definition) || !(definition is SliderSettingDefinition slider)
                || choices.Length == 0)
            {
                parent.gameObject.SetActive(false);
                return;
            }

            float tolerance = slider.Step > 0f ? slider.Step * 0.5f : 0.001f;
            AddRow(parent, choices.Length, i => ApplySlider(slider, choices[i]),
                i => Mathf.Abs(_settings.GetFloat(id) - choices[i]) < tolerance, i => slider.Format(choices[i]));
        }

        private void AddRow(RectTransform parent, int count, Action<int> choose, Func<int, bool> isChosen, Func<int, string> label)
        {
            if (count == 0)
            {
                parent.gameObject.SetActive(false);
                return;
            }

            var row = new Row(parent, isChosen, label);
            for (int i = 0; i < count; i++)
            {
                int index = i;
                FirstLaunchChoice choice = Instantiate(_choiceTemplate, parent);
                choice.gameObject.SetActive(true);
                choice.Button.onClick.AddListener(() => choose(index));
                row.Choices.Add(choice);
            }

            _rows.Add(row);
        }

        // Deferred settings (interface size) are committed at once: the player sees the result of the choice.
        private void ApplySlider(SliderSettingDefinition slider, float value)
        {
            _settings.SetFloat(slider.Id, value);
            if (slider.ApplyMode == SettingApplyMode.Deferred)
            {
                _settings.ApplyTab(slider.TabId);
            }
        }

        // Left/right inside a row, up/down to the same column of the next row (clamped), Continue below the last row.
        private void LinkNavigation()
        {
            for (int r = 0; r < _rows.Count; r++)
            {
                List<FirstLaunchChoice> choices = _rows[r].Choices;
                for (int i = 0; i < choices.Count; i++)
                {
                    Navigation navigation = choices[i].Button.navigation;
                    navigation.mode = Navigation.Mode.Explicit;
                    navigation.selectOnLeft = i > 0 ? choices[i - 1].Button : null;
                    navigation.selectOnRight = i < choices.Count - 1 ? choices[i + 1].Button : null;
                    navigation.selectOnUp = r > 0 ? Column(_rows[r - 1], i) : null;
                    navigation.selectOnDown = r < _rows.Count - 1 ? Column(_rows[r + 1], i) : _continueButton;
                    choices[i].Button.navigation = navigation;
                }
            }

            Navigation continueNavigation = _continueButton.navigation;
            continueNavigation.mode = Navigation.Mode.Explicit;
            continueNavigation.selectOnUp = _rows.Count > 0 ? Column(_rows[_rows.Count - 1], 0) : null;
            continueNavigation.selectOnDown = null;
            continueNavigation.selectOnLeft = null;
            continueNavigation.selectOnRight = null;
            _continueButton.navigation = continueNavigation;
        }

        private static Selectable Column(Row row, int index)
        {
            return row.Choices[Mathf.Min(index, row.Choices.Count - 1)].Button;
        }

        private void Relabel()
        {
            foreach (Row row in _rows)
            {
                for (int i = 0; i < row.Choices.Count; i++)
                {
                    row.Choices[i].SetLabel(row.Label(i));
                }
            }
        }

        private void Draw()
        {
            foreach (Row row in _rows)
            {
                for (int i = 0; i < row.Choices.Count; i++)
                {
                    row.Choices[i].SetChosen(row.IsChosen(i));
                }
            }
        }

        // The requested language while it loads, else the one in use.
        private string CurrentLanguage()
        {
            string requested = _settings != null ? _settings.GetString(SettingIds.Language) : string.Empty;
            if (!string.IsNullOrEmpty(requested))
            {
                return requested;
            }

            return _localization != null ? _localization.CurrentLanguage : string.Empty;
        }

        private async Awaitable FinishAsync()
        {
            if (_leaving)
            {
                return;
            }

            _leaving = true;
            if (_firstLaunch != null)
            {
                _firstLaunch.Complete();
            }

            if (_navigator == null || _config == null || _config.NextScene == null)
            {
                Debug.LogError("[Game] The first-launch screen has no navigator or next scene; it stays here.", this);
                _leaving = false;
                return;
            }

            try
            {
                // False while another switch runs; try again next frame.
                while (!await _navigator.GoToAsync(_config.NextScene))
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
                Debug.LogException(exception, this);
                if (this != null)
                {
                    _leaving = false;
                }
            }
        }

        private sealed class Row
        {
            internal Row(RectTransform row, Func<int, bool> isChosen, Func<int, string> label)
            {
                Parent = row;
                IsChosen = isChosen;
                Label = label;
            }

            internal RectTransform Parent { get; }

            internal Func<int, bool> IsChosen { get; }

            internal Func<int, string> Label { get; }

            internal List<FirstLaunchChoice> Choices { get; } = new List<FirstLaunchChoice>();

            internal int ChosenIndex()
            {
                for (int i = 0; i < Choices.Count; i++)
                {
                    if (IsChosen(i))
                    {
                        return i;
                    }
                }

                return -1;
            }
        }
    }
}

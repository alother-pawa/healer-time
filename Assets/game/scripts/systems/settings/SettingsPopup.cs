using System;
using System.Collections.Generic;
using Game.Contracts.Presentation;
using Game.Foundation.Localization;
using Game.Foundation.UI;
using Paowa.Audio;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Desktop;
using Paowa.Gamepad;
using Paowa.Input;
using Paowa.Input.Rebinding;
using Paowa.Localization;
using Paowa.Settings;
using Paowa.UI;
using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Systems.Settings
{
    /// <summary>
    /// The settings screen over <see cref="ISettingsService"/>: one page per catalog tab (switched by the
    /// <see cref="TabGroup"/>: Q/E, LB/RB, LT/RT, a click, or left/right on a tab), rows built from the tab's definitions, a
    /// description of the selected row, and a footer with Apply / Revert (enabled while the tab has staged values), Defaults
    /// (asks, then resets the tab) and Close, each also on a shortcut action shown in the prompt row. Everything visible is
    /// redrawn from the model on every <see cref="SettingChangedEvent"/>, binding or glyph change and language change, never
    /// from an animation callback. Leaving a tab or closing with staged values asks "Apply changes?"
    /// (<see cref="PendingChangesPrompt"/>); a popup hidden any other way drops them. Key binding rows rebind through
    /// <see cref="KeyBindingRebinder"/>: while a slot listens, UI navigation and pointer clicks are blocked (navigation comes
    /// back the frame after the rebind ends, so the key that finished it is not also a Submit). Volume changes play a short
    /// preview in their category while the popup is open.
    /// REFERENCES: <c>_tabs</c> holds the pages and tab buttons; <c>_pages</c>, <c>_tabButtons</c> and <c>_tabBackgrounds</c>
    /// pair with its pages by index (backgrounds drawn with <c>_selectedTabSprite</c>/<c>_idleTabSprite</c>); the
    /// <c>_*Row</c> fields are the row prefabs per kind; <c>_applyPrompt</c>/<c>_revertPrompt</c> show only while Apply and
    /// Revert are possible; <c>_descriptionPanel</c> holds <c>_descriptionTitle</c> and <c>_descriptionBody</c>;
    /// <c>_rebindBlocker</c> is a transparent cover that catches clicks while a slot listens, with
    /// <c>_rebindCancelButton</c> on it (cancels the rebind with the mouse alone); the <c>_*Key</c> fields are the confirmation
    /// texts.
    /// </summary>
    internal sealed class SettingsPopup : PopupBase
    {
        // Between the focus scope handler (100) and the popup handler (0): Cancel/Back asks about staged values first.
        private const int CancelPriority = CancelHandlerPriorities.Popup + 50;

        [SerializeField] private TabGroup _tabs;
        [SerializeField] private List<SettingsPage> _pages = new List<SettingsPage>();
        [SerializeField] private List<Button> _tabButtons = new List<Button>();
        [SerializeField] private List<Image> _tabBackgrounds = new List<Image>();
        [SerializeField] private Sprite _selectedTabSprite;
        [SerializeField] private Sprite _idleTabSprite;
        [SerializeField] private SettingRow _toggleRow;
        [SerializeField] private SettingRow _sliderRow;
        [SerializeField] private SettingRow _choiceRow;
        [SerializeField] private SettingRow _keyBindingRow;
        [SerializeField] private SettingRow _buttonRow;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _revertButton;
        [SerializeField] private Button _defaultsButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _applyPrompt;
        [SerializeField] private GameObject _revertPrompt;
        [SerializeField] private string _applyAction = "Menu/Apply";
        [SerializeField] private string _revertAction = "Menu/Revert";
        [SerializeField] private string _defaultsAction = "Menu/Defaults";
        [SerializeField] private GameObject _descriptionPanel;
        [SerializeField] private LocalizedLabel _descriptionTitle;
        [SerializeField] private LocalizedLabel _descriptionBody;
        [SerializeField] private GameObject _rebindBlocker;
        [SerializeField] private RebindCancelButton _rebindCancelButton;
        [Tooltip("Seconds a key binding slot listens before it cancels itself.")]
        [Min(0f)] [SerializeField] private float _rebindTimeoutSeconds = 10f;
        [SerializeField] private string _cancelKey;
        [SerializeField] private string _swapTitleKey;
        [SerializeField] private string _swapMessageKey;
        [SerializeField] private string _swapConfirmKey;
        [SerializeField] private string _defaultsTitleKey;
        [SerializeField] private string _defaultsMessageKey;
        [SerializeField] private string _defaultsConfirmKey;
        [SerializeField] private string _pendingTitleKey;
        [SerializeField] private string _pendingMessageKey;
        [SerializeField] private string _pendingApplyKey;
        [SerializeField] private string _pendingDiscardKey;
        [SerializeField] private string _buttonMessageKey;
        [SerializeField] private string _buttonConfirmKey;
        [Tooltip("Played in a volume's category when the volume changes while the popup is open; none plays nothing.")]
        [SerializeField] private AudioClip _volumePreview;
        [Min(0f)] [SerializeField] private float _previewInterval = 0.1f;

        private readonly List<IDisposable> _bindings = new List<IDisposable>();
        private readonly List<string> _tabIds = new List<string>();
        private readonly List<Selectable> _footer = new List<Selectable>();
        private ISettingsService _settings;
        private IInputService _input;
        private IRebindService _rebind;
        private IDisplayService _display;
        private IUIFocusService _focus;
        private ICancelStack _cancelStack;
        private IConfirmDialog _confirm;
        private ISfxService _sfx;
        private KeyBindingRebinder _rebinder;
        private SettingRowContext _context;
        private PendingChangesPrompt _pending;
        private CancelHandler _cancelHandler;
        private bool _shortcutsValid;
        private bool _isOpen;
        private bool _showing;
        private bool _warnedTabs;
        private int _currentTab;
        private GameObject _describedObject;
        private float _nextPreviewTime;
        private bool _navigationBlocked;
        private bool _navigationWasEnabled;
        private int _restoreNavigationFrame = -1;

        private SettingsPage CurrentPage => _currentTab >= 0 && _currentTab < _pages.Count ? _pages[_currentTab] : null;

        private string CurrentTabId => CurrentPage != null ? CurrentPage.TabId : string.Empty;

        protected override void Awake()
        {
            base.Awake();
            CoreServiceLocator.TryGet(out _settings);
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _rebind);
            CoreServiceLocator.TryGet(out _display);
            CoreServiceLocator.TryGet(out _focus);
            CoreServiceLocator.TryGet(out _cancelStack);
            CoreServiceLocator.TryGet(out _confirm);
            CoreServiceLocator.TryGet(out _sfx);
            CoreServiceLocator.TryGet(out IInputGlyphs glyphs);
            if (_settings == null)
            {
                Debug.LogError("[Game] The settings popup needs the settings package's module; its pages stay empty.", this);
            }

            GamepadSettings schemes = GamepadSettings.LoadOrDefault();
            if (_input != null && _rebind != null)
            {
                var options = new RebindOptions { TimeoutSeconds = _rebindTimeoutSeconds }
                    .SetCancelPaths(schemes.KeyboardMouseScheme, "<Keyboard>/escape", "<Gamepad>/start")
                    .SetCancelPaths(schemes.GamepadScheme, "<Gamepad>/start", "<Keyboard>/escape");
                _rebinder = new KeyBindingRebinder(_input, _rebind, options, Confirm, SwapRequest);
                _rebinder.PhaseChanged += OnRebindPhaseChanged;
                _shortcutsValid = _input.HasAction(_applyAction) && _input.HasAction(_revertAction) && _input.HasAction(_defaultsAction);
            }

            _context = new SettingRowContext(_settings, _input, glyphs, _rebinder, schemes.GamepadScheme, Rebind, InvokeButton);
            if (_settings != null)
            {
                _pending = new PendingChangesPrompt(_settings, Confirm,
                    new ConfirmRequest(_pendingTitleKey, _pendingMessageKey, _pendingApplyKey, _pendingDiscardKey));
            }

            foreach (SettingsPage page in _pages)
            {
                _tabIds.Add(page.TabId);
                page.SchemeChanged += () =>
                {
                    if (_isOpen)
                    {
                        Relink(CurrentPage);
                    }
                };
            }

            if (_rebindCancelButton != null)
            {
                _rebindCancelButton.Pressed += () =>
                {
                    if (_rebinder != null)
                    {
                        _rebinder.Cancel();
                    }
                };
            }

            _cancelHandler = new CancelHandler(this);
            _tabs.OnTabChanged.AddListener(OnTabChanged);
            _applyButton.onClick.AddListener(Apply);
            _revertButton.onClick.AddListener(Revert);
            _defaultsButton.onClick.AddListener(() => _ = DefaultsAsync());
            _closeButton.onClick.AddListener(() => _ = CloseAsync());
        }

        protected override void OnShown(object payload)
        {
            _isOpen = true;
            if (_display != null)
            {
                // Picks up a mode changed outside the settings (Alt+Enter, a resized window) and the monitor's sizes.
                _display.RefreshAvailableModes();
            }

            bool rebuilt = BuildPages();
            if (CoreServiceLocator.TryGet(out IEventBus events))
            {
                _bindings.Add(events.Subscribe<SettingChangedEvent>(_ => Redraw()));
                _bindings.Add(events.Subscribe<InputGlyphsChangedEvent>(_ => Redraw()));
                // Also without the glyph module: a binding reset must reach the key binding rows.
                _bindings.Add(events.Subscribe<InputBindingsChangedEvent>(_ => Redraw()));
                _bindings.Add(events.Subscribe<LanguageChangedEvent>(_ => Redraw()));
                _bindings.Add(events.Subscribe<AudioVolumeChangedEvent>(OnVolumeChanged));
            }

            if (_cancelStack != null)
            {
                _cancelStack.Push(_cancelHandler);
            }

            // Opening leaves the selection to the focus service (first selected in Navigation mode, none in Pointer mode).
            _showing = true;
            try
            {
                _tabs.Select(0);
            }
            finally
            {
                _showing = false;
            }

            _currentTab = _tabs.SelectedIndex;
            Redraw();
            if (rebuilt && _focus != null)
            {
                // New rows under the popup's scope: let the focus service pick the selection again.
                _focus.Refocus();
            }
        }

        protected override void OnHidden()
        {
            _isOpen = false;
            foreach (IDisposable binding in _bindings)
            {
                binding.Dispose();
            }

            _bindings.Clear();
            if (_cancelStack != null)
            {
                _cancelStack.Remove(_cancelHandler);
            }

            if (_rebinder != null)
            {
                _rebinder.Cancel();
            }

            RestoreNavigationNow();
            _rebindBlocker.SetActive(false);
            if (_settings != null)
            {
                // Closed without answering "Apply changes?" (scene change, every popup hidden): staged values are dropped.
                foreach (string tabId in _tabIds)
                {
                    _settings.RevertTab(tabId);
                }
            }
        }

        private void Update()
        {
            if (!_isOpen)
            {
                return;
            }

            if (_navigationBlocked && _restoreNavigationFrame >= 0 && Time.frameCount >= _restoreNavigationFrame)
            {
                RestoreNavigationNow();
            }

            UpdateDescription();
            HandleShortcuts();
        }

        // True when any page (re)built its rows.
        private bool BuildPages()
        {
            if (_settings == null)
            {
                return false;
            }

            bool rebuilt = false;
            string scheme = _context.Glyphs != null ? _context.Glyphs.CurrentControlScheme : string.Empty;
            foreach (SettingsPage page in _pages)
            {
                rebuilt |= page.Build(_context, _settings.GetDefinitions(page.TabId), PrefabFor);
                // Each opening starts on the bindings of the device in use.
                page.SelectScheme(scheme);
            }

            if (!_warnedTabs)
            {
                _warnedTabs = true;
                foreach (SettingsTab tab in _settings.Tabs)
                {
                    if (!_tabIds.Contains(tab.Id))
                    {
                        Debug.LogWarning($"[Game] Settings tab '{tab.Id}' has no page in the settings popup; run Game › Build All.", this);
                    }
                }
            }

            return rebuilt;
        }

        private SettingRow PrefabFor(SettingRowKind kind)
        {
            switch (kind)
            {
                case SettingRowKind.Toggle:
                    return _toggleRow;
                case SettingRowKind.Slider:
                    return _sliderRow;
                case SettingRowKind.Choice:
                    return _choiceRow;
                case SettingRowKind.KeyBinding:
                    return _keyBindingRow;
                case SettingRowKind.Button:
                    return _buttonRow;
                default:
                    return null;
            }
        }

        private void OnTabChanged(int index)
        {
            string previousTab = CurrentTabId;
            _currentTab = index;
            Redraw();
            if (!_showing && _focus != null && index < _tabButtons.Count && _tabButtons[index] != null)
            {
                _focus.Select(_tabButtons[index].gameObject);
            }

            // The page has already switched; the question is about the tab just left.
            if (!_showing && _pending != null && previousTab.Length > 0 && _settings.HasPendingChanges(previousTab))
            {
                _ = ResolvePendingAsync(new[] { previousTab });
            }
        }

        // Everything visible, from state: tab visuals, the page's rows, the footer and every navigation link.
        private void Redraw()
        {
            if (!_isOpen)
            {
                return;
            }

            for (int i = 0; i < _tabBackgrounds.Count; i++)
            {
                if (_tabBackgrounds[i] != null)
                {
                    _tabBackgrounds[i].sprite = i == _currentTab ? _selectedTabSprite : _idleTabSprite;
                }
            }

            SettingsPage page = CurrentPage;
            if (page != null)
            {
                page.Redraw();
            }

            bool pending = _settings != null && _settings.HasPendingChanges(CurrentTabId);
            _applyButton.interactable = pending;
            _revertButton.interactable = pending;
            _applyPrompt.SetActive(pending);
            _revertPrompt.SetActive(pending);
            _describedObject = null;
            Relink(page);
            KeepFooterSelection();
        }

        // Apply or Revert just disabled themselves (or the selection sat on them): selection moves to Defaults, beside them.
        private void KeepFooterSelection()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (_focus != null && selected != null &&
                ((selected == _applyButton.gameObject && !_applyButton.interactable) ||
                 (selected == _revertButton.gameObject && !_revertButton.interactable)))
            {
                _focus.Select(_defaultsButton.gameObject);
            }
        }

        private void Relink(SettingsPage page)
        {
            _footer.Clear();
            foreach (Button button in new[] { _applyButton, _revertButton, _defaultsButton, _closeButton })
            {
                if (button.gameObject.activeSelf && button.interactable)
                {
                    _footer.Add(button);
                }
            }

            Selectable tab = _currentTab < _tabButtons.Count ? _tabButtons[_currentTab] : null;
            Selectable footerFirst = _footer.Count > 0 ? _footer[0] : null;
            Selectable pageFirst = null;
            Selectable pageLast = null;
            if (page != null)
            {
                page.Link(tab, footerFirst);
                pageFirst = page.First;
                pageLast = page.Last;
            }

            Selectable belowTabs = pageFirst != null ? pageFirst : footerFirst;
            foreach (Button tabButton in _tabButtons)
            {
                // Tabs have no left/right neighbour: SettingsTabButton steps the page instead.
                SetNavigation(tabButton, null, belowTabs, null, null);
            }

            Selectable aboveFooter = pageLast != null ? pageLast : tab;
            for (int i = 0; i < _footer.Count; i++)
            {
                SetNavigation(_footer[i], aboveFooter, null, i > 0 ? _footer[i - 1] : null, i < _footer.Count - 1 ? _footer[i + 1] : null);
            }

            foreach (Button button in new[] { _applyButton, _revertButton })
            {
                if (!button.interactable)
                {
                    SetNavigation(button, aboveFooter, null, null, null);
                }
            }
        }

        private static void SetNavigation(Selectable selectable, Selectable up, Selectable down, Selectable left, Selectable right)
        {
            Navigation navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            navigation.selectOnLeft = left;
            navigation.selectOnRight = right;
            selectable.navigation = navigation;
        }

        private void UpdateDescription()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == _describedObject)
            {
                return;
            }

            _describedObject = selected;
            SettingsPage page = CurrentPage;
            SettingDefinition definition = selected != null && page != null ? page.Describe(selected) : null;
            _descriptionPanel.SetActive(definition != null);
            if (definition == null)
            {
                return;
            }

            _descriptionTitle.SetKey(definition.LabelKey);
            _descriptionBody.gameObject.SetActive(definition.DescriptionKey.Length > 0);
            if (definition.DescriptionKey.Length > 0)
            {
                _descriptionBody.SetKey(definition.DescriptionKey);
            }
        }

        private void HandleShortcuts()
        {
            if (!_shortcutsValid || _input.IsRebinding || !OwnsFocus() || (_pending != null && _pending.IsAsking) ||
                (_rebinder != null && _rebinder.CurrentPhase != KeyBindingRebinder.Phase.Idle))
            {
                return;
            }

            if (_input.WasPressedThisFrame(_applyAction) && _applyButton.interactable)
            {
                Apply();
            }
            else if (_input.WasPressedThisFrame(_revertAction) && _revertButton.interactable)
            {
                Revert();
            }
            else if (_input.WasPressedThisFrame(_defaultsAction))
            {
                _ = DefaultsAsync();
            }
        }

        // True while this popup's scope (or one inside it) is on top: no dialog over it.
        private bool OwnsFocus()
        {
            if (_focus == null)
            {
                return true;
            }

            FocusScope top = _focus.TopScope;
            return top != null && top.transform.IsChildOf(transform);
        }

        private void Apply()
        {
            if (_settings != null && _settings.HasPendingChanges(CurrentTabId))
            {
                // A display change shows "Keep these display settings?" once the new mode is visible.
                _settings.ApplyTab(CurrentTabId);
            }
        }

        private void Revert()
        {
            if (_settings != null)
            {
                _settings.RevertTab(CurrentTabId);
            }
        }

        private async Awaitable DefaultsAsync()
        {
            string tabId = CurrentTabId;
            if (_settings == null || tabId.Length == 0)
            {
                return;
            }

            try
            {
                var request = new ConfirmRequest(_defaultsTitleKey, _defaultsMessageKey, _defaultsConfirmKey, _cancelKey,
                    Loc.Get(TabLabelKey(tabId)));
                if (await Confirm(request) && _isOpen)
                {
                    // Immediate settings change now, deferred ones are staged (Apply), key bindings get their defaults.
                    _settings.ResetTabToDefaults(tabId);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private async Awaitable CloseAsync()
        {
            try
            {
                if (_pending == null || await ResolvePendingAsync(_tabIds))
                {
                    Close();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private async Awaitable<bool> ResolvePendingAsync(IReadOnlyList<string> tabIds)
        {
            try
            {
                return await _pending.ResolveAsync(tabIds);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return false;
            }
        }

        private void Rebind(KeyBindingSettingDefinition definition)
        {
            if (_rebinder != null && _rebinder.CurrentPhase == KeyBindingRebinder.Phase.Idle && _isOpen)
            {
                _ = RebindAsync(definition);
            }
        }

        private async Awaitable RebindAsync(KeyBindingSettingDefinition definition)
        {
            try
            {
                await _rebinder.RebindAsync(definition);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void OnRebindPhaseChanged()
        {
            bool listening = _rebinder.CurrentPhase == KeyBindingRebinder.Phase.Listening;
            if (listening && _isOpen)
            {
                BlockNavigation();
            }
            else if (_navigationBlocked)
            {
                // The frame after: the press that ended the rebind must not also reach the UI as Submit or a move.
                _restoreNavigationFrame = Time.frameCount + 1;
            }

            _rebindBlocker.SetActive(listening && _isOpen);
            Redraw();
        }

        private void BlockNavigation()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            if (!_navigationBlocked)
            {
                _navigationBlocked = true;
                _navigationWasEnabled = eventSystem.sendNavigationEvents;
            }

            eventSystem.sendNavigationEvents = false;
            _restoreNavigationFrame = -1;
        }

        private void RestoreNavigationNow()
        {
            if (!_navigationBlocked)
            {
                return;
            }

            _navigationBlocked = false;
            _restoreNavigationFrame = -1;
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.sendNavigationEvents = _navigationWasEnabled;
            }
        }

        private void InvokeButton(ButtonSettingDefinition button)
        {
            _ = InvokeButtonAsync(button);
        }

        private async Awaitable InvokeButtonAsync(ButtonSettingDefinition button)
        {
            try
            {
                string message = button.DescriptionKey.Length > 0 ? button.DescriptionKey : _buttonMessageKey;
                if (await Confirm(new ConfirmRequest(button.LabelKey, message, _buttonConfirmKey, _cancelKey)) && _isOpen)
                {
                    _settings.Invoke(button.Id);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private Awaitable<bool> Confirm(ConfirmRequest request)
        {
            if (_confirm != null)
            {
                return _confirm.ConfirmAsync(request);
            }

            // Without the dialog nothing destructive happens: every question answers no.
            var declined = new AwaitableCompletionSource<bool>();
            declined.SetResult(false);
            return declined.Awaitable;
        }

        private ConfirmRequest SwapRequest(InputBindingInfo conflict)
        {
            string control = conflict.DisplayString.Length > 0 ? conflict.DisplayString : conflict.EffectivePath;
            return new ConfirmRequest(_swapTitleKey, _swapMessageKey, _swapConfirmKey, _cancelKey, control, ActionLabel(conflict));
        }

        // The label of the row showing the other binding, else the action's name.
        private string ActionLabel(InputBindingInfo conflict)
        {
            if (_settings != null)
            {
                foreach (string tabId in _tabIds)
                {
                    foreach (SettingDefinition definition in _settings.GetDefinitions(tabId))
                    {
                        if (definition is KeyBindingSettingDefinition binding &&
                            string.Equals(binding.Action, conflict.Action, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(binding.CompositePart, conflict.PartName, StringComparison.OrdinalIgnoreCase) &&
                            (binding.ControlScheme.Length == 0 || conflict.IsInGroup(binding.ControlScheme)))
                        {
                            return Loc.Get(binding.LabelKey);
                        }
                    }
                }
            }

            return conflict.Action;
        }

        private string TabLabelKey(string tabId)
        {
            foreach (SettingsTab tab in _settings.Tabs)
            {
                if (tab.Id == tabId)
                {
                    return tab.LabelKey;
                }
            }

            return tabId;
        }

        private void OnVolumeChanged(AudioVolumeChangedEvent evt)
        {
            if (_volumePreview == null || _sfx == null || Time.unscaledTime < _nextPreviewTime)
            {
                return;
            }

            _nextPreviewTime = Time.unscaledTime + _previewInterval;
            // Master scales every category; the preview plays as UI sound for it.
            _sfx.PlaySfx(_volumePreview, evt.Category == AudioCategory.Master ? AudioCategory.UI : evt.Category);
        }

        // Cancel/Back with staged values asks "Apply changes?" instead of closing at once.
        private bool TryCancel()
        {
            if (!_isOpen || _pending == null || _pending.IsAsking || !OwnsFocus() || !_pending.HasPending(_tabIds))
            {
                return false;
            }

            _ = CloseAsync();
            return true;
        }

        private sealed class CancelHandler : IUICancelHandler
        {
            private readonly SettingsPopup _popup;

            internal CancelHandler(SettingsPopup popup)
            {
                _popup = popup;
            }

            public int Priority => CancelPriority;

            public bool TryCancel()
            {
                return _popup.TryCancel();
            }
        }
    }
}

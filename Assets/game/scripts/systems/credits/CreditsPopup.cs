using System.Collections.Generic;
using Game.Contracts.Presentation;
using Game.Foundation.Localization;
using Game.Foundation.UI;
using Paowa.Core.Foundation.Services;
using Paowa.Input;
using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Systems.Credits
{
    /// <summary>
    /// The credits popup: a list built once from <see cref="CreditsData"/> (one title per section, one row per entry) inside
    /// a clipped viewport, scrolled by a <see cref="CreditsScroll"/>. Each open starts at the top; with animations on the
    /// list then scrolls by itself, with animations off it stays still. Navigate (arrows, WASD, stick, d-pad) and the mouse
    /// wheel scroll by hand while this popup owns focus and uGUI navigation is on (off while the loading screen, a rebind
    /// or the dev console holds input). The list position is drawn from the scroll state every frame. Cancel (Esc, B) or
    /// Close closes it. The keyboard scroll prompt shows on keyboard/mouse, the stick prompt on a gamepad.
    /// REFERENCES: <c>_data</c> is the content; <c>_viewport</c> clips <c>_list</c> (top-anchored, sized by its layout);
    /// <c>_sectionTemplate</c> (a localized title) and <c>_entryTemplate</c> are inactive templates outside the list;
    /// <c>_closeButton</c>; <c>_keysPrompt</c>/<c>_stickPrompt</c> are the two scroll prompts.
    /// </summary>
    internal sealed class CreditsPopup : PopupBase
    {
        [SerializeField] private CreditsData _data;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _list;
        [SerializeField] private LocalizedLabel _sectionTemplate;
        [SerializeField] private CreditsEntryView _entryTemplate;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _keysPrompt;
        [SerializeField] private GameObject _stickPrompt;
        [SerializeField] private string _scrollAction = "UI/Navigate";
        [SerializeField] private string _wheelAction = "UI/ScrollWheel";

        private readonly List<GameObject> _rows = new List<GameObject>();
        private CreditsScroll _scroll;
        private IInputService _input;
        private IUIFocusService _focus;
        private IAnimationSettings _animations;
        private bool _built;
        private bool _scrollValid;
        private bool _wheelValid;

        protected override void Awake()
        {
            base.Awake();
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _focus);
            CoreServiceLocator.TryGet(out _animations);
            _scrollValid = _input != null && _input.HasAction(_scrollAction);
            _wheelValid = _input != null && _input.HasAction(_wheelAction);
            _scroll = _data != null
                ? new CreditsScroll(_data.AutoScrollSpeed, _data.ManualScrollSpeed, _data.WheelStep, _data.StartDelaySeconds,
                    _data.ResumeDelaySeconds)
                : new CreditsScroll(0f, 0f, 0f, 0f, 0f);
            _closeButton.onClick.AddListener(Close);
        }

        protected override void OnShown(object payload)
        {
            if (!_built)
            {
                Build();
            }

            _scroll.Reset();
            Draw();
        }

        private void LateUpdate()
        {
            float axis = 0f;
            float wheel = 0f;
            if (AcceptsInput())
            {
                if (_scrollValid)
                {
                    float y = _input.ReadVector2(_scrollAction).y;
                    axis = Mathf.Abs(y) < (_data != null ? _data.StickDeadZone : 0f) ? 0f : -y;
                }

                if (_wheelValid)
                {
                    float y = _input.ReadVector2(_wheelAction).y;
                    wheel = y == 0f ? 0f : -Mathf.Sign(y);
                }
            }

            _scroll.Tick(Time.unscaledDeltaTime, axis, wheel, _animations == null || _animations.Enabled);
            Draw();
        }

        // Every frame from state: sizes (language and text size change them), list position, the prompt of the device in use.
        private void Draw()
        {
            _scroll.SetExtent(_list.rect.height, _viewport.rect.height);
            _list.anchoredPosition = new Vector2(_list.anchoredPosition.x, _scroll.Offset);
            bool gamepad = _input != null && _input.CurrentDevice == InputDeviceKind.Gamepad;
            SetActive(_keysPrompt, !gamepad);
            SetActive(_stickPrompt, gamepad);
        }

        private void Build()
        {
            _built = true;
            foreach (GameObject row in _rows)
            {
                Destroy(row);
            }

            _rows.Clear();
            if (_data == null)
            {
                Debug.LogWarning("[Game] The credits popup has no credits data.", this);
                return;
            }

            foreach (CreditsSection section in _data.Sections)
            {
                if (section == null)
                {
                    continue;
                }

                if (section.TitleKey.Length > 0)
                {
                    LocalizedLabel title = Instantiate(_sectionTemplate, _list);
                    title.gameObject.SetActive(true);
                    title.SetKey(section.TitleKey);
                    _rows.Add(title.gameObject);
                }

                foreach (CreditsEntry entry in section.Entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    CreditsEntryView view = Instantiate(_entryTemplate, _list);
                    view.gameObject.SetActive(true);
                    view.Show(entry);
                    _rows.Add(view.gameObject);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_list);
        }

        // Input belongs to the credits only while their scope is on top and uGUI navigation is on.
        private bool AcceptsInput()
        {
            EventSystem current = EventSystem.current;
            if (_input == null || current == null || !current.sendNavigationEvents)
            {
                return false;
            }

            if (_focus == null)
            {
                return true;
            }

            FocusScope top = _focus.TopScope;
            return top != null && top.transform.IsChildOf(transform);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}

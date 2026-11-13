using System.Collections.Generic;
using Game.Foundation.Localization;
using Game.Foundation.UI;
using Paowa.Core.Foundation.Services;
using Paowa.Platform;
using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Achievements
{
    /// <summary>
    /// The achievements popup: one row per definition of the platform's achievement catalog (built once, in catalog order)
    /// and the unlocked count. Every open redraws all rows from <see cref="IPlatformService.IsAchievementUnlocked"/>, scrolls
    /// to the top and selects the first row (Close when the catalog is empty or missing). No animation of its own. Cancel
    /// (Esc, B) or Close closes it.
    /// REFERENCES: <c>_scroll</c> scrolls <c>_list</c> (a vertical layout, kept in view of the selection by
    /// <c>ScrollToSelected</c>); <c>_rowTemplate</c> is an inactive row outside the list; <c>_progress</c> the "x / y" text;
    /// <c>_emptyLabel</c> shows when there is no achievement; <c>_closeButton</c>.
    /// </summary>
    internal sealed class AchievementsPopup : PopupBase
    {
        private const string ProgressKey = "achievements.progress";

        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _list;
        [SerializeField] private AchievementRowView _rowTemplate;
        [SerializeField] private LocalizedLabel _progress;
        [SerializeField] private GameObject _emptyLabel;
        [SerializeField] private Button _closeButton;

        private readonly List<(AchievementDefinition definition, AchievementRowView view)> _rows =
            new List<(AchievementDefinition, AchievementRowView)>();
        private IPlatformService _platform;
        private IUIFocusService _focus;
        private FocusScope _scope;
        private bool _built;

        protected override void Awake()
        {
            base.Awake();
            CoreServiceLocator.TryGet(out _platform);
            CoreServiceLocator.TryGet(out _focus);
            _scope = GetComponent<FocusScope>();
            _closeButton.onClick.AddListener(Close);
        }

        protected override void OnShown(object payload)
        {
            if (!_built)
            {
                Build();
            }

            int unlocked = 0;
            foreach ((AchievementDefinition definition, AchievementRowView view) in _rows)
            {
                bool isUnlocked = _platform.IsAchievementUnlocked(definition.Id);
                unlocked += isUnlocked ? 1 : 0;
                view.Show(AchievementDisplay.For(definition.NameKey, definition.DescriptionKey, definition.Hidden, isUnlocked),
                    definition.Icon);
            }

            _progress.SetFormat(ProgressKey, unlocked, _rows.Count);
            _emptyLabel.SetActive(_rows.Count == 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_list);
            _scroll.verticalNormalizedPosition = 1f;
            Selectable first = _rows.Count > 0 ? _rows[0].view.Selectable : _closeButton;
            if (_scope != null)
            {
                _scope.FirstSelected = first;
            }

            if (_focus != null)
            {
                _focus.Select(first.gameObject);
            }
        }

        // Rows in catalog order, linked top to bottom and then to Close (Up from Close returns to the last row).
        private void Build()
        {
            _built = true;
            AchievementCatalog catalog = _platform != null ? _platform.Achievements : null;
            if (catalog == null)
            {
                Debug.LogWarning("[Game] No achievement catalog is assigned in PlatformSettings; the achievements list is empty.", this);
                return;
            }

            foreach (AchievementDefinition definition in catalog.Achievements)
            {
                if (definition == null || definition.Id.Length == 0)
                {
                    continue;
                }

                AchievementRowView view = Instantiate(_rowTemplate, _list);
                view.gameObject.SetActive(true);
                _rows.Add((definition, view));
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                Selectable up = i > 0 ? _rows[i - 1].view.Selectable : null;
                Selectable down = i + 1 < _rows.Count ? _rows[i + 1].view.Selectable : _closeButton;
                Link(_rows[i].view.Selectable, up, down);
            }

            Link(_closeButton, _rows.Count > 0 ? _rows[_rows.Count - 1].view.Selectable : null, null);
        }

        private static void Link(Selectable target, Selectable up, Selectable down)
        {
            Navigation navigation = target.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            target.navigation = navigation;
        }
    }
}

using System;
using System.Collections.Generic;
using Paowa.Core.Foundation.Services;
using Paowa.Settings;
using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Settings
{
    /// <summary>
    /// One tab page of the settings popup: a scrolling list of rows built from the tab's definitions
    /// (<see cref="SettingRowPlanner"/>, one row prefab per kind) and, on a page with key bindings, a control scheme bar
    /// (Keyboard &amp; Mouse / Controller) that filters the scheme rows; rows of no particular scheme always show. Rows are
    /// built once and rebuilt only when the tab's definitions change (a catalog added at runtime). Navigation is explicit and
    /// rewritten whenever the visible rows change: the scheme bar, then the rows top to bottom (a row with several columns
    /// keeps the column when moving up or down), with the popup's tab above and its footer below.
    /// REFERENCES: <c>_content</c> is the list the rows are created under (vertical layout inside a scroll view with
    /// <c>ScrollToSelected</c>); <c>_schemes</c>, <c>_schemeButtons</c> and <c>_schemeBackgrounds</c> pair by index, drawn with
    /// <c>_selectedSchemeSprite</c>/<c>_idleSchemeSprite</c>; empty lists mean no scheme bar.
    /// </summary>
    internal sealed class SettingsPage : MonoBehaviour
    {
        [SerializeField] private string _tabId;
        [SerializeField] private RectTransform _content;
        [SerializeField] private List<string> _schemes = new List<string>();
        [SerializeField] private List<Button> _schemeButtons = new List<Button>();
        [SerializeField] private List<Image> _schemeBackgrounds = new List<Image>();
        [SerializeField] private Sprite _selectedSchemeSprite;
        [SerializeField] private Sprite _idleSchemeSprite;

        private readonly List<SettingRow> _rows = new List<SettingRow>();
        private readonly List<SettingDefinition> _built = new List<SettingDefinition>();
        private readonly List<List<Selectable>> _grid = new List<List<Selectable>>();
        private IUIFocusService _focus;
        private string _scheme = string.Empty;
        private Selectable _above;
        private Selectable _below;

        /// <summary>
        /// Raised after the player picked another scheme and the visible rows changed; the popup then rewrites the page's and
        /// the footer's navigation (the footer's up link points at the page's last row).
        /// </summary>
        internal event Action SchemeChanged;

        /// <summary>Id of the settings tab shown by this page.</summary>
        internal string TabId => _tabId ?? string.Empty;

        /// <summary>First navigable control (top of the page); null when the page has none.</summary>
        internal Selectable First => _grid.Count > 0 ? _grid[0][0] : null;

        /// <summary>Navigable control at the bottom of the page (first column); null when the page has none.</summary>
        internal Selectable Last => _grid.Count > 0 ? _grid[_grid.Count - 1][0] : null;

        /// <summary>
        /// Creates the rows of <paramref name="definitions"/> unless they are already built; true when rows were (re)built.
        /// </summary>
        internal bool Build(SettingRowContext context, IReadOnlyList<SettingDefinition> definitions, Func<SettingRowKind, SettingRow> prefabFor)
        {
            if (IsBuilt(definitions))
            {
                return false;
            }

            foreach (SettingRow row in _rows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _rows.Clear();
            _built.Clear();
            _built.AddRange(definitions);
            foreach (SettingRowPlan plan in SettingRowPlanner.Plan(definitions))
            {
                SettingRow prefab = prefabFor(plan.Kind);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Game] Settings tab '{TabId}' has no row prefab for {plan.Kind} ('{plan.Definitions[0].Id}').", this);
                    continue;
                }

                SettingRow row = Instantiate(prefab, _content);
                row.name = plan.Definitions[0].Id;
                row.Bind(context, plan);
                _rows.Add(row);
            }

            ApplyFilter();
            return true;
        }

        /// <summary>Shows the rows of <paramref name="scheme"/> (ignored when the page has no such scheme); the first scheme otherwise.</summary>
        internal void SelectScheme(string scheme)
        {
            _scheme = _schemes.Count == 0 ? string.Empty : _schemes.Contains(scheme) ? scheme : _schemes[0];
            ApplyFilter();
        }

        /// <summary>Redraws every visible row from the current state.</summary>
        internal void Redraw()
        {
            foreach (SettingRow row in _rows)
            {
                if (row != null && row.gameObject.activeSelf)
                {
                    row.Redraw();
                }
            }
        }

        /// <summary>Rewrites the page's navigation between <paramref name="above"/> (the tab) and <paramref name="below"/> (the footer).</summary>
        internal void Link(Selectable above, Selectable below)
        {
            _above = above;
            _below = below;
            _grid.Clear();
            var schemeRow = new List<Selectable>();
            for (int i = 0; i < _schemeButtons.Count; i++)
            {
                AddIfShown(schemeRow, _schemeButtons[i]);
            }

            if (schemeRow.Count > 0)
            {
                _grid.Add(schemeRow);
            }

            foreach (SettingRow row in _rows)
            {
                if (row == null || !row.gameObject.activeSelf)
                {
                    continue;
                }

                var columns = new List<Selectable>();
                for (int i = 0; i < row.ColumnCount; i++)
                {
                    AddIfShown(columns, row.GetColumn(i));
                }

                if (columns.Count > 0)
                {
                    _grid.Add(columns);
                }
            }

            for (int r = 0; r < _grid.Count; r++)
            {
                List<Selectable> columns = _grid[r];
                bool horizontal = (r == 0 && schemeRow.Count > 0) || columns.Count > 1;
                for (int c = 0; c < columns.Count; c++)
                {
                    Navigation navigation = columns[c].navigation;
                    navigation.mode = Navigation.Mode.Explicit;
                    navigation.selectOnUp = r > 0 ? Pick(_grid[r - 1], c) : _above;
                    navigation.selectOnDown = r < _grid.Count - 1 ? Pick(_grid[r + 1], c) : _below;
                    // Single-column rows keep left/right for themselves (slider, stepper, toggle).
                    navigation.selectOnLeft = horizontal && c > 0 ? columns[c - 1] : null;
                    navigation.selectOnRight = horizontal && c < columns.Count - 1 ? columns[c + 1] : null;
                    columns[c].navigation = navigation;
                }
            }
        }

        /// <summary>Definition to describe while <paramref name="selected"/> is selected; null when it is not one of the rows' controls.</summary>
        internal SettingDefinition Describe(GameObject selected)
        {
            foreach (SettingRow row in _rows)
            {
                if (row != null && row.ColumnOf(selected) >= 0)
                {
                    return row.DefinitionFor(selected);
                }
            }

            return null;
        }

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _focus);
            for (int i = 0; i < _schemeButtons.Count; i++)
            {
                int index = i;
                _schemeButtons[i].onClick.AddListener(() => OnSchemePressed(index));
            }
        }

        private void OnSchemePressed(int index)
        {
            if (index >= _schemes.Count || _schemes[index] == _scheme)
            {
                return;
            }

            _scheme = _schemes[index];
            ApplyFilter();
            Redraw();
            if (SchemeChanged != null)
            {
                SchemeChanged();
            }
            else
            {
                Link(_above, _below);
            }

            if (_focus != null)
            {
                // The rows changed under the selection: keep it on the scheme button just pressed.
                _focus.Select(_schemeButtons[index].gameObject);
                _focus.Refocus();
            }
        }

        private bool IsBuilt(IReadOnlyList<SettingDefinition> definitions)
        {
            if (definitions.Count != _built.Count)
            {
                return false;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != _built[i])
                {
                    return false;
                }
            }

            return _built.Count == 0 || _rows.Count > 0;
        }

        private void ApplyFilter()
        {
            foreach (SettingRow row in _rows)
            {
                if (row != null)
                {
                    row.gameObject.SetActive(_schemes.Count == 0 || row.Scheme.Length == 0 ||
                                             string.Equals(row.Scheme, _scheme, StringComparison.OrdinalIgnoreCase));
                }
            }

            for (int i = 0; i < _schemeBackgrounds.Count; i++)
            {
                if (_schemeBackgrounds[i] != null)
                {
                    _schemeBackgrounds[i].sprite = i < _schemes.Count && _schemes[i] == _scheme ? _selectedSchemeSprite : _idleSchemeSprite;
                }
            }
        }

        private static void AddIfShown(List<Selectable> list, Selectable selectable)
        {
            if (selectable != null && selectable.gameObject.activeSelf)
            {
                list.Add(selectable);
            }
        }

        private static Selectable Pick(List<Selectable> columns, int column)
        {
            return columns[Mathf.Min(column, columns.Count - 1)];
        }
    }
}

using System.Collections.Generic;
using Game.Foundation.InputPrompts;
using Game.Foundation.Localization;
using Paowa.Gamepad;
using Paowa.Input;
using Paowa.Input.Rebinding;
using Paowa.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Rebind row: one slot button per definition (primary, secondary), each showing the glyph of its binding for its own
    /// scheme (keyboard/mouse glyphs on a keyboard row even while a gamepad is in use; gamepad rows use the last gamepad
    /// family). Submit or a click on a slot asks the popup to rebind it; while that slot listens it shows the "Press a key…
    /// (Esc/Start to cancel)" text of its scheme. A slot whose binding does not exist (or a backend that cannot rebind)
    /// shows <c>_unboundKey</c>; an existing binding without a control (an unused secondary slot) shows it too but stays
    /// rebindable. Everything is redrawn from the input service and the rebind state.
    /// REFERENCES: <c>_slots</c>, <c>_glyphs</c> and <c>_texts</c> pair by index (button, glyph view, state text); slots past
    /// the row's definitions are hidden.
    /// </summary>
    internal sealed class KeyBindingSettingRow : SettingRow
    {
        [SerializeField] private List<Button> _slots = new List<Button>();
        [SerializeField] private List<InputGlyphView> _glyphs = new List<InputGlyphView>();
        [SerializeField] private List<TMP_Text> _texts = new List<TMP_Text>();
        [SerializeField] private string _unboundKey;
        [SerializeField] private string _listeningKeyboardKey;
        [SerializeField] private string _listeningGamepadKey;

        internal override int ColumnCount => Mathf.Min(Definitions.Count, _slots.Count);

        internal override void Redraw()
        {
            if (Context == null)
            {
                return;
            }

            for (int i = 0; i < ColumnCount; i++)
            {
                RedrawSlot(i, Definitions[i] as KeyBindingSettingDefinition);
            }
        }

        internal override Selectable GetColumn(int column)
        {
            return column >= 0 && column < ColumnCount ? _slots[column] : null;
        }

        internal override SettingDefinition DefinitionFor(GameObject selected)
        {
            int column = ColumnOf(selected);
            return column >= 0 ? Definitions[column] : Definition;
        }

        protected override void OnBind()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].gameObject.SetActive(i < Definitions.Count);
            }
        }

        private void Awake()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                int column = i;
                _slots[i].onClick.AddListener(() => OnSlotPressed(column));
            }
        }

        private void OnSlotPressed(int column)
        {
            if (Context != null && column < ColumnCount && Definitions[column] is KeyBindingSettingDefinition binding)
            {
                Context.Rebind(binding);
            }
        }

        private void RedrawSlot(int index, KeyBindingSettingDefinition binding)
        {
            bool gamepad = binding != null && string.Equals(binding.ControlScheme, Context.GamepadScheme,
                System.StringComparison.OrdinalIgnoreCase);
            KeyBindingRebinder rebinder = Context.Rebinder;
            if (binding != null && rebinder != null && rebinder.CurrentPhase == KeyBindingRebinder.Phase.Listening &&
                rebinder.Target == binding)
            {
                ShowText(index, gamepad ? _listeningGamepadKey : _listeningKeyboardKey, true);
                return;
            }

            if (binding == null || Context.Input == null || !binding.TryFindBinding(Context.Input, out InputBindingInfo info))
            {
                ShowText(index, _unboundKey, false);
                return;
            }

            PromptGlyph glyph = InputPromptResolver.FromGlyph(GlyphOf(info, gamepad));
            _slots[index].interactable = Context.Rebinder != null && Context.Rebinder.SupportsRebinding;
            if (!glyph.IsVisible)
            {
                ShowText(index, _unboundKey, Context.Rebinder != null && Context.Rebinder.SupportsRebinding);
                return;
            }

            _texts[index].gameObject.SetActive(false);
            _glyphs[index].Show(glyph);
        }

        private InputGlyph GlyphOf(InputBindingInfo info, bool gamepad)
        {
            if (Context.Glyphs == null)
            {
                return new InputGlyph(null, info.DisplayString, null);
            }

            GamepadFamily family = gamepad && Context.Glyphs != null ? Context.Glyphs.LastGamepadFamily : GamepadFamily.None;
            return Context.Glyphs.GetGlyph(info, gamepad ? InputDeviceKind.Gamepad : InputDeviceKind.KeyboardMouse, family);
        }

        private void ShowText(int index, string key, bool interactable)
        {
            _glyphs[index].Show(PromptGlyph.Hidden);
            _texts[index].gameObject.SetActive(true);
            _texts[index].text = Loc.Get(key);
            _slots[index].interactable = interactable;
        }
    }
}

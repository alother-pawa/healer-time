using System;
using System.Collections.Generic;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Gamepad;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Foundation.InputPrompts
{
    /// <summary>
    /// A button prompt: one glyph per action (e.g. "Menu/TabPrevious" and "Menu/TabNext" for "Switch tab") followed by a
    /// localized label (a <c>LocalizedLabel</c> on a child). Glyphs are resolved from the current state each time the prompt
    /// is enabled and on <see cref="InputGlyphsChangedEvent"/> (device family changed, binding changed, glyph set replaced),
    /// so the prompt never depends on having seen an earlier event. When no action has a binding for the device in use the
    /// whole prompt hides and leaves its layout group; it stays active, so it keeps listening and shows again on its own.
    /// REFERENCES: <c>_actions</c> and <c>_glyphs</c> pair up by index; <c>_label</c> is the label object; <c>_layoutElement</c>
    /// (on this object) is taken out of the parent layout while the prompt is hidden.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputPrompt : MonoBehaviour
    {
        [Tooltip("Actions shown, \"Context/Action\" or \"Context/Action/part\" for a composite part.")]
        [SerializeField] private List<string> _actions = new List<string>();
        [SerializeField] private List<InputGlyphView> _glyphs = new List<InputGlyphView>();
        [SerializeField] private GameObject _label;
        [SerializeField] private LayoutElement _layoutElement;

        private IInputGlyphs _inputGlyphs;
        private IDisposable _binding;

        /// <summary>Resolves and shows every glyph again.</summary>
        public void Refresh()
        {
            bool anyVisible = false;
            for (int i = 0; i < _glyphs.Count; i++)
            {
                PromptGlyph glyph = i < _actions.Count ? InputPromptResolver.Resolve(_inputGlyphs, _actions[i]) : PromptGlyph.Hidden;
                if (_glyphs[i] != null)
                {
                    _glyphs[i].Show(glyph);
                }

                anyVisible |= glyph.IsVisible;
            }

            if (_label != null)
            {
                _label.SetActive(anyVisible);
            }

            if (_layoutElement != null)
            {
                _layoutElement.ignoreLayout = !anyVisible;
            }
        }

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _inputGlyphs);
        }

        private void OnEnable()
        {
            if (CoreServiceLocator.TryGet(out IEventBus events))
            {
                _binding = events.Subscribe<InputGlyphsChangedEvent>(_ => Refresh());
            }

            Refresh();
        }

        private void OnDisable()
        {
            _binding?.Dispose();
            _binding = null;
        }
    }
}

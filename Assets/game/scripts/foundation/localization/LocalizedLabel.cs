using System;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Localization;
using TMPro;
using UnityEngine;

namespace Game.Foundation.Localization
{
    /// <summary>
    /// Shows the localized text of a key on a TextMeshPro label and writes it again whenever the language changes, so
    /// labels follow the language picked in the settings at once. Nothing but an empty text shows while the localization
    /// module is still loading its first language. Code may switch the key or show a formatted text
    /// (<see cref="SetFormat"/>); the arguments are kept and formatted again in the new language. Core's own
    /// <c>LocalizedText</c> drives a uGUI <c>Text</c>; this is the TextMeshPro counterpart.
    /// REFERENCES: <c>_label</c> is the text written (the TextMeshPro text on this object when unwired).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalizedLabel : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private TMP_Text _label;

        private object[] _arguments;
        private IDisposable _languageBinding;

        /// <summary>Key shown by the label.</summary>
        public string Key => _key;

        /// <summary>Shows <paramref name="key"/>.</summary>
        public void SetKey(string key)
        {
            _key = key;
            _arguments = null;
            Refresh();
        }

        /// <summary>Shows <paramref name="key"/> formatted with <paramref name="arguments"/>.</summary>
        public void SetFormat(string key, params object[] arguments)
        {
            _key = key;
            _arguments = arguments;
            Refresh();
        }

        private void OnEnable()
        {
            if (CoreServiceLocator.TryGet(out IEventBus events))
            {
                _languageBinding = events.Subscribe<LanguageChangedEvent>(_ => Refresh());
            }

            Refresh();
        }

        private void OnDisable()
        {
            _languageBinding?.Dispose();
            _languageBinding = null;
        }

        private void Refresh()
        {
            if (string.IsNullOrEmpty(_key))
            {
                return;
            }

            if (_label == null && !TryGetComponent(out _label))
            {
                return;
            }

            // While the first language loads the label stays empty (neither the saved key nor #key# shows);
            // LanguageChangedEvent writes the text once the table is ready.
            _label.text = !Loc.IsReady ? string.Empty : _arguments == null ? Loc.Get(_key) : Loc.Format(_key, _arguments);
        }
    }
}

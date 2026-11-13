using System;
using Game.Foundation.Localization;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Localization;
using TMPro;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Shows the demo's sprint and crouch states with their press mode ("Sprint (toggle): on"). Drawn from the values
    /// <see cref="GameplayScreen"/> passes every frame; the text is rewritten only when a value or the language changes.
    /// REFERENCES: <c>_text</c>; the <c>*Key</c> fields are the line formats and the word keys.
    /// </summary>
    internal sealed class StanceView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private string _sprintKey;
        [SerializeField] private string _crouchKey;
        [SerializeField] private string _onKey;
        [SerializeField] private string _offKey;
        [SerializeField] private string _holdKey;
        [SerializeField] private string _toggleKey;

        private IDisposable _languageBinding;
        private int _shown = -1;

        private void OnEnable()
        {
            _shown = -1;
            if (CoreServiceLocator.TryGet(out IEventBus events))
            {
                _languageBinding = events.Subscribe<LanguageChangedEvent>(_ => _shown = -1);
            }
        }

        private void OnDisable()
        {
            _languageBinding?.Dispose();
            _languageBinding = null;
        }

        internal void Render(bool sprinting, bool toggleSprint, bool crouching, bool toggleCrouch)
        {
            int state = (sprinting ? 1 : 0) | (toggleSprint ? 2 : 0) | (crouching ? 4 : 0) | (toggleCrouch ? 8 : 0);
            if (state == _shown || !Loc.IsReady)
            {
                return;
            }

            _shown = state;
            _text.text = Loc.Format(_sprintKey, Loc.Get(toggleSprint ? _toggleKey : _holdKey), Loc.Get(sprinting ? _onKey : _offKey))
                + "\n" + Loc.Format(_crouchKey, Loc.Get(toggleCrouch ? _toggleKey : _holdKey), Loc.Get(crouching ? _onKey : _offKey));
        }
    }
}

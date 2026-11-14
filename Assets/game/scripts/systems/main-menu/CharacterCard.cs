using System;
using Game.Contracts.Roster;
using Game.Foundation.Localization;
using Game.Foundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.MainMenu
{
    /// <summary>
    /// One character in the list of the character page: its idle preview, its name, and a different card when it is the
    /// chosen one. Pressing it chooses the character.
    /// REFERENCES: <c>_button</c> is the card; <c>_background</c> swaps between <c>_normal</c> and <c>_chosen</c>; <c>_preview</c>
    /// plays the idle frames; <c>_name</c> shows the name.
    /// </summary>
    internal sealed class CharacterCard : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Sprite _normal;
        [SerializeField] private Sprite _chosen;
        [SerializeField] private SpriteFlipbook _preview;
        [SerializeField] private LocalizedLabel _name;

        internal event Action<CharacterDefinition> Pressed;

        internal CharacterDefinition Character { get; private set; }

        internal Button Button => _button;

        internal void Show(CharacterDefinition character, bool chosen)
        {
            if (Character != character)
            {
                Character = character;
                _preview.Play(character.PreviewFrames, character.PreviewFrameSeconds, character.Tint, character.Scale);
                _name.SetKey(character.NameKey);
            }

            _background.sprite = chosen ? _chosen : _normal;
        }

        private void Awake()
        {
            _button.onClick.AddListener(() =>
            {
                if (Character != null)
                {
                    Pressed?.Invoke(Character);
                }
            });
        }
    }
}

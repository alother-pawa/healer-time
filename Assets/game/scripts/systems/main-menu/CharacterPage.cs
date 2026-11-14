using System.Collections.Generic;
using Game.Contracts.Roster;
using Game.Foundation.Localization;
using Game.Foundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.MainMenu
{
    /// <summary>
    /// The character page of the main menu: two tabs (Hero, Healer), a card per character of the tab, and the chosen
    /// character's big preview, name, description and numbers. Every character is free: pressing a card chooses it at once
    /// (<see cref="IRoster.Select(HeroDefinition)"/>). Drawn from the roster every time it changes.
    /// REFERENCES: the tab buttons and their <c>*TabMark</c> objects (shown on the open tab); <c>_list</c> holds the cards cloned
    /// from <c>_cardTemplate</c>; the detail parts; the <c>*Key</c> fields format the numbers.
    /// </summary>
    internal sealed class CharacterPage : MonoBehaviour
    {
        [SerializeField] private Button _heroTab;
        [SerializeField] private Button _healerTab;
        [SerializeField] private GameObject _heroTabMark;
        [SerializeField] private GameObject _healerTabMark;
        [SerializeField] private RectTransform _list;
        [SerializeField] private CharacterCard _cardTemplate;
        [SerializeField] private SpriteFlipbook _preview;
        [SerializeField] private LocalizedLabel _name;
        [SerializeField] private LocalizedLabel _description;
        [SerializeField] private LocalizedLabel _stats;
        [SerializeField] private string _heroStatsKey = "characters.stats.hero";
        [SerializeField] private string _healerStatsKey = "characters.stats.healer";

        private readonly List<CharacterCard> _cards = new List<CharacterCard>();
        private IRoster _roster;
        private bool _healers;

        internal Button HeroTab => _heroTab;

        /// <summary>The control to select first when the page opens (the chosen character's card).</summary>
        internal Selectable FirstSelectable
        {
            get
            {
                foreach (CharacterCard card in _cards)
                {
                    if (card.gameObject.activeSelf && card.Character == Chosen)
                    {
                        return card.Button;
                    }
                }

                return _heroTab;
            }
        }

        private CharacterDefinition Chosen => _roster == null ? null
            : _healers ? (CharacterDefinition)_roster.SelectedHealer : _roster.SelectedHero;

        internal void Init(IRoster roster)
        {
            _roster = roster;
            _heroTab.onClick.AddListener(() => ShowTab(false));
            _healerTab.onClick.AddListener(() => ShowTab(true));
            Render();
        }

        internal void ShowTab(bool healers)
        {
            _healers = healers;
            Render();
        }

        /// <summary>Draws the tabs, the cards and the detail from the roster.</summary>
        internal void Render()
        {
            if (_roster == null)
            {
                return;
            }

            _heroTabMark.SetActive(!_healers);
            _healerTabMark.SetActive(_healers);
            int count = _healers ? _roster.Healers.Count : _roster.Heroes.Count;
            while (_cards.Count < count)
            {
                CharacterCard card = Instantiate(_cardTemplate, _list);
                card.Pressed += Choose;
                _cards.Add(card);
            }

            for (int i = 0; i < _cards.Count; i++)
            {
                bool used = i < count;
                _cards[i].gameObject.SetActive(used);
                if (used)
                {
                    CharacterDefinition character = _healers ? (CharacterDefinition)_roster.Healers[i] : _roster.Heroes[i];
                    _cards[i].Show(character, character == Chosen);
                }
            }

            CharacterDefinition chosen = Chosen;
            _preview.Play(chosen.PreviewFrames, chosen.PreviewFrameSeconds, chosen.Tint, chosen.Scale);
            _name.SetKey(chosen.NameKey);
            _description.SetKey(chosen.DescriptionKey);
            if (chosen is HeroDefinition hero)
            {
                _stats.SetFormat(_heroStatsKey, hero.MaxHealth, hero.AttackDamage, hero.StrikeMultiplier);
            }
            else if (chosen is HealerDefinition healer)
            {
                _stats.SetFormat(_healerStatsKey, healer.MagicCapacity, healer.SkillDamage, healer.SkillHeal);
            }
        }

        private void Choose(CharacterDefinition character)
        {
            if (character is HeroDefinition hero)
            {
                _roster.Select(hero);
            }
            else if (character is HealerDefinition healer)
            {
                _roster.Select(healer);
            }

            Render();
        }
    }
}

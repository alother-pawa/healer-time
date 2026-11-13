using System.Collections.Generic;
using System.Globalization;
using Game.Foundation.Localization;
using Paowa.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// One card of the slot panel, drawn entirely from a <see cref="SlotCardModel"/> by <see cref="Draw"/> (no state of its
    /// own, no animation): title, thumbnail or placeholder with a state icon, location, playtime and local save time for a
    /// healthy slot, a status line otherwise, and the action buttons the state allows (hidden ones leave the layout).
    /// REFERENCES: <c>_title</c> formats <c>_titleKey</c> with the slot number; <c>_thumbnail</c> shows the slot's thumbnail
    /// over <c>_thumbnailPlaceholder</c>; <c>_stateIcon</c> shows <c>_emptyIcon</c> or <c>_problemIcon</c>; <c>_details</c>
    /// holds <c>_location</c>, <c>_playtime</c> (<c>_playtimeKey</c>) and <c>_savedAt</c> (<c>_savedAtKey</c>);
    /// <c>_status</c> shows one of the status keys; the three buttons sit in a vertical layout.
    /// </summary>
    internal sealed class SaveSlotCardView : MonoBehaviour
    {
        private const string SavedAtFormat = "yyyy-MM-dd HH:mm";

        [SerializeField] private LocalizedLabel _title;
        [SerializeField] private string _titleKey;
        [SerializeField] private RawImage _thumbnail;
        [SerializeField] private Image _thumbnailPlaceholder;
        [SerializeField] private Image _stateIcon;
        [SerializeField] private Sprite _emptyIcon;
        [SerializeField] private Sprite _problemIcon;
        [SerializeField] private GameObject _details;
        [SerializeField] private LocalizedLabel _location;
        [SerializeField] private LocalizedLabel _playtime;
        [SerializeField] private string _playtimeKey;
        [SerializeField] private LocalizedLabel _savedAt;
        [SerializeField] private string _savedAtKey;
        [SerializeField] private LocalizedLabel _status;
        [SerializeField] private string _emptyKey;
        [SerializeField] private string _unlabeledKey;
        [SerializeField] private string _damagedKey;
        [SerializeField] private string _newerVersionKey;
        [SerializeField] private string _unreadableKey;
        [SerializeField] private string _migrationFailedKey;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _deleteButton;

        internal Button ContinueButton => _continueButton;

        internal Button NewGameButton => _newGameButton;

        internal Button DeleteButton => _deleteButton;

        /// <summary>Shows <paramref name="model"/>; <paramref name="thumbnail"/> may be null (placeholder).</summary>
        internal void Draw(SlotCardModel model, Texture2D thumbnail)
        {
            _title.SetFormat(_titleKey, model.Number);

            bool hasThumbnail = thumbnail != null;
            _thumbnail.texture = thumbnail;
            _thumbnail.gameObject.SetActive(hasThumbnail);
            _thumbnailPlaceholder.gameObject.SetActive(!hasThumbnail);
            Sprite icon = model.State == SlotCardState.Empty ? _emptyIcon
                : model.State == SlotCardState.Saved ? null : _problemIcon;
            _stateIcon.sprite = icon;
            // A problem icon also sits over a thumbnail; an empty slot never has one.
            _stateIcon.gameObject.SetActive(icon != null);

            bool saved = model.State == SlotCardState.Saved;
            _details.SetActive(saved);
            if (saved)
            {
                string labelKey = model.Card.LabelKey;
                _location.gameObject.SetActive(labelKey.Length > 0);
                if (labelKey.Length > 0)
                {
                    _location.SetKey(labelKey);
                }

                _playtime.SetFormat(_playtimeKey, SlotCardModel.FormatPlaytime(model.Card.PlaytimeSeconds));
                _savedAt.SetFormat(_savedAtKey,
                    model.LastSavedUtc.ToLocalTime().ToString(SavedAtFormat, CultureInfo.InvariantCulture));
            }

            string statusKey = StatusKey(model);
            _status.gameObject.SetActive(!string.IsNullOrEmpty(statusKey));
            if (!string.IsNullOrEmpty(statusKey))
            {
                _status.SetKey(statusKey);
            }

            _continueButton.gameObject.SetActive(model.CanContinue);
            _newGameButton.gameObject.SetActive(model.CanNewGame);
            _deleteButton.gameObject.SetActive(model.CanDelete);
        }

        /// <summary>Visible action buttons, top to bottom (the order of the vertical layout).</summary>
        internal void GetVisibleButtons(List<Selectable> results)
        {
            results.Clear();
            foreach (Button button in new[] { _continueButton, _newGameButton, _deleteButton })
            {
                if (button.gameObject.activeSelf)
                {
                    results.Add(button);
                }
            }
        }

        private string StatusKey(SlotCardModel model)
        {
            switch (model.State)
            {
                case SlotCardState.Empty:
                    return _emptyKey;
                case SlotCardState.Unlabeled:
                    return _unlabeledKey;
                case SlotCardState.Damaged:
                    return _damagedKey;
                case SlotCardState.NeedsNewerVersion:
                    return _newerVersionKey;
                case SlotCardState.LoadFailed:
                    return model.LoadFailure == SaveLoadStatus.MigrationFailed
                        ? _migrationFailedKey
                        : _unreadableKey;
                default:
                    return string.Empty;
            }
        }
    }
}

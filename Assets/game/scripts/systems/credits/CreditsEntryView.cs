using Game.Foundation.Localization;
using TMPro;
using UnityEngine;

namespace Game.Systems.Credits
{
    /// <summary>
    /// One credits row: name (as written), role (localized, hidden when the entry has no role key) and note (as written,
    /// hidden when empty). REFERENCES: <c>_nameText</c>, <c>_role</c>, <c>_noteText</c> are the three texts.
    /// </summary>
    internal sealed class CreditsEntryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private LocalizedLabel _role;
        [SerializeField] private TMP_Text _noteText;

        internal void Show(CreditsEntry entry)
        {
            _nameText.text = entry.Name;
            bool hasRole = entry.RoleKey.Length > 0;
            _role.gameObject.SetActive(hasRole);
            if (hasRole)
            {
                _role.SetKey(entry.RoleKey);
            }

            bool hasNote = entry.Note.Length > 0;
            _noteText.gameObject.SetActive(hasNote);
            _noteText.text = entry.Note;
        }
    }
}

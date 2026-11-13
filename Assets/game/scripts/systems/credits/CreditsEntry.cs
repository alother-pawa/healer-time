using System;
using UnityEngine;

namespace Game.Systems.Credits
{
    /// <summary>
    /// One credited person, company or third-party work: a name shown as written (names are not translated), an optional
    /// role key (localized) and an optional note shown as written (a license line, a URL).
    /// </summary>
    [Serializable]
    internal sealed class CreditsEntry
    {
        [SerializeField] private string _name;
        [SerializeField] private string _roleKey;
        [TextArea(1, 6)] [SerializeField] private string _note;

        internal string Name => _name ?? string.Empty;

        internal string RoleKey => _roleKey ?? string.Empty;

        internal string Note => _note ?? string.Empty;
    }
}

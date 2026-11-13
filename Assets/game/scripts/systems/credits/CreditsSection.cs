using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Credits
{
    /// <summary>A titled group of credits entries (studio, third-party notices, thanks).</summary>
    [Serializable]
    internal sealed class CreditsSection
    {
        [SerializeField] private string _titleKey;
        [SerializeField] private List<CreditsEntry> _entries = new List<CreditsEntry>();

        internal string TitleKey => _titleKey ?? string.Empty;

        internal IReadOnlyList<CreditsEntry> Entries => _entries ?? (_entries = new List<CreditsEntry>());
    }
}

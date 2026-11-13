using System;
using System.Collections.Generic;
using Game.Contracts.Presentation;
using Paowa.Settings;
using UnityEngine;

namespace Game.Systems.Settings
{
    /// <summary>
    /// "Apply changes?" when the player leaves a tab or closes the settings while deferred values are staged (resolution,
    /// full-screen mode, monitor, UI scale). One question covers every listed tab with staged values: Apply commits them
    /// (a display change then asks "Keep these display settings?"), Discard, Cancel/Back or a dialog closed any other way
    /// drops them, the safe choice since nothing staged is on screen yet. Only one question at a time.
    /// </summary>
    internal sealed class PendingChangesPrompt
    {
        private readonly ISettingsService _settings;
        private readonly Func<ConfirmRequest, Awaitable<bool>> _confirm;
        private readonly ConfirmRequest _request;

        internal PendingChangesPrompt(ISettingsService settings, Func<ConfirmRequest, Awaitable<bool>> confirm, ConfirmRequest request)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        /// <summary>True while the question is open.</summary>
        internal bool IsAsking { get; private set; }

        /// <summary>True when any of <paramref name="tabIds"/> has staged values.</summary>
        internal bool HasPending(IReadOnlyList<string> tabIds)
        {
            for (int i = 0; i < tabIds.Count; i++)
            {
                if (_settings.HasPendingChanges(tabIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Asks about the staged values of <paramref name="tabIds"/> and applies or drops them. Completes true once they are
        /// resolved (at once when nothing is staged), false without doing anything when a question is already open.
        /// </summary>
        internal async Awaitable<bool> ResolveAsync(IReadOnlyList<string> tabIds)
        {
            if (IsAsking)
            {
                return false;
            }

            var pending = new List<string>();
            for (int i = 0; i < tabIds.Count; i++)
            {
                if (_settings.HasPendingChanges(tabIds[i]))
                {
                    pending.Add(tabIds[i]);
                }
            }

            if (pending.Count == 0)
            {
                return true;
            }

            bool apply;
            IsAsking = true;
            try
            {
                apply = await _confirm(_request);
            }
            finally
            {
                IsAsking = false;
            }

            // Read again: the values may have been applied or dropped while the question was open.
            for (int i = 0; i < pending.Count; i++)
            {
                if (!_settings.HasPendingChanges(pending[i]))
                {
                    continue;
                }

                if (apply)
                {
                    _settings.ApplyTab(pending[i]);
                }
                else
                {
                    _settings.RevertTab(pending[i]);
                }
            }

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using Game.Contracts.Presentation;
using Game.Systems.Settings;
using NUnit.Framework;
using Paowa.Settings;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// "Apply changes?" logic of the settings popup with a fake settings service: nothing staged asks nothing, Apply
    /// commits and Discard (or any other close) drops the staged tabs only, one question at a time, and a tab resolved while
    /// the question was open is left alone.
    /// </summary>
    public sealed class PendingChangesPromptTests
    {
        private static readonly string[] AllTabs = { "gameplay", "graphics", "controls" };

        private FakeSettings _settings;
        private AwaitableCompletionSource<bool> _answer;
        private int _questions;
        private PendingChangesPrompt _prompt;

        [SetUp]
        public void SetUp()
        {
            _settings = new FakeSettings();
            _questions = 0;
            _prompt = new PendingChangesPrompt(_settings, Confirm,
                new ConfirmRequest("settings.pending.title", "settings.pending.message", "settings.apply", "settings.pending.discard"));
        }

        [Test]
        public void NothingStagedResolvesWithoutAsking()
        {
            Awaitable<bool> resolved = _prompt.ResolveAsync(AllTabs);

            Assert.That(Result(resolved), Is.True);
            Assert.That(_questions, Is.Zero);
            Assert.That(_settings.Calls, Is.Empty);
        }

        [Test]
        public void ApplyCommitsOnlyTheStagedTabs()
        {
            _settings.Pending.Add("graphics");

            Awaitable<bool> resolved = _prompt.ResolveAsync(AllTabs);

            Assert.That(_prompt.IsAsking, Is.True);
            Assert.That(resolved.GetAwaiter().IsCompleted, Is.False);
            _answer.SetResult(true);

            Assert.That(Result(resolved), Is.True);
            Assert.That(_prompt.IsAsking, Is.False);
            Assert.That(_settings.Calls, Is.EqualTo(new[] { "apply:graphics" }));
        }

        [Test]
        public void DiscardOrClosingTheDialogDropsTheStagedTabs()
        {
            _settings.Pending.Add("graphics");
            _settings.Pending.Add("controls");

            Awaitable<bool> resolved = _prompt.ResolveAsync(AllTabs);
            _answer.SetResult(false);

            Assert.That(Result(resolved), Is.True);
            Assert.That(_settings.Calls, Is.EqualTo(new[] { "revert:graphics", "revert:controls" }));
        }

        [Test]
        public void OneQuestionAtATime()
        {
            _settings.Pending.Add("graphics");
            Awaitable<bool> first = _prompt.ResolveAsync(AllTabs);

            Awaitable<bool> second = _prompt.ResolveAsync(AllTabs);

            Assert.That(Result(second), Is.False, "A second request while asking does nothing.");
            Assert.That(_questions, Is.EqualTo(1));
            _answer.SetResult(true);
            Assert.That(Result(first), Is.True);
            Assert.That(_settings.Calls, Is.EqualTo(new[] { "apply:graphics" }));
        }

        [Test]
        public void ATabResolvedMeanwhileIsLeftAlone()
        {
            _settings.Pending.Add("graphics");
            Awaitable<bool> resolved = _prompt.ResolveAsync(AllTabs);

            // The display change was applied from elsewhere while the dialog was open.
            _settings.Pending.Clear();
            _answer.SetResult(true);

            Assert.That(Result(resolved), Is.True);
            Assert.That(_settings.Calls, Is.Empty);
        }

        [Test]
        public void HasPendingLooksAtTheListedTabsOnly()
        {
            _settings.Pending.Add("graphics");

            Assert.That(_prompt.HasPending(AllTabs), Is.True);
            Assert.That(_prompt.HasPending(new[] { "gameplay" }), Is.False);
        }

        private Awaitable<bool> Confirm(ConfirmRequest request)
        {
            _questions++;
            _answer = new AwaitableCompletionSource<bool>();
            return _answer.Awaitable;
        }

        private static bool Result(Awaitable<bool> awaitable)
        {
            Awaitable<bool>.Awaiter awaiter = awaitable.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "Not resolved yet.");
            return awaiter.GetResult();
        }

        /// <summary>Settings service that only tracks staged tabs and records Apply/Revert calls.</summary>
        private sealed class FakeSettings : ISettingsService
        {
            internal readonly HashSet<string> Pending = new HashSet<string>();
            internal readonly List<string> Calls = new List<string>();

            public IReadOnlyList<SettingsTab> Tabs => Array.Empty<SettingsTab>();

            public bool HasPendingChanges(string tabId) => Pending.Contains(tabId);

            public void ApplyTab(string tabId)
            {
                Calls.Add("apply:" + tabId);
                Pending.Remove(tabId);
            }

            public void RevertTab(string tabId)
            {
                Calls.Add("revert:" + tabId);
                Pending.Remove(tabId);
            }

            public IReadOnlyList<SettingDefinition> GetDefinitions(string tabId) => Array.Empty<SettingDefinition>();

            public bool TryGetDefinition(string id, out SettingDefinition definition)
            {
                definition = null;
                return false;
            }

            public void AddCatalog(SettingsCatalog catalog)
            {
            }

            public SettingValue Get(string id) => SettingValue.None;

            public void Set(string id, SettingValue value)
            {
            }

            public void ResetTabToDefaults(string tabId)
            {
            }

            public void ResetToDefault(string id)
            {
            }

            public void GetOptions(string id, List<SettingOption> results)
            {
                results.Clear();
            }

            public void Invoke(string id)
            {
            }

            public IDisposable RegisterApplier(ISettingApplier applier) => new Release();

            public IDisposable RegisterOptionsProvider(string providerId, ISettingOptionsProvider provider) => new Release();

            public IDisposable RegisterAction(string actionId, Action<ButtonSettingDefinition> handler) => new Release();

            public IDisposable RegisterResetHandler(ISettingResetHandler handler) => new Release();

            public void NotifyExternalChange(string id)
            {
            }

            private sealed class Release : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}

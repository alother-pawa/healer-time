using System;
using System.Collections.Generic;
using Game.Contracts.Presentation;
using Game.Systems.SaveSlots;
using NUnit.Framework;
using Paowa.Save;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// The slot panel's actions with an in-memory save service: New game on an empty slot creates it at once, over a used
    /// slot it asks, deletes, then creates (a failed delete keeps the old game, a failed first save leaves no half-made
    /// slot), Delete asks and removes, Continue loads or remembers why it failed (Delete only, the session unbound from an
    /// unreadable slot), a slot from a newer version can only be deleted after a stronger question, one action runs at a
    /// time, and the panel starts on the most recently saved slot or the first empty one.
    /// </summary>
    public sealed class SaveSlotFlowTests
    {
        private static readonly string[] Ids = { "slot-1", "slot-2", "slot-3" };
        private static readonly DateTime Earlier = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Later = new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc);

        private FakeSaveSlotService _save;
        private SlotSaveWriter _writer;
        private SaveSlotFlow _flow;
        private readonly Queue<bool> _answers = new Queue<bool>();
        private readonly List<ConfirmRequest> _asked = new List<ConfirmRequest>();
        private AwaitableCompletionSource<bool> _pendingAnswer;
        private int _entered;
        private int _changed;

        [SetUp]
        public void SetUp()
        {
            var bus = new TestEventBus();
            _save = new FakeSaveSlotService(bus) { DataVersion = 1, AutoSaveTarget = AutoSaveTarget.CurrentSlot };
            _writer = new SlotSaveWriter(_save, bus, new PlaytimeTracker(null), () => Completed<byte[]>(null));
            _answers.Clear();
            _asked.Clear();
            _pendingAnswer = null;
            _entered = 0;
            _changed = 0;
            _flow = new SaveSlotFlow(_save, _writer, Ids, 1, "save-slots.location.start", Confirm, Keys(), () => _entered++);
            _flow.Changed += () => _changed++;
        }

        [Test]
        public void NewGameOnAnEmptySlotCreatesItAtOnceAndEntersGameplay()
        {
            Run(_flow.NewGameAsync(0));

            Assert.That(_asked, Is.Empty, "An empty slot needs no confirmation.");
            Assert.That(_save.Calls, Is.EqualTo(new[] { "new", "metadata", "save:slot-1" }));
            Assert.That(_entered, Is.EqualTo(1));
            Assert.That(_save.CurrentSlotId, Is.EqualTo("slot-1"));
            Assert.That(_flow.Describe(0).State, Is.EqualTo(SlotCardState.Saved));
            Assert.That(_flow.Describe(0).Card.LabelKey, Is.EqualTo("save-slots.location.start"));
        }

        [Test]
        public void NewGameOverAUsedSlotAsksThenDeletesBeforeCreating()
        {
            _save.Add("slot-2", Earlier).Attachments["thumbnail"] = new byte[] { 1 };
            _answers.Enqueue(true);

            Run(_flow.NewGameAsync(1));

            Assert.That(_asked, Has.Count.EqualTo(1));
            Assert.That(_asked[0].TitleKey, Is.EqualTo("overwrite.title"));
            Assert.That(_asked[0].HasCancel, Is.True);
            Assert.That(_asked[0].MessageArguments, Is.EqualTo(new object[] { 2 }));
            Assert.That(_save.Calls, Is.EqualTo(new[] { "delete:slot-2", "new", "metadata", "save:slot-2" }));
            Assert.That(_save.Slots["slot-2"].Attachments, Is.Empty, "The old game's thumbnail went with the delete.");
            Assert.That(_entered, Is.EqualTo(1));
        }

        [Test]
        public void DecliningTheOverwriteChangesNothing()
        {
            _save.Add("slot-1", Earlier);
            _answers.Enqueue(false);

            Run(_flow.NewGameAsync(0));

            Assert.That(_save.Calls, Is.Empty);
            Assert.That(_entered, Is.Zero);
            Assert.That(_flow.Describe(0).State, Is.EqualTo(SlotCardState.Saved));
        }

        [Test]
        public void AFailedDeleteKeepsTheOldGameAndCreatesNothing()
        {
            _save.Add("slot-1", Earlier);
            _save.FailDelete = true;
            _answers.Enqueue(true);

            Run(_flow.NewGameAsync(0));

            Assert.That(_save.Calls, Is.EqualTo(new[] { "delete:slot-1" }));
            Assert.That(_entered, Is.Zero);
            Assert.That(_asked[1].MessageKey, Is.EqualTo("error.delete"));
            Assert.That(_asked[1].HasCancel, Is.False, "Failures are single-button notices.");
            Assert.That(_flow.Describe(0).State, Is.EqualTo(SlotCardState.Saved));
        }

        [Test]
        public void AFailedFirstSaveLeavesNoHalfMadeSlot()
        {
            _save.FailSaveAs = true;
            _save.FailSaveAsLeavesData = true;

            Run(_flow.NewGameAsync(2));

            Assert.That(_save.Calls, Is.EqualTo(new[] { "new", "metadata", "save:slot-3", "delete:slot-3", "new" }));
            Assert.That(_save.Slots.ContainsKey("slot-3"), Is.False);
            Assert.That(_save.CurrentSlotId, Is.Null);
            Assert.That(_entered, Is.Zero);
            Assert.That(_asked[0].MessageKey, Is.EqualTo("error.create"));
            Assert.That(_flow.Describe(2).State, Is.EqualTo(SlotCardState.Empty));
            Assert.That(_changed, Is.EqualTo(1));
        }

        [Test]
        public void DeleteAsksAndRemovesTheSlot()
        {
            _save.Add("slot-1", Earlier);
            _save.BindCurrent("slot-1");
            _answers.Enqueue(true);

            Run(_flow.DeleteAsync(0));

            Assert.That(_asked[0].TitleKey, Is.EqualTo("delete.title"));
            Assert.That(_asked[0].CancelKey, Is.EqualTo("cancel"));
            Assert.That(_save.Slots.ContainsKey("slot-1"), Is.False);
            Assert.That(_save.CurrentSlotId, Is.Null);
            Assert.That(_flow.Describe(0).State, Is.EqualTo(SlotCardState.Empty));
            Assert.That(_changed, Is.EqualTo(1));
        }

        [Test]
        public void DecliningTheDeleteKeepsTheSlot()
        {
            _save.Add("slot-1", Earlier);
            _answers.Enqueue(false);

            Run(_flow.DeleteAsync(0));

            Assert.That(_save.Slots.ContainsKey("slot-1"), Is.True);
            Assert.That(_save.Calls, Is.Empty);
        }

        [Test]
        public void AFailedDeleteIsReported()
        {
            _save.Add("slot-1", Earlier);
            _save.FailDelete = true;
            _answers.Enqueue(true);

            Run(_flow.DeleteAsync(0));

            Assert.That(_asked[1].MessageKey, Is.EqualTo("error.delete"));
            Assert.That(_flow.Describe(0).State, Is.EqualTo(SlotCardState.Saved));
        }

        [Test]
        public void ContinueLoadsAndEntersGameplay()
        {
            _save.Add("slot-1", Earlier);

            Run(_flow.ContinueAsync(0));

            Assert.That(_save.Calls, Is.EqualTo(new[] { "load:slot-1" }));
            Assert.That(_save.CurrentSlotId, Is.EqualTo("slot-1"));
            Assert.That(_entered, Is.EqualTo(1));
            Assert.That(_asked, Is.Empty);
        }

        [Test]
        public void AnUnreadableSlotIsRememberedOffersDeleteOnlyAndReleasesTheSession()
        {
            FakeSaveSlotService.Slot slot = _save.Add("slot-1", Earlier);
            slot.LoadStatus = SaveLoadStatus.Unreadable;
            slot.HasBackup = true;

            Run(_flow.ContinueAsync(0));

            Assert.That(_entered, Is.Zero);
            Assert.That(_save.CurrentSlotId, Is.Null, "Core bound the session to the unreadable slot; the flow released it.");
            Assert.That(_asked[0].MessageKey, Is.EqualTo("error.unreadable"));
            Assert.That(_asked[0].HasCancel, Is.False);
            SlotCardModel card = _flow.Describe(0);
            Assert.That(card.State, Is.EqualTo(SlotCardState.LoadFailed));
            Assert.That(card.CanContinue || card.CanNewGame, Is.False, "Core already tried .tmp and .bak: nothing to restore.");
            Assert.That(card.CanDelete, Is.True);
        }

        [Test]
        public void AFailedMigrationKeepsTheSessionAndSaysWhy()
        {
            _save.Add("slot-1", Earlier).LoadStatus = SaveLoadStatus.MigrationFailed;
            _save.Add("slot-2", Later);
            _save.BindCurrent("slot-2");

            Run(_flow.ContinueAsync(0));

            Assert.That(_save.CurrentSlotId, Is.EqualTo("slot-2"), "Nothing changed: the session of another slot stays.");
            Assert.That(_asked[0].MessageKey, Is.EqualTo("error.migration-failed"));
            Assert.That(_flow.Describe(0).LoadFailure, Is.EqualTo(SaveLoadStatus.MigrationFailed));
        }

        [Test]
        public void ASlotFromANewerVersionCanOnlyBeDeletedAfterAStrongerQuestion()
        {
            _save.Add("slot-1", Earlier).DataVersion = 2;

            SlotCardModel card = _flow.Describe(0);
            Run(_flow.ContinueAsync(0));
            Run(_flow.NewGameAsync(0));

            Assert.That(card.State, Is.EqualTo(SlotCardState.NeedsNewerVersion));
            Assert.That(card.CanContinue || card.CanNewGame, Is.False);
            Assert.That(_save.Calls, Is.Empty);
            Assert.That(_asked, Is.Empty);

            _answers.Enqueue(false);
            Run(_flow.DeleteAsync(0));
            Assert.That(_asked[0].MessageKey, Is.EqualTo("delete.newer-message"));
            Assert.That(_save.Slots.ContainsKey("slot-1"), Is.True);

            _answers.Enqueue(true);
            Run(_flow.DeleteAsync(0));
            Assert.That(_save.Slots.ContainsKey("slot-1"), Is.False);
        }

        [Test]
        public void OneActionAtATime()
        {
            _save.Add("slot-1", Earlier);
            _save.Add("slot-2", Earlier);
            _pendingAnswer = new AwaitableCompletionSource<bool>();

            Awaitable first = _flow.DeleteAsync(0);
            Run(_flow.DeleteAsync(1));

            Assert.That(_flow.IsBusy, Is.True);
            Assert.That(_asked, Has.Count.EqualTo(1), "The second request was ignored while the first asks.");

            _pendingAnswer.SetResult(true);
            Run(first);
            Assert.That(_flow.IsBusy, Is.False);
            Assert.That(_save.Slots.ContainsKey("slot-1"), Is.False);
            Assert.That(_save.Slots.ContainsKey("slot-2"), Is.True);
        }

        [Test]
        public void ThePanelStartsOnTheLatestSaveElseTheFirstEmptySlot()
        {
            Assert.That(SaveSlotFlow.PreferredIndex(Cards()), Is.EqualTo(0), "All empty: the first one.");

            _save.Add("slot-1", Earlier);
            _save.Add("slot-3", Later);
            Assert.That(SaveSlotFlow.PreferredIndex(Cards()), Is.EqualTo(2), "The most recently saved slot.");

            _save.Slots.Clear();
            _save.Add("slot-1", Earlier).DataVersion = 5;
            Assert.That(SaveSlotFlow.PreferredIndex(Cards()), Is.EqualTo(1), "Nothing to continue: the first empty slot.");

            _save.Add("slot-2", Earlier).DataVersion = 5;
            _save.Add("slot-3", Earlier).DataVersion = 5;
            Assert.That(SaveSlotFlow.PreferredIndex(Cards()), Is.EqualTo(0), "Only Delete everywhere: the first card.");
        }

        private List<SlotCardModel> Cards()
        {
            var cards = new List<SlotCardModel>();
            for (int i = 0; i < _flow.Count; i++)
            {
                cards.Add(_flow.Describe(i));
            }

            return cards;
        }

        private Awaitable<bool> Confirm(ConfirmRequest request)
        {
            _asked.Add(request);
            if (_pendingAnswer != null)
            {
                return _pendingAnswer.Awaitable;
            }

            // Notices (no cancel button) are acknowledged; questions take the queued answer.
            return Completed(request.HasCancel ? _answers.Dequeue() : true);
        }

        private static Awaitable<T> Completed<T>(T value)
        {
            var source = new AwaitableCompletionSource<T>();
            source.SetResult(value);
            return source.Awaitable;
        }

        private static void Run(Awaitable action)
        {
            Awaitable.Awaiter awaiter = action.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "The action did not finish.");
            awaiter.GetResult();
        }

        private static SaveSlotDialogKeys Keys()
        {
            return new SaveSlotDialogKeys
            {
                Cancel = "cancel",
                OverwriteTitle = "overwrite.title",
                OverwriteMessage = "overwrite.message",
                OverwriteConfirm = "overwrite.confirm",
                DeleteTitle = "delete.title",
                DeleteMessage = "delete.message",
                DeleteConfirm = "delete.confirm",
                DeleteNewerMessage = "delete.newer-message",
                ErrorTitle = "error.title",
                ErrorConfirm = "ok",
                UnreadableMessage = "error.unreadable",
                MigrationFailedMessage = "error.migration-failed",
                NewerVersionMessage = "error.newer",
                CreateFailedMessage = "error.create",
                DeleteFailedMessage = "error.delete",
            };
        }
    }
}

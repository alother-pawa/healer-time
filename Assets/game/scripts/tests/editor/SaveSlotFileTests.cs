using System;
using System.IO;
using Game.Contracts.Presentation;
using Game.Systems.SaveSlots;
using NUnit.Framework;
using Paowa.Core.Messaging;
using Paowa.Save;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// The slot flow and writer on Core's real save service over a temporary folder (<see cref="SaveServiceFactory"/>):
    /// New game writes data, card metadata and nothing else; Continue restores playtime and label through Core's events; a
    /// checkpoint attaches the thumbnail; Delete removes every file; a damaged data file without copies ends Delete-only
    /// with the session released.
    /// </summary>
    public sealed class SaveSlotFileTests
    {
        private static readonly byte[] Jpg = { 0xFF, 0xD8, 0x07 };

        private string _root;
        private ISaveSlotService _save;
        private PlaytimeTracker _playtime;
        private SlotSaveWriter _writer;
        private SaveSlotFlow _flow;
        private int _entered;
        private ConfirmRequest _lastNotice;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "pc-base-save-tests", Guid.NewGuid().ToString("N"));
            _save = SaveServiceFactory.CreateForTests(_root, out IEventBus events);
            _save.DataVersion = 1;
            _save.AutoSaveTarget = AutoSaveTarget.CurrentSlot;
            _playtime = new PlaytimeTracker(null);
            _writer = new SlotSaveWriter(_save, events, _playtime, () => Completed(Jpg));
            _entered = 0;
            _lastNotice = null;
            _flow = new SaveSlotFlow(_save, _writer, new[] { "slot-1", "slot-2", "slot-3" }, 1, "save-slots.location.start", Confirm,
                Keys(), () => _entered++);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void NewGameContinueCheckpointAndDeleteOnRealFiles()
        {
            Run(_flow.NewGameAsync(0));

            Assert.That(_entered, Is.EqualTo(1));
            Assert.That(_save.TryGetSlotInfo("slot-1", out SaveSlotInfo info), Is.True);
            Assert.That(SlotCard.TryParse(info.Extra, out SlotCard card), Is.True);
            Assert.That(card.LabelKey, Is.EqualTo("save-slots.location.start"));
            Assert.That(info.DataVersion, Is.EqualTo(1));
            Assert.That(_flow.Describe(0).State, Is.EqualTo(SlotCardState.Saved));

            _playtime.Restore(125d);
            Assert.That(Result(_writer.SaveCheckpointAsync("location.harbour")), Is.True);
            Assert.That(_save.TryReadSlotAttachment("slot-1", SlotSaveWriter.ThumbnailName, out byte[] thumbnail), Is.True);
            Assert.That(thumbnail, Is.EqualTo(Jpg));

            _writer.StartNewGame(string.Empty);
            Assert.That(_playtime.Seconds, Is.Zero);
            Run(_flow.ContinueAsync(0));

            Assert.That(_entered, Is.EqualTo(2));
            Assert.That(_playtime.Seconds, Is.EqualTo(125d), "Restored from the slot data by SaveSlotLoadedEvent.");
            Assert.That(_writer.CurrentLabelKey, Is.EqualTo("location.harbour"));
            Assert.That(_writer.CurrentThumbnail, Is.EqualTo(Jpg));

            Run(_flow.DeleteAsync(0));

            Assert.That(_save.TryGetSlotInfo("slot-1", out _), Is.False);
            Assert.That(_save.CurrentSlotId, Is.Null);
            Assert.That(Directory.GetFiles(Path.Combine(_root, "slots"), "slot-1*", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void ADamagedDataFileWithoutCopiesEndsDeleteOnly()
        {
            Run(_flow.NewGameAsync(1));
            _writer.StartNewGame(string.Empty);
            File.WriteAllText(Path.Combine(_root, "slots", "slot-2.json"), "not json");

            Run(_flow.ContinueAsync(1));

            Assert.That(_entered, Is.EqualTo(1), "Only the new game entered gameplay.");
            Assert.That(_lastNotice.MessageKey, Is.EqualTo("error.unreadable"));
            Assert.That(_save.CurrentSlotId, Is.Null, "The session is not bound to the damaged slot.");
            SlotCardModel card = _flow.Describe(1);
            Assert.That(card.CanContinue || card.CanNewGame, Is.False);
            Assert.That(card.CanDelete, Is.True);
        }

        private Awaitable<bool> Confirm(ConfirmRequest request)
        {
            if (!request.HasCancel)
            {
                _lastNotice = request;
            }

            return Completed(true);
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

        private static T Result<T>(Awaitable<T> awaitable)
        {
            Awaitable<T>.Awaiter awaiter = awaitable.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "Not finished.");
            return awaiter.GetResult();
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

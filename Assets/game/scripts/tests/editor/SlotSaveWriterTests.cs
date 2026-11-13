using System;
using System.Collections.Generic;
using Game.Contracts.Save;
using Game.Systems.SaveSlots;
using NUnit.Framework;
using Paowa.Save;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Every slot save carries the slot card, also a direct Core <c>Save()</c>: playtime and label go into the slot data and
    /// the metadata in Core's writing hook. Every save writes the last thumbnail once more (Core's backups stay paired); a
    /// checkpoint then attaches its fresh capture, unless another save happened during the capture (the image waits for the
    /// next save) or its session ended (new game, load, delete). Loading restores playtime, label and thumbnail.
    /// </summary>
    public sealed class SlotSaveWriterTests
    {
        private static readonly byte[] Jpg = { 0xFF, 0xD8, 0x01 };
        private static readonly byte[] OlderJpg = { 0xFF, 0xD8, 0x02 };
        private static readonly DateTime Earlier = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

        private TestEventBus _bus;
        private FakeSaveSlotService _save;
        private PlaytimeTracker _playtime;
        private SlotSaveWriter _writer;
        private Func<Awaitable<byte[]>> _capture;

        [SetUp]
        public void SetUp()
        {
            _bus = new TestEventBus();
            _save = new FakeSaveSlotService(_bus) { DataVersion = 1, AutoSaveTarget = AutoSaveTarget.CurrentSlot };
            _playtime = new PlaytimeTracker(null);
            _capture = () => Completed(Jpg);
            _writer = new SlotSaveWriter(_save, _bus, _playtime, () => _capture());
        }

        [Test]
        public void SavingWritesTheCardIntoTheDataAndTheMetadataFirst()
        {
            _writer.StartNewGame("save-slots.location.start");
            _playtime.Restore(75d);

            Assert.That(_writer.SaveAs("slot-1"), Is.True);

            Assert.That(_save.Calls, Is.EqualTo(new[] { "new", "metadata", "save:slot-1" }));
            Assert.That(_save.Current.Get(SlotSaveWriter.PlaytimeKey, 0d), Is.EqualTo(75d));
            Assert.That(_save.Current.Get(SlotSaveWriter.LabelKey, string.Empty), Is.EqualTo("save-slots.location.start"));
            Assert.That(SlotCard.TryParse(_save.Slots["slot-1"].Extra, out SlotCard card), Is.True);
            Assert.That(card.PlaytimeSeconds, Is.EqualTo(75d));
            Assert.That(card.LabelKey, Is.EqualTo("save-slots.location.start"));
        }

        [Test]
        public void ADirectCoreSaveAlsoWritesAFreshCard()
        {
            _writer.StartNewGame("start");
            _writer.SaveAs("slot-1");
            _playtime.Restore(99d);

            Assert.That(_save.Save(), Is.True);

            Assert.That(SlotCard.TryParse(_save.Slots["slot-1"].Extra, out SlotCard card), Is.True);
            Assert.That(card.PlaytimeSeconds, Is.EqualTo(99d));
            Assert.That(_save.PendingSlotMetadata, Is.EqualTo(_save.Slots["slot-1"].Extra));
        }

        [Test]
        public void AnotherSaveDuringTheCaptureDefersTheFreshThumbnailToTheNextSave()
        {
            var pending = new AwaitableCompletionSource<byte[]>();
            _capture = () => pending.Awaitable;
            _writer.StartNewGame("start");
            _writer.SaveAs("slot-1");

            Awaitable<bool> checkpoint = _writer.SaveCheckpointAsync("a");
            Assert.That(_writer.Save(), Is.True);
            pending.SetResult(Jpg);

            Assert.That(Result(checkpoint), Is.True);
            Assert.That(_save.Slots["slot-1"].Attachments, Is.Empty, "Attaching now would pair it with the other save.");
            Assert.That(_writer.CurrentThumbnail, Is.EqualTo(Jpg));

            Assert.That(_writer.Save(), Is.True);
            Assert.That(_save.Slots["slot-1"].Attachments["thumbnail"], Is.EqualTo(Jpg));
        }

        [Test]
        public void SaveWithoutACurrentSlotWritesNothing()
        {
            Assert.That(_writer.Save(), Is.False);
            Assert.That(_save.Calls, Is.Empty);
        }

        [Test]
        public void ACheckpointSavesTheCurrentSlotThenAttachesTheThumbnail()
        {
            _writer.StartNewGame("start");
            _writer.SaveAs("slot-1");
            _save.Calls.Clear();

            Assert.That(Result(_writer.SaveCheckpointAsync("chapter.two")), Is.True);

            Assert.That(_save.Calls, Is.EqualTo(new[] { "metadata", "autosave:slot-1", "attach:slot-1:thumbnail" }));
            Assert.That(_save.Slots["slot-1"].Attachments["thumbnail"], Is.EqualTo(Jpg));
            Assert.That(_writer.CurrentLabelKey, Is.EqualTo("chapter.two"));
            Assert.That(_save.Current.Get(SlotSaveWriter.LabelKey, string.Empty), Is.EqualTo("chapter.two"));
        }

        [Test]
        public void WithoutANewCaptureTheLastThumbnailIsWrittenAgain()
        {
            _writer.StartNewGame("start");
            _writer.SaveAs("slot-1");
            Result(_writer.SaveCheckpointAsync("a"));
            _capture = () => Completed<byte[]>(null);
            _save.Calls.Clear();

            Assert.That(Result(_writer.SaveCheckpointAsync("b")), Is.True);
            Assert.That(_writer.Save(), Is.True);

            Assert.That(_save.Calls, Is.EqualTo(new[]
            {
                "metadata", "autosave:slot-1", "attach:slot-1:thumbnail", "metadata", "save:slot-1", "attach:slot-1:thumbnail",
            }));
            Assert.That(_save.Slots["slot-1"].Attachments["thumbnail"], Is.EqualTo(Jpg));
        }

        [Test]
        public void NoThumbnailYetMeansNoAttachmentAndTheSaveStillSucceeds()
        {
            _capture = () => Completed<byte[]>(null);
            _writer.StartNewGame("start");
            _writer.SaveAs("slot-1");

            Assert.That(Result(_writer.SaveCheckpointAsync("a")), Is.True);

            Assert.That(_save.Calls, Has.No.Member("attach:slot-1:thumbnail"));
        }

        [Test]
        public void AThrowingCaptureDoesNotFailTheCheckpoint()
        {
            _capture = () => throw new InvalidOperationException("no screen (test)");
            _writer.StartNewGame("start");
            _writer.SaveAs("slot-1");

            Assert.That(Result(_writer.SaveCheckpointAsync("a")), Is.True);
            Assert.That(_save.Slots["slot-1"].Attachments, Is.Empty);
        }

        [Test]
        public void AThumbnailOfAnEndedSessionIsDropped()
        {
            var pending = new AwaitableCompletionSource<byte[]>();
            _capture = () => pending.Awaitable;
            _writer.StartNewGame("start");
            _writer.SaveAs("slot-1");

            Awaitable<bool> checkpoint = _writer.SaveCheckpointAsync("a");
            _writer.StartNewGame("start");
            _writer.SaveAs("slot-2");
            pending.SetResult(Jpg);

            Assert.That(Result(checkpoint), Is.True, "The progress was saved before the capture.");
            Assert.That(_save.Slots["slot-1"].Attachments, Is.Empty);
            Assert.That(_save.Slots["slot-2"].Attachments, Is.Empty);
        }

        [Test]
        public void WithoutACurrentSlotTheCheckpointGoesToTheSharedSlot()
        {
            Assert.That(Result(_writer.SaveCheckpointAsync("a")), Is.True);

            Assert.That(_save.Calls, Is.EqualTo(new[] { "metadata", "autosave:autosave", "attach:autosave:thumbnail" }));
        }

        [Test]
        public void LoadingRestoresPlaytimeLabelAndThumbnail()
        {
            FakeSaveSlotService.Slot slot = _save.Add("slot-3", Earlier);
            slot.Data[SlotSaveWriter.PlaytimeKey] = 640d;
            slot.Data[SlotSaveWriter.LabelKey] = "chapter.three";
            slot.Attachments[SlotSaveWriter.ThumbnailName] = OlderJpg;

            _save.LoadSlot("slot-3");

            Assert.That(_playtime.Seconds, Is.EqualTo(640d));
            Assert.That(_writer.CurrentLabelKey, Is.EqualTo("chapter.three"));
            Assert.That(_writer.CurrentThumbnail, Is.EqualTo(OlderJpg));
        }

        [Test]
        public void ANewGameStartsFromZero()
        {
            _playtime.Restore(500d);

            _writer.StartNewGame("start");

            Assert.That(_playtime.Seconds, Is.Zero);
            Assert.That(_writer.CurrentThumbnail, Is.Null);
            Assert.That(_save.CurrentSlotId, Is.Null);
        }

        [Test]
        public void GameSavedIsPublishedForCheckpointAndSaveButNotForTheNewGameSave()
        {
            var saved = new List<GameSavedEvent>();
            _bus.Subscribe<GameSavedEvent>(saved.Add);
            _writer.StartNewGame("start");
            _writer.SaveAs("slot-1");
            Assert.That(saved, Is.Empty, "The first save of a new game is not the player saving.");

            Assert.That(_writer.Save(), Is.True);
            Assert.That(Result(_writer.SaveCheckpointAsync("a")), Is.True);

            Assert.That(saved.Count, Is.EqualTo(2));
            Assert.That(saved[0].SlotId, Is.EqualTo("slot-1"));
            Assert.That(saved[0].IsCheckpoint, Is.False);
            Assert.That(saved[1].IsCheckpoint, Is.True);
        }

        [Test]
        public void GameSavedIsNotPublishedWithoutACurrentSlot()
        {
            var saved = new List<GameSavedEvent>();
            _bus.Subscribe<GameSavedEvent>(saved.Add);
            _writer.StartNewGame("start");

            Assert.That(_writer.Save(), Is.False);
            Assert.That(saved, Is.Empty);
        }

        private static Awaitable<T> Completed<T>(T value)
        {
            var source = new AwaitableCompletionSource<T>();
            source.SetResult(value);
            return source.Awaitable;
        }

        private static T Result<T>(Awaitable<T> awaitable)
        {
            Awaitable<T>.Awaiter awaiter = awaitable.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "Not finished.");
            return awaiter.GetResult();
        }
    }
}

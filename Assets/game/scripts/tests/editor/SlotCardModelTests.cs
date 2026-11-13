using System;
using Game.Systems.SaveSlots;
using NUnit.Framework;
using Paowa.Save;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Card state and actions from Core's slot listing (<see cref="SaveSlotInfo"/>) and the last load outcome, the slot card
    /// JSON round trip (tolerant of missing, garbage and unknown fields) and the playtime text.
    /// </summary>
    public sealed class SlotCardModelTests
    {
        private const int GameVersion = 2;
        private static readonly DateTime Saved = new DateTime(2026, 10, 16, 8, 30, 0, DateTimeKind.Utc);

        [Test]
        public void NoFilesIsAnEmptySlotWithNewGameOnly()
        {
            SlotCardModel card = Describe(false, default, SaveLoadStatus.Loaded);

            Assert.That(card.State, Is.EqualTo(SlotCardState.Empty));
            AssertActions(card, newGame: true);
            Assert.That(card.IsUsed, Is.False);
        }

        [Test]
        public void AHealthySlotShowsItsCardAndAllowsContinueNewGameAndDelete()
        {
            string extra = new SlotCard(3725d, "chapter.two").ToJson();
            SlotCardModel card = Describe(true, Info(extra, GameVersion, true, false, null), SaveLoadStatus.Loaded);

            Assert.That(card.State, Is.EqualTo(SlotCardState.Saved));
            Assert.That(card.Card.PlaytimeSeconds, Is.EqualTo(3725d));
            Assert.That(card.Card.LabelKey, Is.EqualTo("chapter.two"));
            Assert.That(card.LastSavedUtc, Is.EqualTo(Saved));
            Assert.That(card.Number, Is.EqualTo(2));
            AssertActions(card, @continue: true, newGame: true, delete: true);
        }

        [Test]
        public void UnreadableMetadataWithDataAllowsContinueAndDelete()
        {
            SlotCardModel card = Describe(true, Info(null, 0, true, false, "metadata unreadable"), SaveLoadStatus.Loaded);

            Assert.That(card.State, Is.EqualTo(SlotCardState.Unlabeled));
            AssertActions(card, @continue: true, delete: true);
        }

        [Test]
        public void MissingDataAllowsDeleteOnly()
        {
            SlotCardModel card = Describe(true, Info(null, GameVersion, false, false, "data missing"), SaveLoadStatus.Loaded);

            Assert.That(card.State, Is.EqualTo(SlotCardState.Damaged));
            AssertActions(card, delete: true);
        }

        [Test]
        public void DataFromANewerVersionCanOnlyBeDeleted()
        {
            SlotCardModel listed = Describe(true, Info(null, GameVersion + 1, true, true, null), SaveLoadStatus.Loaded);
            SlotCardModel reported = Describe(true, Info(null, 0, true, true, "metadata unreadable"), SaveLoadStatus.NewerDataVersion);

            Assert.That(listed.State, Is.EqualTo(SlotCardState.NeedsNewerVersion));
            Assert.That(reported.State, Is.EqualTo(SlotCardState.NeedsNewerVersion), "The load found the newer version.");
            AssertActions(listed, delete: true);
            AssertActions(reported, delete: true);
        }

        [Test]
        public void AFailedLoadOffersDeleteOnlyEvenWithABackup()
        {
            SlotCardModel unreadable = Describe(true, Info(null, GameVersion, true, true, null), SaveLoadStatus.Unreadable);
            SlotCardModel migration = Describe(true, Info(null, GameVersion, true, true, null), SaveLoadStatus.MigrationFailed);

            Assert.That(unreadable.State, Is.EqualTo(SlotCardState.LoadFailed));
            Assert.That(unreadable.LoadFailure, Is.EqualTo(SaveLoadStatus.Unreadable));
            AssertActions(unreadable, delete: true);
            Assert.That(migration.LoadFailure, Is.EqualTo(SaveLoadStatus.MigrationFailed));
            AssertActions(migration, delete: true);
        }

        [Test]
        public void MissingDataWinsOverAFailedLoad()
        {
            SlotCardModel card = Describe(true, Info(null, GameVersion, false, false, "data missing"), SaveLoadStatus.Unreadable);

            Assert.That(card.State, Is.EqualTo(SlotCardState.Damaged));
        }

        [Test]
        public void TheCardRoundTripsThroughJson()
        {
            string json = new SlotCard(12.5d, "location.harbour").ToJson();

            Assert.That(SlotCard.TryParse(json, out SlotCard card), Is.True);
            Assert.That(card.PlaytimeSeconds, Is.EqualTo(12.5d));
            Assert.That(card.LabelKey, Is.EqualTo("location.harbour"));
        }

        [Test]
        public void MissingOrGarbageMetadataReadsAsAnEmptyCard()
        {
            foreach (string text in new[] { null, string.Empty, "not json", "{\"_playtimeSeconds\":" })
            {
                Assert.That(SlotCard.TryParse(text, out SlotCard card), Is.False, text ?? "null");
                Assert.That(card.PlaytimeSeconds, Is.Zero);
                Assert.That(card.LabelKey, Is.Empty);
            }
        }

        [Test]
        public void UnknownFieldsAreIgnoredAndNegativePlaytimeReadsAsZero()
        {
            Assert.That(SlotCard.TryParse("{\"_labelKey\":\"a.b\",\"_future\":3}", out SlotCard known), Is.True);
            Assert.That(known.LabelKey, Is.EqualTo("a.b"));
            Assert.That(known.PlaytimeSeconds, Is.Zero);

            Assert.That(SlotCard.TryParse("{\"_playtimeSeconds\":-40}", out SlotCard negative), Is.True);
            Assert.That(negative.PlaytimeSeconds, Is.Zero);
        }

        [Test]
        public void PlaytimeShowsHoursAndWholeMinutes()
        {
            Assert.That(SlotCardModel.FormatPlaytime(0d), Is.EqualTo("0:00"));
            Assert.That(SlotCardModel.FormatPlaytime(59.9d), Is.EqualTo("0:00"));
            Assert.That(SlotCardModel.FormatPlaytime(305d), Is.EqualTo("0:05"));
            Assert.That(SlotCardModel.FormatPlaytime(3725d), Is.EqualTo("1:02"));
            Assert.That(SlotCardModel.FormatPlaytime(130 * 3600d), Is.EqualTo("130:00"));
            Assert.That(SlotCardModel.FormatPlaytime(-5d), Is.EqualTo("0:00"));
            Assert.That(SlotCardModel.FormatPlaytime(double.NaN), Is.EqualTo("0:00"));
        }

        private static SlotCardModel Describe(bool exists, SaveSlotInfo info, SaveLoadStatus failure)
        {
            return SlotCardModel.Describe("slot-2", 2, exists, info, GameVersion, failure);
        }

        private static SaveSlotInfo Info(string extra, int dataVersion, bool hasData, bool hasBackup, string corruption)
        {
            return new SaveSlotInfo("slot-2", false, null, Saved, null, 1, extra, dataVersion, hasData, hasBackup, corruption);
        }

        private static void AssertActions(SlotCardModel card, bool @continue = false, bool newGame = false, bool delete = false)
        {
            Assert.That(card.CanContinue, Is.EqualTo(@continue), "Continue");
            Assert.That(card.CanNewGame, Is.EqualTo(newGame), "New game");
            Assert.That(card.CanDelete, Is.EqualTo(delete), "Delete");
        }
    }
}

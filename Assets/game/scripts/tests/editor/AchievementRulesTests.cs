using System.Collections.Generic;
using Game.Systems.Achievements;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>The demo's unlock rules and what a row shows for locked, unlocked and hidden achievements.</summary>
    public sealed class AchievementRulesTests
    {
        private static readonly string[] Tracked = { "Gameplay/Move", "Gameplay/Jump", "Gameplay/Pause" };

        [Test]
        public void AnyPress_EarnsFirstAction()
        {
            var rules = new AchievementRules();
            var earned = new List<string>();

            rules.OnActionPressed("Gameplay/Jump", Tracked, earned);

            Assert.That(earned, Is.EqualTo(new[] { AchievementIds.FirstAction }));
        }

        [Test]
        public void AllActions_IsEarnedOnlyOnceEveryTrackedActionWasPressed()
        {
            var rules = new AchievementRules();
            var earned = new List<string>();

            rules.OnActionPressed("Gameplay/Move", Tracked, earned);
            rules.OnActionPressed("Gameplay/Jump", Tracked, earned);
            rules.OnActionPressed("Gameplay/Jump", Tracked, earned);
            Assert.That(earned, Has.No.Member(AchievementIds.AllActions));

            rules.OnActionPressed("Gameplay/Pause", Tracked, earned);
            Assert.That(earned, Has.Member(AchievementIds.AllActions));
        }

        [Test]
        public void WithoutTrackedActions_AllActionsIsNeverEarned()
        {
            var rules = new AchievementRules();
            var earned = new List<string>();

            rules.OnActionPressed("Gameplay/Move", null, earned);
            rules.OnActionPressed("Gameplay/Jump", new string[0], earned);

            Assert.That(earned, Has.No.Member(AchievementIds.AllActions));
        }

        [Test]
        public void EmptyAction_EarnsNothing()
        {
            var earned = new List<string>();

            new AchievementRules().OnActionPressed(string.Empty, Tracked, earned);

            Assert.That(earned, Is.Empty);
        }

        [Test]
        public void GameSaved_EarnsFirstSave()
        {
            var earned = new List<string>();

            new AchievementRules().OnGameSaved(earned);

            Assert.That(earned, Is.EqualTo(new[] { AchievementIds.FirstSave }));
        }

        [Test]
        public void LockedHiddenAchievement_ShowsPlaceholdersAndNoIcon()
        {
            AchievementDisplay display = AchievementDisplay.For("a.name", "a.description", true, false);

            Assert.That(display.NameKey, Is.EqualTo(AchievementDisplay.HiddenNameKey));
            Assert.That(display.DescriptionKey, Is.EqualTo(AchievementDisplay.HiddenDescriptionKey));
            Assert.That(display.ShowIcon, Is.False);
            Assert.That(display.StatusKey, Is.EqualTo(AchievementDisplay.LockedKey));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void UnlockedAchievement_ShowsItsOwnTexts(bool hidden)
        {
            AchievementDisplay display = AchievementDisplay.For("a.name", "a.description", hidden, true);

            Assert.That(display.NameKey, Is.EqualTo("a.name"));
            Assert.That(display.DescriptionKey, Is.EqualTo("a.description"));
            Assert.That(display.ShowIcon, Is.True);
            Assert.That(display.StatusKey, Is.EqualTo(AchievementDisplay.UnlockedKey));
        }

        [Test]
        public void LockedVisibleAchievement_ShowsItsTextsAndIcon()
        {
            AchievementDisplay display = AchievementDisplay.For("a.name", "a.description", false, false);

            Assert.That(display.NameKey, Is.EqualTo("a.name"));
            Assert.That(display.ShowIcon, Is.True);
            Assert.That(display.Unlocked, Is.False);
        }
    }
}

namespace Game.Systems.Achievements
{
    /// <summary>
    /// What one achievement row shows (pure logic): a hidden achievement keeps placeholder texts until it is unlocked.
    /// </summary>
    internal readonly struct AchievementDisplay
    {
        internal const string HiddenNameKey = "achievements.hidden.name";
        internal const string HiddenDescriptionKey = "achievements.hidden.description";
        internal const string UnlockedKey = "achievements.status.unlocked";
        internal const string LockedKey = "achievements.status.locked";

        private AchievementDisplay(string nameKey, string descriptionKey, bool unlocked, bool showIcon)
        {
            NameKey = nameKey;
            DescriptionKey = descriptionKey;
            Unlocked = unlocked;
            ShowIcon = showIcon;
        }

        internal string NameKey { get; }

        internal string DescriptionKey { get; }

        internal bool Unlocked { get; }

        /// <summary>False for a locked hidden achievement, whose icon would give it away.</summary>
        internal bool ShowIcon { get; }

        internal string StatusKey => Unlocked ? UnlockedKey : LockedKey;

        internal static AchievementDisplay For(string nameKey, string descriptionKey, bool hidden, bool unlocked)
        {
            bool secret = hidden && !unlocked;
            return new AchievementDisplay(secret ? HiddenNameKey : nameKey, secret ? HiddenDescriptionKey : descriptionKey,
                unlocked, !secret);
        }
    }
}

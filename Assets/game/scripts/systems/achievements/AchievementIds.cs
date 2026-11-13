namespace Game.Systems.Achievements
{
    /// <summary>
    /// API names of the demo's achievements, identical to the definitions under <c>data/achievements</c> and to the
    /// achievements set up on the Steamworks partner site.
    /// </summary>
    internal static class AchievementIds
    {
        /// <summary>Any gameplay action pressed.</summary>
        internal const string FirstAction = "ACH_FIRST_ACTION";

        /// <summary>Every tracked gameplay action pressed at least once in one session.</summary>
        internal const string AllActions = "ACH_ALL_ACTIONS";

        /// <summary>The game in play saved (never the first save of a new game); hidden until unlocked.</summary>
        internal const string FirstSave = "ACH_FIRST_SAVE";
    }
}

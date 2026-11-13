namespace Game.Editor.Builders
{
    /// <summary>
    /// Identity and window defaults the project settings step writes into Player Settings. The values are placeholders of
    /// the base; a game cloned from it changes them here. Company and product name also name the folder of
    /// <c>Player.log</c> and of the save files (<c>%USERPROFILE%\AppData\LocalLow\&lt;Company&gt;\&lt;Product&gt;</c>).
    /// </summary>
    internal static class ProjectIdentity
    {
        internal const string CompanyName = "Paowa";
        internal const string ProductName = "Healer Time";

        /// <summary>Window size used when the player is not on the monitor's native resolution (windowed mode).</summary>
        internal const int DefaultScreenWidth = 1920;

        /// <summary>See <see cref="DefaultScreenWidth"/>.</summary>
        internal const int DefaultScreenHeight = 1080;
    }
}

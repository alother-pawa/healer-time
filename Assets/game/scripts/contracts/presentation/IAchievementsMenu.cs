namespace Game.Contracts.Presentation
{
    /// <summary>Opens the achievements popup (from the main menu; any other screen may open it too).</summary>
    public interface IAchievementsMenu
    {
        /// <summary>Shows the achievements popup with the current unlock state; does nothing while it is open.</summary>
        void Open();
    }
}

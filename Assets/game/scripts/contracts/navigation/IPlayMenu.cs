namespace Game.Contracts.Navigation
{
    /// <summary>
    /// What the main menu's Play button opens (the save-slot panel). Registered by the module that owns that flow; while
    /// no module registers it, the main menu shows a notice instead.
    /// </summary>
    public interface IPlayMenu
    {
        /// <summary>Opens the Play flow on top of the main menu.</summary>
        void Open();
    }
}

namespace Game.Contracts.Presentation
{
    /// <summary>Opens the settings popup (from the main menu now, from the pause menu later).</summary>
    public interface ISettingsMenu
    {
        /// <summary>Shows the settings popup on top of the popup stack; does nothing while it is already open.</summary>
        void Open();
    }
}

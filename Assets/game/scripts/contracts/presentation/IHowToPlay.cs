namespace Game.Contracts.Presentation
{
    /// <summary>Opens the How to play popup (from the main menu and from the pause menu).</summary>
    public interface IHowToPlay
    {
        /// <summary>Shows the How to play popup on top of the popup stack; does nothing while it is already open.</summary>
        void Open();
    }
}

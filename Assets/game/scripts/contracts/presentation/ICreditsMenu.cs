namespace Game.Contracts.Presentation
{
    /// <summary>Opens the credits popup (from the main menu; any other screen may open it too).</summary>
    public interface ICreditsMenu
    {
        /// <summary>Shows the credits popup on top of the popup stack, scrolled to the top; does nothing while it is open.</summary>
        void Open();
    }
}

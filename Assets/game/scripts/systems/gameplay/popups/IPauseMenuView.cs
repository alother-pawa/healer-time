namespace Game.Systems.Gameplay
{
    /// <summary>The pause menu as <see cref="PauseController"/> sees it (the popup, or a fake in tests).</summary>
    internal interface IPauseMenuView
    {
        /// <summary>Shows the menu; true when it is open afterwards.</summary>
        bool Show();

        /// <summary>Hides the menu; the view reports it through <see cref="PauseController.OnMenuHidden"/>.</summary>
        void Hide();
    }
}

namespace Game.Systems.Settings
{
    /// <summary>Shows and hides the "Keep these display settings?" popup for <see cref="DisplayConfirmPresenter"/>.</summary>
    internal interface IDisplayConfirmView
    {
        /// <summary>Shows the popup; it reads the change and the countdown from <paramref name="presenter"/> every frame.</summary>
        void Show(DisplayConfirmPresenter presenter);

        /// <summary>Hides the popup; does nothing when it is not open.</summary>
        void Hide();
    }
}

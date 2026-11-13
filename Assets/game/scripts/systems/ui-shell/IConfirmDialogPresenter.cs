namespace Game.Systems.UIShell
{
    /// <summary>Shows and hides the confirmation popup for <see cref="ConfirmDialogService"/> (a fake in tests).</summary>
    internal interface IConfirmDialogPresenter
    {
        /// <summary>Opens the popup for <paramref name="session"/>; the popup answers through the session.</summary>
        void Show(ConfirmDialogSession session);

        /// <summary>Closes the popup; closing a popup that is not open does nothing.</summary>
        void Hide();
    }
}

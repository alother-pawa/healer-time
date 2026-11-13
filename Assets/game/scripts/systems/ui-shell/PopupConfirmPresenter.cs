using Paowa.UI;

namespace Game.Systems.UIShell
{
    /// <summary>Presents confirmations with the registered <see cref="ConfirmDialogPopup"/> on the UI manager's popup stack.</summary>
    internal sealed class PopupConfirmPresenter : IConfirmDialogPresenter
    {
        private readonly IUIManager _ui;
        private readonly UIViewId _view;

        internal PopupConfirmPresenter(IUIManager ui, UIViewId view)
        {
            _ui = ui;
            _view = view;
        }

        public void Show(ConfirmDialogSession session)
        {
            _ui.ShowPopup(_view, session);
        }

        public void Hide()
        {
            _ui.HidePopup(_view);
        }
    }
}

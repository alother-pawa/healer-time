using Paowa.UI;

namespace Game.Systems.Settings
{
    /// <summary><see cref="IDisplayConfirmView"/> over the UI manager's popup stack: the presenter is the popup's payload.</summary>
    internal sealed class PopupDisplayConfirmView : IDisplayConfirmView
    {
        private readonly IUIManager _ui;
        private readonly UIViewId _view;

        internal PopupDisplayConfirmView(IUIManager ui, UIViewId view)
        {
            _ui = ui;
            _view = view;
        }

        public void Show(DisplayConfirmPresenter presenter)
        {
            _ui.ShowPopup(_view, presenter);
        }

        public void Hide()
        {
            _ui.HidePopup(_view);
        }
    }
}

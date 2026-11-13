using Game.Contracts.Presentation;
using Paowa.UI;

namespace Game.Systems.Credits
{
    /// <summary><see cref="ICreditsMenu"/> over the UI manager's popup stack.</summary>
    internal sealed class CreditsMenu : ICreditsMenu
    {
        private readonly IUIManager _ui;
        private readonly UIViewId _popup;

        internal CreditsMenu(IUIManager ui, UIViewId popup)
        {
            _ui = ui;
            _popup = popup;
        }

        public void Open()
        {
            if (!_ui.IsPopupOpen(_popup))
            {
                _ui.ShowPopup(_popup);
            }
        }
    }
}

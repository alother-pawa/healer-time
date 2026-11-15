using Game.Contracts.Presentation;
using Paowa.UI;

namespace Game.Systems.UIShell
{
    /// <summary>Default <see cref="IHowToPlay"/>: shows the How to play popup registered by the module unless it is open.</summary>
    internal sealed class HowToPlayMenu : IHowToPlay
    {
        private readonly IUIManager _ui;
        private readonly UIViewId _popup;

        internal HowToPlayMenu(IUIManager ui, UIViewId popup)
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

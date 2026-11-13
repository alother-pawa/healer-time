using Game.Contracts.Navigation;
using Paowa.UI;

namespace Game.Systems.SaveSlots
{
    /// <summary><see cref="IPlayMenu"/> of the main menu's Play button: shows the slot panel unless it is already open.</summary>
    internal sealed class SaveSlotPlayMenu : IPlayMenu
    {
        private readonly IUIManager _ui;
        private readonly UIViewId _panel;
        private readonly SaveSlotPanelContext _context;

        internal SaveSlotPlayMenu(IUIManager ui, UIViewId panel, SaveSlotPanelContext context)
        {
            _ui = ui;
            _panel = panel;
            _context = context;
        }

        public void Open()
        {
            if (!_ui.IsPopupOpen(_panel))
            {
                _ui.ShowPopup(_panel, _context);
            }
        }
    }
}

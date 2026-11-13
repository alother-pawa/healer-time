using Game.Contracts.Presentation;
using Paowa.UI;

namespace Game.Systems.Settings
{
    /// <summary>Default <see cref="ISettingsMenu"/>: shows the settings popup registered by the module unless it is open.</summary>
    internal sealed class SettingsMenu : ISettingsMenu
    {
        private readonly IUIManager _ui;
        private readonly UIViewId _popup;

        internal SettingsMenu(IUIManager ui, UIViewId popup)
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

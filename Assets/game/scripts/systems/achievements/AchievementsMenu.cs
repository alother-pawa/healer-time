using Game.Contracts.Presentation;
using Paowa.UI;

namespace Game.Systems.Achievements
{
    /// <summary><see cref="IAchievementsMenu"/> over the UI manager's popup stack.</summary>
    internal sealed class AchievementsMenu : IAchievementsMenu
    {
        private readonly IUIManager _ui;
        private readonly UIViewId _popup;

        internal AchievementsMenu(IUIManager ui, UIViewId popup)
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

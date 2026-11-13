using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Systems.Settings
{
    /// <summary>
    /// On a tab button: left/right navigation while the tab is selected switches pages (<see cref="TabGroup.Step"/>, wrapping)
    /// instead of only moving the selection, so the selected tab and the visible page never differ. The button's own
    /// navigation has no left/right neighbour; the popup then selects the new page's tab button.
    /// REFERENCES: <c>_tabs</c> is the popup's tab group.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class SettingsTabButton : MonoBehaviour, IMoveHandler
    {
        [SerializeField] private TabGroup _tabs;

        public void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Left)
            {
                _tabs.Step(-1);
                eventData.Use();
            }
            else if (eventData.moveDir == MoveDirection.Right)
            {
                _tabs.Step(1);
                eventData.Use();
            }
        }
    }
}

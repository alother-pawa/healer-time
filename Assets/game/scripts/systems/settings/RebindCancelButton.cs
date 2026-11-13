using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Cancel button on the cover shown while a key binding slot listens, so a mouse-only player can cancel a rebind. It
    /// acts on pointer down, in the same frame as the press: the Input System's rebind waits briefly after a match before
    /// completing, so the cancel usually wins; if the click was taken as the new control anyway,
    /// <see cref="KeyBindingRebinder"/> puts the previous control back. Navigation is blocked while listening, so only the
    /// pointer reaches it.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class RebindCancelButton : MonoBehaviour, IPointerDownHandler
    {
        /// <summary>Raised on a left-button press over the button.</summary>
        internal event Action Pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Pressed?.Invoke();
            }
        }
    }
}

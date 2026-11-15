using Game.Foundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.UIShell
{
    /// <summary>
    /// The How to play popup: fixed localized text about the board, the gems, items and keys (built into the prefab), and a
    /// Close button. Cancel/Back closes it like every popup.
    /// REFERENCES: <c>_closeButton</c> hides the popup.
    /// </summary>
    internal sealed class HowToPlayPopup : PopupBase
    {
        [SerializeField] private Button _closeButton;

        protected override void Awake()
        {
            base.Awake();
            _closeButton.onClick.AddListener(Close);
        }

        protected override void OnShown(object payload)
        {
        }
    }
}

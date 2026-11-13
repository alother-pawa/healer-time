using UnityEngine;

namespace Game.Systems.UIShell
{
    /// <summary>Popups owned by the UI shell, loaded from <c>Resources/game/config/UIShellConfig</c>.</summary>
    [CreateAssetMenu(menuName = "Game/Config/UI Shell", fileName = "UIShellConfig")]
    internal sealed class UIShellConfig : ScriptableObject
    {
        internal const string ResourcePath = "game/config/UIShellConfig";

        [SerializeField] private ConfirmDialogPopup _confirmDialog;

        /// <summary>Prefab of the confirmation popup.</summary>
        internal ConfirmDialogPopup ConfirmDialog => _confirmDialog;
    }
}

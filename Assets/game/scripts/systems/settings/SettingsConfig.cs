using UnityEngine;

namespace Game.Systems.Settings
{
    /// <summary>Popup prefabs of the settings module, loaded from <c>Resources/game/config/SettingsConfig</c>.</summary>
    [CreateAssetMenu(menuName = "Game/Config/Settings", fileName = "SettingsConfig")]
    internal sealed class SettingsConfig : ScriptableObject
    {
        internal const string ResourcePath = "game/config/SettingsConfig";

        [SerializeField] private SettingsPopup _popup;
        [SerializeField] private DisplayConfirmPopup _displayConfirmPopup;

        /// <summary>Prefab of the settings popup.</summary>
        internal SettingsPopup Popup => _popup;

        /// <summary>Prefab of the "Keep these display settings?" popup.</summary>
        internal DisplayConfirmPopup DisplayConfirmPopup => _displayConfirmPopup;
    }
}

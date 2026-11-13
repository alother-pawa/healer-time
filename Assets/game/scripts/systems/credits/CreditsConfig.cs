using UnityEngine;

namespace Game.Systems.Credits
{
    /// <summary>The credits popup prefab the module registers (written by the builder).</summary>
    [CreateAssetMenu(menuName = "Game/Config/Credits", fileName = "CreditsConfig")]
    internal sealed class CreditsConfig : ScriptableObject
    {
        internal const string ResourcePath = "game/config/CreditsConfig";

        [SerializeField] private CreditsPopup _popup;

        internal CreditsPopup Popup => _popup;
    }
}

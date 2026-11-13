using Game.Contracts.Navigation;
using Paowa.Desktop;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Gameplay settings, loaded from <c>Resources/game/config/GameplayConfig</c> by the module and referenced by the gameplay
    /// screen. The builder writes the references (pause popup, main menu scene) on every run; the tuning values (cursor
    /// policy, look speeds, rumble) are only created with their defaults, so a game's tuning survives a rebuild.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Config/Gameplay", fileName = "GameplayConfig")]
    internal sealed class GameplayConfig : ScriptableObject
    {
        internal const string ResourcePath = "game/config/GameplayConfig";

        [SerializeField] private PauseMenuPopup _pausePopup;
        [SerializeField] private SceneKey _menuScene;
        [Tooltip("Cursor while playing without a popup: Locked (mouse look) or Confined (pointer games). The pause menu "
                 + "requests Menu on top of it.")]
        [SerializeField] private CursorPolicy _cursorPolicy = CursorPolicy.Locked;
        [SerializeField] private LookTuning _look = new LookTuning();
        [Tooltip("Rumble played on Attack when vibration is on.")]
        [SerializeField] private RumbleTuning _attackRumble = new RumbleTuning();

        /// <summary>Prefab of the pause menu popup.</summary>
        internal PauseMenuPopup PausePopup => _pausePopup;

        /// <summary>Scene Quit to main menu opens.</summary>
        internal SceneKey MenuScene => _menuScene;

        /// <summary>Cursor policy of gameplay (never Menu: that is the pause menu's).</summary>
        internal CursorPolicy CursorPolicy => _cursorPolicy == CursorPolicy.Menu ? CursorPolicy.Locked : _cursorPolicy;

        /// <summary>Look speeds before the player's sensitivity.</summary>
        internal LookTuning Look => _look ?? (_look = new LookTuning());

        /// <summary>Rumble on Attack.</summary>
        internal RumbleTuning AttackRumble => _attackRumble ?? (_attackRumble = new RumbleTuning());
    }
}

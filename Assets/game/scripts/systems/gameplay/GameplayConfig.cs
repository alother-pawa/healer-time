using Game.Contracts.Navigation;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Gameplay references, loaded from <c>Resources/game/config/GameplayConfig</c> by the module and referenced by the
    /// gameplay screen. The builder writes them on every run.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Config/Gameplay", fileName = "GameplayConfig")]
    internal sealed class GameplayConfig : ScriptableObject
    {
        internal const string ResourcePath = "game/config/GameplayConfig";

        [SerializeField] private PauseMenuPopup _pausePopup;
        [SerializeField] private SceneKey _menuScene;

        /// <summary>Prefab of the pause menu popup.</summary>
        internal PauseMenuPopup PausePopup => _pausePopup;

        /// <summary>Scene Quit to main menu opens.</summary>
        internal SceneKey MenuScene => _menuScene;
    }
}

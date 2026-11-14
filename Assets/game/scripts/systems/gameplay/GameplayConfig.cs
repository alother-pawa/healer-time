using Game.Contracts.Navigation;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Gameplay references, loaded from <c>Resources/game/config/GameplayConfig</c> by the module and referenced by the
    /// gameplay scene: popups (pause, result), the menu scene, the balance config and the sounds. The builder writes them on every run.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Config/Gameplay", fileName = "GameplayConfig")]
    internal sealed class GameplayConfig : ScriptableObject
    {
        internal const string ResourcePath = "game/config/GameplayConfig";

        [SerializeField] private PauseMenuPopup _pausePopup;
        [SerializeField] private ResultPopup _resultPopup;
        [SerializeField] private SceneKey _menuScene;
        [SerializeField] private BalanceConfig _balance;
        [SerializeField] private GameplaySounds _sounds = new GameplaySounds();

        /// <summary>Prefab of the pause menu popup.</summary>
        internal PauseMenuPopup PausePopup => _pausePopup;

        internal ResultPopup ResultPopup => _resultPopup;

        /// <summary>Scene Quit to main menu opens.</summary>
        internal SceneKey MenuScene => _menuScene;

        internal BalanceConfig Balance => _balance;

        internal GameplaySounds Sounds => _sounds ?? (_sounds = new GameplaySounds());
    }
}

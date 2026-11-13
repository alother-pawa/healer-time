using Game.Contracts.Navigation;
using UnityEngine;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// What the save slot module loads from <c>Resources/game/config/SaveSlotsConfig</c>: the slot panel prefab, the scene a
    /// New game or Continue goes to, and the game-owned <see cref="SaveSlotsData"/>. Written by the builder on every run.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Config/Save Slots", fileName = "SaveSlotsConfig")]
    internal sealed class SaveSlotsConfig : ScriptableObject
    {
        internal const string ResourcePath = "game/config/SaveSlotsConfig";

        [SerializeField] private SaveSlotPanel _panel;
        [SerializeField] private SceneKey _gameplayScene;
        [SerializeField] private SaveSlotsData _data;

        /// <summary>Prefab of the slot panel popup.</summary>
        internal SaveSlotPanel Panel => _panel;

        /// <summary>Scene entered after New game or Continue.</summary>
        internal SceneKey GameplayScene => _gameplayScene;

        /// <summary>Slot ids, data version, migrators and thumbnail settings.</summary>
        internal SaveSlotsData Data => _data;
    }
}

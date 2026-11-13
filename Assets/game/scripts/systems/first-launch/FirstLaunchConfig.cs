using Game.Contracts.Navigation;
using UnityEngine;

namespace Game.Systems.FirstLaunch
{
    /// <summary>
    /// The first-launch screen's destination (written by the builder) and its quick choices (the game's values, kept on
    /// rebuild). Each value is snapped to the setting's own range and step when chosen.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Config/First Launch", fileName = "FirstLaunchConfig")]
    internal sealed class FirstLaunchConfig : ScriptableObject
    {
        [SerializeField] private SceneKey _nextScene;
        [Tooltip("Interface size choices (graphics.ui-scale), 1 = reference size.")]
        [SerializeField] private float[] _uiScaleChoices = { 0.8f, 1f, 1.2f };
        [Tooltip("Brightness choices (display.brightness), 1 = neutral; applied with URP only.")]
        [SerializeField] private float[] _brightnessChoices = { 0.8f, 1f, 1.2f };

        internal SceneKey NextScene => _nextScene;

        internal float[] UIScaleChoices => _uiScaleChoices ?? (_uiScaleChoices = new float[0]);

        internal float[] BrightnessChoices => _brightnessChoices ?? (_brightnessChoices = new float[0]);
    }
}

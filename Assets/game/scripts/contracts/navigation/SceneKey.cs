using UnityEngine;

namespace Game.Contracts.Navigation
{
    /// <summary>
    /// Names a Build Settings scene, so scenes and code refer to a destination asset instead of to each other or to a
    /// scene file.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Navigation/Scene Key", fileName = "SceneKey")]
    public sealed class SceneKey : ScriptableObject
    {
        [Tooltip("Scene name exactly as listed in Build Settings.")]
        [SerializeField] private string _sceneName;
        [Tooltip("Cover the switch to this scene with the loading background only (no title, progress bar or caption), "
                 + "e.g. for the splash, so the studio logo is not preceded by a loading screen.")]
        [SerializeField] private bool _plainTransition;

        /// <summary>Scene name as listed in Build Settings.</summary>
        public string SceneName => _sceneName;

        /// <summary>True when the switch to this scene is covered by the plain loading background.</summary>
        public bool PlainTransition => _plainTransition;
    }
}

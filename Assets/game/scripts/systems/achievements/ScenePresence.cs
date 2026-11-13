using System;
using Game.Contracts.Navigation;
using UnityEngine;

namespace Game.Systems.Achievements
{
    /// <summary>Rich presence shown while a scene is open: the localization key of the Steam <c>status</c> text.</summary>
    [Serializable]
    internal sealed class ScenePresence
    {
        [SerializeField] private SceneKey _scene;
        [SerializeField] private string _statusKey;

        internal SceneKey Scene => _scene;

        internal string StatusKey => _statusKey ?? string.Empty;
    }
}

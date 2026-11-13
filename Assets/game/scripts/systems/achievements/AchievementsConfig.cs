using System.Collections.Generic;
using Game.Contracts.Navigation;
using UnityEngine;

namespace Game.Systems.Achievements
{
    /// <summary>The achievements popup prefab the module registers and the rich presence per scene (written by the builder).</summary>
    [CreateAssetMenu(menuName = "Game/Config/Achievements", fileName = "AchievementsConfig")]
    internal sealed class AchievementsConfig : ScriptableObject
    {
        internal const string ResourcePath = "game/config/AchievementsConfig";

        [SerializeField] private AchievementsPopup _popup;
        [Tooltip("Steam status per scene; a scene without an entry clears the rich presence.")]
        [SerializeField] private List<ScenePresence> _presence = new List<ScenePresence>();

        internal AchievementsPopup Popup => _popup;

        /// <summary>Status key of <paramref name="scene"/>, or empty when it has no entry.</summary>
        internal string StatusKeyOf(SceneKey scene)
        {
            if (scene == null || _presence == null)
            {
                return string.Empty;
            }

            foreach (ScenePresence entry in _presence)
            {
                if (entry != null && entry.Scene == scene)
                {
                    return entry.StatusKey;
                }
            }

            return string.Empty;
        }
    }
}

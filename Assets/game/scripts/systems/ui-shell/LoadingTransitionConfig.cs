using DG.Tweening;
using UnityEngine;

namespace Game.Systems.UIShell
{
    /// <summary>Timing of the loading screen that covers scene switches.</summary>
    [CreateAssetMenu(menuName = "Game/UI/Loading Transition", fileName = "LoadingTransition")]
    internal sealed class LoadingTransitionConfig : ScriptableObject
    {
        [Min(0f)] [SerializeField] private float _coverSeconds = 0.3f;
        [Min(0f)] [SerializeField] private float _revealSeconds = 0.3f;
        [Tooltip("Shortest time the loading screen stays fully covered, so a fast load does not flash. Skipped with animations disabled.")]
        [Min(0f)] [SerializeField] private float _minimumCoveredSeconds = 0.4f;
        [SerializeField] private Ease _ease = Ease.InOutQuad;

        internal float CoverSeconds => _coverSeconds;

        internal float RevealSeconds => _revealSeconds;

        internal float MinimumCoveredSeconds => _minimumCoveredSeconds;

        internal Ease Ease => _ease;
    }
}

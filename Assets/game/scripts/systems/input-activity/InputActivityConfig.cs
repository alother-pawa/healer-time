using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.InputActivity
{
    /// <summary>
    /// Input activity settings, loaded from <c>Resources/game/config/InputActivityConfig</c>: the tracked actions (the builder
    /// writes one per action of the Gameplay map on every run), the vector threshold and the history size (kept when the
    /// builder runs again).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Config/Input Activity", fileName = "InputActivityConfig")]
    internal sealed class InputActivityConfig : ScriptableObject
    {
        internal const string ResourcePath = "game/config/InputActivityConfig";

        [SerializeField] private List<TrackedAction> _actions = new List<TrackedAction>();
        [Tooltip("Magnitude from which a vector action counts as pressed (stick, WASD; mouse delta in pixels per frame).")]
        [SerializeField, Min(0.01f)] private float _vectorThreshold = 0.2f;
        [Tooltip("Entries kept in the recent input history (ring buffer).")]
        [SerializeField, Range(1, 256)] private int _historyCapacity = 32;

        /// <summary>Tracked actions.</summary>
        internal IReadOnlyList<TrackedAction> Actions => _actions;

        /// <summary>Vector press threshold.</summary>
        internal float VectorThreshold => Mathf.Max(0.01f, _vectorThreshold);

        /// <summary>History size.</summary>
        internal int HistoryCapacity => Mathf.Max(1, _historyCapacity);
    }
}

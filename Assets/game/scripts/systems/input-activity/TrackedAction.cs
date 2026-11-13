using System;
using UnityEngine;

namespace Game.Systems.InputActivity
{
    /// <summary>One action watched by the input activity (an entry of <see cref="InputActivityConfig"/>).</summary>
    [Serializable]
    internal sealed class TrackedAction
    {
        [Tooltip("\"Context/Action\" of the game's input actions, e.g. Gameplay/Jump.")]
        [SerializeField] private string _action;
        [SerializeField] private TrackedActionKind _kind;
        [Tooltip("Vector actions: seconds the value may stay below the threshold before the release is recorded (mouse delta "
                 + "is zero on frames without a mouse event, so mouse look needs a short grace).")]
        [SerializeField, Min(0f)] private float _releaseGraceSeconds;

        internal TrackedAction(string action, TrackedActionKind kind, float releaseGraceSeconds)
        {
            _action = action;
            _kind = kind;
            _releaseGraceSeconds = releaseGraceSeconds;
        }

        /// <summary>"Context/Action".</summary>
        internal string Action => _action;

        /// <summary>How the action is read.</summary>
        internal TrackedActionKind Kind => _kind;

        /// <summary>Release grace of a vector action, in seconds (never negative).</summary>
        internal float ReleaseGraceSeconds => Mathf.Max(0f, _releaseGraceSeconds);
    }
}

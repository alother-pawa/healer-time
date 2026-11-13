using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Credits
{
    /// <summary>
    /// The credits content and its scroll pacing. The builder creates a sample once (<c>data/credits/CreditsData</c>);
    /// afterwards the game owns it and edits the sections by hand.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Credits/Credits Data", fileName = "CreditsData")]
    internal sealed class CreditsData : ScriptableObject
    {
        [SerializeField] private List<CreditsSection> _sections = new List<CreditsSection>();
        [Tooltip("Automatic scroll speed in reference pixels per second (animations on only).")]
        [Min(0f)] [SerializeField] private float _autoScrollSpeed = 60f;
        [Tooltip("Scroll speed while Navigate (arrows, WASD, stick, d-pad) is held, in reference pixels per second.")]
        [Min(0f)] [SerializeField] private float _manualScrollSpeed = 900f;
        [Tooltip("Distance of one mouse wheel step, in reference pixels.")]
        [Min(0f)] [SerializeField] private float _wheelStep = 120f;
        [Tooltip("Wait at the top before the automatic scroll starts.")]
        [Min(0f)] [SerializeField] private float _startDelaySeconds = 1.5f;
        [Tooltip("Wait after the last manual scroll before the automatic scroll resumes.")]
        [Min(0f)] [SerializeField] private float _resumeDelaySeconds = 2f;
        [Tooltip("Navigate values below this (stick drift) do not scroll.")]
        [Range(0f, 0.9f)] [SerializeField] private float _stickDeadZone = 0.25f;

        internal IReadOnlyList<CreditsSection> Sections => _sections ?? (_sections = new List<CreditsSection>());

        internal float AutoScrollSpeed => _autoScrollSpeed;

        internal float ManualScrollSpeed => _manualScrollSpeed;

        internal float WheelStep => _wheelStep;

        internal float StartDelaySeconds => _startDelaySeconds;

        internal float ResumeDelaySeconds => _resumeDelaySeconds;

        internal float StickDeadZone => _stickDeadZone;
    }
}

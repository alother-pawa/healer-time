namespace Game.Systems.Credits
{
    /// <summary>
    /// Scroll state of the credits list (pure logic). <see cref="Offset"/> is how far the list is scrolled from the top,
    /// clamped to [0, <see cref="MaxOffset"/>]. With animations on, the list waits the start delay at the top, then
    /// scrolls by itself and stops at the end; manual input (Navigate, wheel) moves it at once and holds the automatic
    /// scroll for the resume delay. With animations off nothing moves by itself: the list stays where it is (at the top
    /// when opened) and scrolls only by hand, so both modes show the same list.
    /// </summary>
    internal sealed class CreditsScroll
    {
        private readonly float _autoSpeed;
        private readonly float _manualSpeed;
        private readonly float _wheelStep;
        private readonly float _startDelay;
        private readonly float _resumeDelay;
        private float _wait;

        internal CreditsScroll(float autoSpeed, float manualSpeed, float wheelStep, float startDelay, float resumeDelay)
        {
            _autoSpeed = autoSpeed < 0f ? 0f : autoSpeed;
            _manualSpeed = manualSpeed < 0f ? 0f : manualSpeed;
            _wheelStep = wheelStep < 0f ? 0f : wheelStep;
            _startDelay = startDelay < 0f ? 0f : startDelay;
            _resumeDelay = resumeDelay < 0f ? 0f : resumeDelay;
            _wait = _startDelay;
        }

        /// <summary>Distance scrolled from the top, in the list's units.</summary>
        internal float Offset { get; private set; }

        /// <summary>Largest offset: content height minus viewport height, never negative.</summary>
        internal float MaxOffset { get; private set; }

        /// <summary>True when the end of the list is visible.</summary>
        internal bool IsAtEnd => Offset >= MaxOffset;

        /// <summary>Back to the top with the start delay; used each time the credits open.</summary>
        internal void Reset()
        {
            Offset = 0f;
            _wait = _startDelay;
        }

        /// <summary>Sets the sizes (read every frame: language and text size change them) and clamps the offset.</summary>
        internal void SetExtent(float contentHeight, float viewportHeight)
        {
            float max = contentHeight - viewportHeight;
            MaxOffset = max > 0f ? max : 0f;
            Offset = Clamp(Offset);
        }

        /// <summary>
        /// Advances one frame. <paramref name="manualAxis"/> is −1..1 (positive scrolls toward the end),
        /// <paramref name="wheelSteps"/> the wheel notches this frame (positive toward the end), <paramref name="autoScroll"/>
        /// whether animations are on.
        /// </summary>
        internal void Tick(float deltaTime, float manualAxis, float wheelSteps, bool autoScroll)
        {
            if (!(deltaTime > 0f))
            {
                deltaTime = 0f;
            }

            if (manualAxis != 0f || wheelSteps != 0f)
            {
                Offset = Clamp(Offset + manualAxis * _manualSpeed * deltaTime + wheelSteps * _wheelStep);
                _wait = _resumeDelay;
                return;
            }

            if (!autoScroll)
            {
                return;
            }

            if (_wait > 0f)
            {
                _wait -= deltaTime;
                return;
            }

            Offset = Clamp(Offset + _autoSpeed * deltaTime);
        }

        private float Clamp(float value)
        {
            return value < 0f ? 0f : value > MaxOffset ? MaxOffset : value;
        }
    }
}

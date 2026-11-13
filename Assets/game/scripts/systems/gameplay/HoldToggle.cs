namespace Game.Systems.Gameplay
{
    /// <summary>
    /// State of a held action that can also work as a toggle (accessibility "hold to toggle"), pure logic. Hold mode: active
    /// while the button is held. Toggle mode: each press switches it. Switching the mode starts from the button as it is
    /// (active only while held), so a latched state never survives a mode change.
    /// </summary>
    internal sealed class HoldToggle
    {
        private bool _toggleMode;

        /// <summary>True while the action is on (sprinting, crouching).</summary>
        internal bool IsActive { get; private set; }

        /// <summary>Advances one frame with the current mode and the button's state.</summary>
        internal void Tick(bool toggleMode, bool pressedThisFrame, bool held)
        {
            if (toggleMode != _toggleMode)
            {
                _toggleMode = toggleMode;
                IsActive = held;
                return;
            }

            if (!toggleMode)
            {
                IsActive = held;
            }
            else if (pressedThisFrame)
            {
                IsActive = !IsActive;
            }
        }

        /// <summary>Off (leaving gameplay).</summary>
        internal void Reset()
        {
            IsActive = false;
        }
    }
}

namespace Game.Contracts.Interaction
{
    /// <summary>Kind of an <see cref="InputActivityEntry"/>.</summary>
    public enum InputActivityPhase
    {
        /// <summary>The action started (button down, vector past the threshold).</summary>
        Pressed = 0,

        /// <summary>The action ended.</summary>
        Released = 1
    }
}

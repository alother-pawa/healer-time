namespace Game.Contracts.Presentation
{
    /// <summary>
    /// Global switch for presentation animation. When disabled every animation jumps to its end state at once, so the UI
    /// shows the same result and the flow continues without waiting.
    /// </summary>
    public interface IAnimationSettings
    {
        /// <summary>Whether animations play; persisted.</summary>
        bool Enabled { get; set; }
    }
}

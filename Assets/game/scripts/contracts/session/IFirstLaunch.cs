namespace Game.Contracts.Session
{
    /// <summary>
    /// Whether the first-launch screen (language, photosensitivity notice, quick display choices) is still due. The flag is
    /// stored in the global save store, so it is per install and outlives save slots. The splash asks it to pick its next
    /// scene; the first-launch screen completes it when the player continues or skips.
    /// </summary>
    public interface IFirstLaunch
    {
        /// <summary>True until <see cref="Complete"/> was called once on this install.</summary>
        bool IsPending { get; }

        /// <summary>Marks the first launch as done and writes the global store right away.</summary>
        void Complete();

        /// <summary>Makes the first-launch screen due again (development and tests).</summary>
        void Reset();
    }
}

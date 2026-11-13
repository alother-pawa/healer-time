namespace Game.Contracts.Session
{
    /// <summary>
    /// Data that outlives a scene (the last run's summary, a pending request for the next scene). Holds one value per type,
    /// so each module stores its own Contracts type without the session knowing about it. Not persisted.
    /// </summary>
    public interface ISessionContext
    {
        /// <summary>Stores <paramref name="value"/>, replacing the previous value of the same type.</summary>
        void Set<T>(T value) where T : class;

        /// <summary>Reads the value of type <typeparamref name="T"/>; false when none is stored.</summary>
        bool TryGet<T>(out T value) where T : class;

        /// <summary>Removes the value of type <typeparamref name="T"/>; false when none was stored.</summary>
        bool Remove<T>() where T : class;
    }
}

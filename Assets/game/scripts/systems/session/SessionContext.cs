using System;
using System.Collections.Generic;
using Game.Contracts.Session;

namespace Game.Systems.Session
{
    /// <summary>Default <see cref="ISessionContext"/>: one value per type, kept for the application's lifetime.</summary>
    internal sealed class SessionContext : ISessionContext
    {
        private readonly Dictionary<Type, object> _values = new Dictionary<Type, object>();

        public void Set<T>(T value) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _values[typeof(T)] = value;
        }

        public bool TryGet<T>(out T value) where T : class
        {
            bool found = _values.TryGetValue(typeof(T), out object stored);
            value = found ? (T)stored : null;
            return found;
        }

        public bool Remove<T>() where T : class
        {
            return _values.Remove(typeof(T));
        }
    }
}

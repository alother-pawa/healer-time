using System;
using Game.Contracts.Session;
using Paowa.Core.Persistence;
using Paowa.Save;

namespace Game.Systems.FirstLaunch
{
    /// <summary>
    /// <see cref="IFirstLaunch"/> over the global save store (key <see cref="DoneKey"/>). Completing writes the store at
    /// once through <c>flush</c>, so a crash right after the screen never shows it again. Without a store (no save module)
    /// the flag could not be remembered, so the screen is never due rather than shown on every start.
    /// </summary>
    internal sealed class FirstLaunchState : IFirstLaunch
    {
        internal const string DoneKey = "game.firstlaunch.done";

        private readonly ISaveStore _store;
        private readonly Func<bool> _flush;

        internal FirstLaunchState(ISaveStore store, Func<bool> flush)
        {
            _store = store;
            _flush = flush;
        }

        public bool IsPending => _store != null && !_store.Get(DoneKey, false);

        public void Complete()
        {
            if (_store == null)
            {
                return;
            }

            _store.Set(DoneKey, true);
            Flush();
        }

        public void Reset()
        {
            if (_store == null)
            {
                return;
            }

            _store.Delete(DoneKey);
            Flush();
        }

        // A failed write is logged and published by Core; the value stays in memory and is written on quit.
        private void Flush()
        {
            if (_flush != null)
            {
                _flush();
            }
        }
    }
}

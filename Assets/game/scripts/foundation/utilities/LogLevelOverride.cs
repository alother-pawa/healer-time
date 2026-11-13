using Paowa.Core.Foundation.Logging;
using Paowa.Core.Foundation.Services;
using UnityEngine;

namespace Game.Foundation.Utilities
{
    /// <summary>
    /// Sets Core's log level for the whole session once Core has started (after the first scene loads). Null keeps Core's
    /// default: Info in the Editor and development builds, Warning in release builds. Only logs written through
    /// <see cref="ICoreLogger"/> are filtered; direct <c>Debug.Log</c> calls are not.
    /// </summary>
    internal static class LogLevelOverride
    {
        // Info = everything, Warning = warnings and errors, Error = errors only, Off = nothing; null = Core's default.
        private static readonly LogLevel? Level = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            if (Level.HasValue && CoreServiceLocator.TryGet(out ICoreLogger logger))
            {
                logger.MinimumLevel = Level.Value;
            }
        }
    }
}

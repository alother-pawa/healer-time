using System;

namespace Game.Foundation.Utilities
{
    /// <summary>Handle that runs an action on its first <see cref="Dispose"/>; later calls do nothing.</summary>
    public sealed class DisposableAction : IDisposable
    {
        private Action _onDispose;

        /// <summary>Creates a handle that runs <paramref name="onDispose"/> once.</summary>
        public DisposableAction(Action onDispose)
        {
            _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        }

        public void Dispose()
        {
            Action onDispose = _onDispose;
            _onDispose = null;
            onDispose?.Invoke();
        }
    }
}

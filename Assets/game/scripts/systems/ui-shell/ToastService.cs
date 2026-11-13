using System;
using System.Collections.Generic;
using Game.Contracts.Presentation;
using Paowa.Core.Foundation.Logging;
using UnityEngine;

namespace Game.Systems.UIShell
{
    /// <summary>
    /// Default <see cref="IToastService"/>: a queue shown one message at a time by the <see cref="ToastHost"/> of the UI
    /// scene. Messages wait in the queue until a host attaches.
    /// </summary>
    internal sealed class ToastService : IToastService
    {
        private static readonly LogCategory Category = new LogCategory("Toast", new Color(1f, 0.9f, 0.5f));

        private readonly Queue<string> _pending = new Queue<string>();
        private readonly ICoreLogger _logger;
        private ToastHost _host;
        private bool _isPlaying;

        internal ToastService(ICoreLogger logger)
        {
            _logger = logger;
        }

        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Toast message must not be empty.", nameof(message));
            }

            _pending.Enqueue(message);
            _ = DrainAsync();
        }

        internal void Attach(ToastHost host)
        {
            _host = host;
            _ = DrainAsync();
        }

        internal void Detach(ToastHost host)
        {
            if (_host == host)
            {
                _host = null;
            }
        }

        private async Awaitable DrainAsync()
        {
            if (_isPlaying)
            {
                return;
            }

            _isPlaying = true;
            try
            {
                while (_host != null && _pending.Count > 0)
                {
                    await _host.PlayAsync(_pending.Dequeue());
                }
            }
            catch (OperationCanceledException)
            {
                // The host was destroyed mid-toast; remaining messages wait for the next host.
            }
            catch (Exception exception)
            {
                _logger.LogError(Category, $"Showing a toast failed: {exception}");
            }
            finally
            {
                _isPlaying = false;
            }
        }
    }
}

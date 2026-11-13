using System;
using System.Collections.Generic;
using Paowa.Core.Messaging;

namespace Game.Tests.Editor
{
    /// <summary>Synchronous <see cref="IEventBus"/> for tests (Core's bus is internal): handlers run in subscription order.</summary>
    internal sealed class TestEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public void Publish<T>(in T evt) where T : struct, IEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out List<Delegate> handlers))
            {
                return;
            }

            foreach (Delegate handler in handlers.ToArray())
            {
                ((Action<T>)handler)(evt);
            }
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out List<Delegate> handlers))
            {
                handlers = new List<Delegate>();
                _handlers[typeof(T)] = handlers;
            }

            handlers.Add(handler);
            return new Subscription(() => handlers.Remove(handler));
        }

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;

            internal Subscription(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                Action dispose = _dispose;
                _dispose = null;
                if (dispose != null)
                {
                    dispose();
                }
            }
        }
    }
}

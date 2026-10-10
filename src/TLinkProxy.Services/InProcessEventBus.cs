using TLinkProxy.Abstractions;

namespace TLinkProxy.Services
{
    public sealed class InProcessEventBus : IEventBus
    {
        private readonly Lock _gate = new();
        private readonly Dictionary<Type, List<Delegate>> _handlers = [];

        public IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : class
        {
            ArgumentNullException.ThrowIfNull(handler);

            Type eventType = typeof(TEvent);
            Action<TEvent> callback = handler.Handle;

            lock (_gate)
            {
                if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
                {
                    handlers = [];
                    _handlers[eventType] = handlers;
                }

                handlers.Add(callback);
            }

            return new Subscription(() => Unsubscribe(eventType, callback));
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : class
        {
            ArgumentNullException.ThrowIfNull(@event);

            Type eventType = typeof(TEvent);
            Delegate[] handlers;

            lock (_gate)
            {
                if (!_handlers.TryGetValue(eventType, out List<Delegate> list))
                {
                    return;
                }

                handlers = [.. list];
            }

            foreach (Delegate handler in handlers)
            {
                try
                {
                    ((Action<TEvent>)handler)(@event);
                }
                catch
                {
                    // One faulty subscriber must not break delivery to the rest.
                }
            }
        }

        private void Unsubscribe<TEvent>(Type eventType, Action<TEvent> callback)
        {
            lock (_gate)
            {
                if (!_handlers.TryGetValue(eventType, out List<Delegate> list))
                {
                    return;
                }

                list.Remove(callback);

                if (list.Count == 0)
                {
                    _handlers.Remove(eventType);
                }
            }
        }

        private sealed class Subscription : IDisposable
        {
            private Action _unsubscribe;

            public Subscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
            }
        }
    }
}

using TLinkProxy.Abstractions;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace TLinkProxy.Scheduling
{
    public sealed class ModelScheduler : IModelScheduler, IDisposable
    {
        private const int DefaultQueueDepth = 64;
        private const int DefaultGlobalQueueCapacity = 256;

        private readonly IAiBackendRegistry _registry;
        private readonly ConcurrentDictionary<string, ConcurrencyGate> _gates = new();
        private readonly ConcurrentDictionary<string, int> _gateLimits = new();

        private readonly Channel<int> _globalTokens;
        private int _disposed;

        public ModelScheduler(IAiBackendRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));

            int capacity = Math.Max(1, registry.Current?.GlobalQueueCapacity ?? DefaultGlobalQueueCapacity);
            _globalTokens = Channel.CreateBounded<int>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false
            });

            for (int i = 0; i < capacity; i++)
            {
                _globalTokens.Writer.TryWrite(0);
            }
        }

        public Task<IDisposable> AcquireAsync(string backendAlias, int timeoutMs, CancellationToken ct)
        {
            if (String.IsNullOrWhiteSpace(backendAlias))
            {
                return Task.FromResult<IDisposable>(null);
            }

            ConcurrencyGate gate = GetGate(backendAlias);
            return gate.AcquireAsync(timeoutMs, ct);
        }

        public int GetQueueDepth(string backendAlias)
        {
            if (String.IsNullOrWhiteSpace(backendAlias))
            {
                return 0;
            }

            return GetGate(backendAlias).QueueDepth;
        }

        public int GetAvailableSlots(string backendAlias)
        {
            if (String.IsNullOrWhiteSpace(backendAlias))
            {
                return 0;
            }

            return GetGate(backendAlias).AvailableSlots;
        }

        public Task<IDisposable> AcquireGlobalAsync(int timeoutMs, CancellationToken ct)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return Task.FromResult<IDisposable>(null);
            }

            // Back-pressure valve: take a token immediately. When none are available the
            // global queue is saturated and the caller should return 429.
            if (_globalTokens.Reader.TryRead(out _))
            {
                return Task.FromResult<IDisposable>(new GlobalReleaser(_globalTokens));
            }

            return Task.FromResult<IDisposable>(null);
        }

        private ConcurrencyGate GetGate(string alias)
        {
            IAiModelConfig modelConfig = _registry.GetBackendByAlias(alias);
            int limit = Math.Max(1, modelConfig?.ConcurrencyLimit ?? 1);
            int queueDepth = modelConfig?.AllowQueue == false
                ? 0
                : Math.Max(0, modelConfig?.MaxQueueDepth ?? DefaultQueueDepth);

            // If the configured concurrency limit changed (hot-reload), swap in a fresh gate so
            // new requests observe the updated limit. In-flight requests keep using the old gate.
            if (_gateLimits.TryGetValue(alias, out int existingLimit) && existingLimit != limit)
            {
                _gates.TryRemove(alias, out ConcurrencyGate _);
                _gateLimits.TryUpdate(alias, limit, existingLimit);
            }
            else
            {
                _gateLimits.TryAdd(alias, limit);
            }

            return _gates.GetOrAdd(alias, _ => new ConcurrencyGate(limit, queueDepth));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                _globalTokens.Writer.TryComplete();
            }
            catch
            {
            }

            foreach (ConcurrencyGate gate in _gates.Values)
            {
                try { gate.Dispose(); } catch { }
            }

            _gates.Clear();
            _gateLimits.Clear();
        }

        private sealed class GlobalReleaser : IDisposable
        {
            private readonly Channel<int> _tokens;
            private int _released;

            public GlobalReleaser(Channel<int> tokens) => _tokens = tokens;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    try
                    {
                        _tokens.Writer.TryWrite(0);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}

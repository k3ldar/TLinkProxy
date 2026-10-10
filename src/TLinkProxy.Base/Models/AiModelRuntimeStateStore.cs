using TLinkProxy.Abstractions;
using System.Collections.Concurrent;


namespace TLinkProxy.Base.Models
{
    public sealed class AiModelRuntimeStateStore : IAiModelRuntimeStateStore
    {
        private readonly ConcurrentDictionary<string, bool> _health = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, bool> _running = new(StringComparer.OrdinalIgnoreCase);

        public bool? GetHealth(string alias)
        {
            if (String.IsNullOrWhiteSpace(alias))
            {
                return null;
            }

            return _health.TryGetValue(alias, out bool healthy) ? healthy : null;
        }

        public void SetHealth(string alias, bool healthy)
        {
            if (String.IsNullOrWhiteSpace(alias))
            {
                return;
            }

            _health[alias] = healthy;
        }

        public bool GetRunning(string alias)
        {
            if (String.IsNullOrWhiteSpace(alias))
            {
                return false;
            }

            return _running.TryGetValue(alias, out bool isRunning) && isRunning;
        }

        public void SetRunning(string alias, bool isRunning)
        {
            if (String.IsNullOrWhiteSpace(alias))
            {
                return;
            }

            _running[alias] = isRunning;
        }
    }
}

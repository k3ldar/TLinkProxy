using SimpleDB;

using TLinkProxy.Abstractions;
using TLinkProxy.Base.Models;
using TLinkProxy.Db.Tables;

namespace TLinkProxy.Db.Internal
{
    internal sealed class SessionMetricStore : ISessionMetricStore
    {
        private readonly ISimpleDBOperations<SessionMetricDataRow> _metricsData;
        private readonly Lock _countsGate = new();
        private Dictionary<string, int> _toolCounts;

        public SessionMetricStore(ISimpleDBOperations<SessionMetricDataRow> metricsData)
        {
            ArgumentNullException.ThrowIfNull(metricsData);
            _metricsData = metricsData;
        }

        public void Record(ISessionMetricRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            // Held for the whole write so a concurrent first count build cannot double count this record.
            lock (_countsGate)
            {
                SessionMetricDataRow session = _metricsData.Select(s => s.SessionId == record.SessionId).FirstOrDefault();

                if (session == null)
                {
                    session = new()
                    {
                        Model = record.Model,
                        SessionId = record.SessionId
                    };

                    _metricsData.Insert(session);
                }

                foreach (IToolInvocation invocation in record.ToolInvocations)
                {
                    session.ToolInvocations.Add(new Base.Models.ToolInvocation
                    {
                        Name = invocation.Name,
                        Arguments = invocation.Arguments,
                        Outcome = invocation.Outcome,
                    });
                }

                _metricsData.Update(session);

                AddToolCounts(record.ToolInvocations);
            }
        }

        public IReadOnlyDictionary<string, int> GetToolUsageCounts()
        {
            lock (_countsGate)
            {
                // Built once from persisted data, then kept current by Record.
                _toolCounts ??= BuildToolCounts();

                return new Dictionary<string, int>(_toolCounts, StringComparer.OrdinalIgnoreCase);
            }
        }

        private Dictionary<string, int> BuildToolCounts()
        {
            Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);

            foreach (SessionMetricDataRow session in _metricsData.Select())
            {
                foreach (Base.Models.ToolInvocation invocation in session.ToolInvocations.ToList())
                {
                    IncrementCount(counts, invocation?.Name);
                }
            }

            return counts;
        }

        private void AddToolCounts(IEnumerable<IToolInvocation> invocations)
        {
            if (_toolCounts == null)
            {
                return;
            }

            foreach (IToolInvocation invocation in invocations)
            {
                IncrementCount(_toolCounts, invocation?.Name);
            }
        }

        private static void IncrementCount(Dictionary<string, int> counts, string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return;
            }

            counts[name] = counts.TryGetValue(name, out int existing) ? existing + 1 : 1;
        }

        public List<ISessionMetricRecord> GetAll()
        {
            List<ISessionMetricRecord> result = [];

            List<SessionMetricDataRow> sessions = [.. _metricsData.Select()];

            foreach (SessionMetricDataRow session in sessions)
            {
                SessionMetricRecord record = new()
                {
                    Model = session.Model,
                    RecordedAtUtc = session.Created,
                    SessionId = session.SessionId,
                };

                foreach (var invocation in session.ToolInvocations.ToList())
                {
                    ToolInvocation toolInvocation = new()
                    {
                        Name = invocation.Name,
                        Arguments = invocation.Arguments,
                        Outcome = invocation.Outcome
                    };

                    record.ToolInvocations.Add(toolInvocation);
                }

                result.Add(record);
            }

            return result;
        }
    }
}

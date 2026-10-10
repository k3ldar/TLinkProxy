using TLinkProxy.Abstractions;
using TLinkProxy.Base.Models;
using TLinkProxy.Db.Tables;
using SimpleDB;

namespace TLinkProxy.Db.Internal
{
    internal sealed class SessionMetricStore : ISessionMetricStore
    {
        private readonly ISimpleDBOperations<SessionMetricDataRow> _metricsData;

        public SessionMetricStore(ISimpleDBOperations<SessionMetricDataRow> metricsData)
        {
            ArgumentNullException.ThrowIfNull(metricsData);
            _metricsData = metricsData;
        }

        public void Record(ISessionMetricRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

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
        }

        public List<ISessionMetricRecord> GetAll()
        {
            List<ISessionMetricRecord> result = new();

            List<SessionMetricDataRow> sessions = _metricsData.Select().ToList();

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

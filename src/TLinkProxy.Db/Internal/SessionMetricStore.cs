#pragma warning disable CC0009, CC0010 // SQL parameter names are clearer inline

using Microsoft.Data.Sqlite;
using TLinkProxy.Abstractions;
using TLinkProxy.Base.Models;

namespace TLinkProxy.Db.Internal
{
    internal sealed class SessionMetricStore : ISessionMetricStore
    {
        private readonly SqliteDatabase _database;
        private readonly Lock _countsGate = new();
        private Dictionary<string, int> _toolCounts;

        public SessionMetricStore(SqliteDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public void Record(ISessionMetricRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            // Held for the whole write so a concurrent first count build cannot double count this record.
            lock (_countsGate)
            {
                using SqliteConnection connection = _database.CreateConnection();
                using SqliteTransaction transaction = connection.BeginTransaction();

                long id;
                List<Base.Models.ToolInvocation> invocations;

                using (SqliteCommand select = connection.CreateCommand(
                    "SELECT Id, ToolInvocations FROM SessionMetrics WHERE SessionId = $sessionId ORDER BY Id LIMIT 1;", transaction)
                    .With("$sessionId", record.SessionId))
                using (SqliteDataReader reader = select.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        id = reader.GetInt64("Id");
                        invocations = DbJson.Deserialize<List<Base.Models.ToolInvocation>>(reader.GetStringOrNull("ToolInvocations")) ?? [];
                    }
                    else
                    {
                        id = 0;
                        invocations = [];
                    }
                }

                string now = DbTime.Format(DateTime.UtcNow);

                if (id == 0)
                {
                    using SqliteCommand insert = connection.CreateCommand("""
                        INSERT INTO SessionMetrics (Model, SessionId, ToolInvocations, Created, Updated)
                        VALUES ($model, $sessionId, $toolInvocations, $now, $now);
                        SELECT last_insert_rowid();
                        """, transaction)
                        .With("$model", record.Model)
                        .With("$sessionId", record.SessionId)
                        .With("$toolInvocations", DbJson.Serialize(invocations))
                        .With("$now", now);

                    id = Convert.ToInt64(insert.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
                }

                foreach (IToolInvocation invocation in record.ToolInvocations)
                {
                    invocations.Add(new Base.Models.ToolInvocation
                    {
                        Name = invocation.Name,
                        Arguments = invocation.Arguments,
                        Outcome = invocation.Outcome,
                    });
                }

                using SqliteCommand update = connection.CreateCommand(
                    "UPDATE SessionMetrics SET ToolInvocations = $toolInvocations, Updated = $now WHERE Id = $id;", transaction)
                    .With("$toolInvocations", DbJson.Serialize(invocations))
                    .With("$now", now)
                    .With("$id", id);

                update.ExecuteNonQuery();
                transaction.Commit();

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

        public List<ISessionMetricRecord> GetAll()
        {
            List<ISessionMetricRecord> result = [];

            using SqliteConnection connection = _database.CreateConnection();
            using SqliteCommand command = connection.CreateCommand(
                "SELECT Model, SessionId, ToolInvocations, Created FROM SessionMetrics ORDER BY Id;");
            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                SessionMetricRecord record = new()
                {
                    Model = reader.GetStringOrNull("Model"),
                    RecordedAtUtc = DbTime.Parse(reader.GetStringOrNull("Created")),
                    SessionId = reader.GetStringOrNull("SessionId"),
                };

                foreach (Base.Models.ToolInvocation invocation in DbJson.Deserialize<List<Base.Models.ToolInvocation>>(reader.GetStringOrNull("ToolInvocations")) ?? [])
                {
                    record.ToolInvocations.Add(new Base.Models.ToolInvocation
                    {
                        Name = invocation.Name,
                        Arguments = invocation.Arguments,
                        Outcome = invocation.Outcome
                    });
                }

                result.Add(record);
            }

            return result;
        }

        private Dictionary<string, int> BuildToolCounts()
        {
            Dictionary<string, int> counts = [with(StringComparer.OrdinalIgnoreCase)];

            using SqliteConnection connection = _database.CreateConnection();
            using SqliteCommand command = connection.CreateCommand("SELECT ToolInvocations FROM SessionMetrics;");
            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                foreach (Base.Models.ToolInvocation invocation in DbJson.Deserialize<List<Base.Models.ToolInvocation>>(reader.GetStringOrNull("ToolInvocations")) ?? [])
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
    }
}

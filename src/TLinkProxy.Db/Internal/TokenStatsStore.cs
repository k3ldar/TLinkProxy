#pragma warning disable CC0009, CC0010 // SQL parameter names are clearer inline

using System.Globalization;
using Microsoft.Data.Sqlite;
using TLinkProxy.Abstractions;
using TLinkProxy.Base.Models;
using static TLinkProxy.Base.Constants;

namespace TLinkProxy.Db.Internal
{
    internal sealed class TokenStatsStore : ITokenStatsStore
    {
        private const string SelectColumns = "Id, RequestCount, OriginalTokens, OptimizedTokens, PerModel, CreatedDay";

        private readonly SqliteDatabase _database;
        private readonly Lock _writeGate = new();

        public TokenStatsStore(SqliteDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task RecordAsync(string sessionId, int originalBytes, int optimizedBytes, string modelName)
        {
            long originalTokens = (long)Math.Ceiling((double)originalBytes / CharsPerToken);
            long optimizedTokens = (long)Math.Ceiling((double)optimizedBytes / CharsPerToken);

            lock (_writeGate)
            {
                using SqliteConnection connection = _database.CreateConnection();
                using SqliteTransaction transaction = connection.BeginTransaction();

                TokenDailyRow row = GetOrCreateToday(connection, transaction);

                row.RequestCount++;
                row.OriginalTokens += originalTokens;
                row.OptimizedTokens += optimizedTokens;

                if (!row.PerModel.TryGetValue(modelName, out TokenStatsModel modelStats))
                {
                    modelStats = new TokenStatsModel();
                    row.PerModel[modelName] = modelStats;
                }

                modelStats.RequestCount++;
                modelStats.OriginalTokens += originalTokens;
                modelStats.OptimizedTokens += optimizedTokens;

                using SqliteCommand update = connection.CreateCommand("""
                    UPDATE DailyTokenCounts
                    SET RequestCount = $requestCount, OriginalTokens = $originalTokens,
                        OptimizedTokens = $optimizedTokens, PerModel = $perModel, Updated = $updated
                    WHERE Id = $id;
                    """, transaction)
                    .With("$requestCount", (long)row.RequestCount)
                    .With("$originalTokens", row.OriginalTokens)
                    .With("$optimizedTokens", row.OptimizedTokens)
                    .With("$perModel", DbJson.Serialize(row.PerModel))
                    .With("$updated", DbTime.Format(DateTime.UtcNow))
                    .With("$id", row.Id);

                update.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public ITokenDailyRecord GetToday()
        {
            lock (_writeGate)
            {
                using SqliteConnection connection = _database.CreateConnection();
                using SqliteTransaction transaction = connection.BeginTransaction();

                TokenDailyRow row = GetOrCreateToday(connection, transaction);
                transaction.Commit();

                return ConvertRowToRecord(row);
            }
        }

        public ITokenDailyRecord GetThisMonth()
        {
            DateTime now = DateTime.UtcNow;
            DateTime startOfMonth = new(now.Year, now.Month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            using SqliteConnection connection = _database.CreateConnection();
            using SqliteCommand command = connection.CreateCommand(
                $"SELECT {SelectColumns} FROM DailyTokenCounts WHERE CreatedDay >= $start AND CreatedDay <= $end;")
                .With("$start", startOfMonth.ToString(TodayFormat, CultureInfo.InvariantCulture))
                .With("$end", endOfMonth.ToString(TodayFormat, CultureInfo.InvariantCulture));

            return Aggregate(ReadRows(command), DateTimeOffset.UtcNow.ToString(MonthFormat));
        }

        public ITokenDailyRecord GetLifetime()
        {
            using SqliteConnection connection = _database.CreateConnection();
            using SqliteCommand command = connection.CreateCommand($"SELECT {SelectColumns} FROM DailyTokenCounts;");

            return Aggregate(ReadRows(command), LifetimeAggregateLabel);
        }

        private static TokenDailyRow GetOrCreateToday(SqliteConnection connection, SqliteTransaction transaction)
        {
            DateTime utcNow = DateTime.UtcNow;
            string today = utcNow.ToString(TodayFormat, CultureInfo.InvariantCulture);

            using (SqliteCommand select = connection.CreateCommand(
                $"SELECT {SelectColumns} FROM DailyTokenCounts WHERE CreatedDay = $day ORDER BY Id LIMIT 1;", transaction)
                .With("$day", today))
            {
                TokenDailyRow existing = ReadRows(select).FirstOrDefault();

                if (existing != null)
                {
                    return existing;
                }
            }

            string now = DbTime.Format(utcNow);

            using SqliteCommand insert = connection.CreateCommand("""
                INSERT INTO DailyTokenCounts (RequestCount, OriginalTokens, OptimizedTokens, PerModel, Created, CreatedTicks, CreatedDay, Updated)
                VALUES (0, 0, 0, $perModel, $now, $ticks, $day, $now);
                SELECT last_insert_rowid();
                """, transaction)
                .With("$perModel", DbJson.Serialize(new Dictionary<string, TokenStatsModel>()))
                .With("$now", now)
                .With("$ticks", utcNow.Ticks)
                .With("$day", today);

            return new TokenDailyRow
            {
                Id = Convert.ToInt64(insert.ExecuteScalar(), CultureInfo.InvariantCulture),
                CreatedDay = today
            };
        }

        private static List<TokenDailyRow> ReadRows(SqliteCommand command)
        {
            List<TokenDailyRow> rows = [];

            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                rows.Add(new TokenDailyRow
                {
                    Id = reader.GetInt64("Id"),
                    RequestCount = (int)reader.GetInt64("RequestCount"),
                    OriginalTokens = reader.GetInt64("OriginalTokens"),
                    OptimizedTokens = reader.GetInt64("OptimizedTokens"),
                    PerModel = DbJson.Deserialize<Dictionary<string, TokenStatsModel>>(reader.GetStringOrNull("PerModel")) ?? [],
                    CreatedDay = reader.GetStringOrNull("CreatedDay")
                });
            }

            return rows;
        }

        private static TokenDailyRecord Aggregate(IEnumerable<TokenDailyRow> rows, string label)
        {
            TokenDailyRecord aggregate = new()
            {
                Date = label,
                RequestCount = 0,
                OriginalTokens = 0,
                OptimizedTokens = 0,
                PerModel = []
            };

            foreach (TokenDailyRow row in rows)
            {
                aggregate.RequestCount += row.RequestCount;
                aggregate.OriginalTokens += row.OriginalTokens;
                aggregate.OptimizedTokens += row.OptimizedTokens;

                foreach (KeyValuePair<string, TokenStatsModel> modelEntry in row.PerModel)
                {
                    if (!aggregate.PerModel.TryGetValue(modelEntry.Key, out IModelTokenStats stats))
                    {
                        stats = new ModelTokenStats();
                        aggregate.PerModel[modelEntry.Key] = stats;
                    }

                    stats.RequestCount += modelEntry.Value.RequestCount;
                    stats.OriginalTokens += modelEntry.Value.OriginalTokens;
                    stats.OptimizedTokens += modelEntry.Value.OptimizedTokens;
                }
            }

            return aggregate;
        }

        private static TokenDailyRecord ConvertRowToRecord(TokenDailyRow row)
        {
            Dictionary<string, IModelTokenStats> perModelStats = [];

            foreach (KeyValuePair<string, TokenStatsModel> modelEntry in row.PerModel)
            {
                perModelStats[modelEntry.Key] = new ModelTokenStats
                {
                    RequestCount = modelEntry.Value.RequestCount,
                    OriginalTokens = modelEntry.Value.OriginalTokens,
                    OptimizedTokens = modelEntry.Value.OptimizedTokens
                };
            }

            return new TokenDailyRecord
            {
                Date = row.CreatedDay,
                RequestCount = row.RequestCount,
                OriginalTokens = row.OriginalTokens,
                OptimizedTokens = row.OptimizedTokens,
                PerModel = perModelStats
            };
        }
    }
}

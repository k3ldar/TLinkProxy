using TLinkProxy.Abstractions;
using TLinkProxy.Base.Models;
using TLinkProxy.Db.Tables;
using Shared.Classes;
using SimpleDB;
using static TLinkProxy.Base.Constants;

namespace TLinkProxy.Db.Internal
{
    internal sealed class TokenStatsStore : ITokenStatsStore
    {
        private readonly ISimpleDBOperations<TokenDailyDataRow> _tokenStatsData;

        public TokenStatsStore(ISimpleDBOperations<TokenDailyDataRow> tokenStatsData)
        {
            _tokenStatsData = tokenStatsData ?? throw new ArgumentNullException(nameof(tokenStatsData));
        }

        public async Task RecordAsync(string sessionId, int originalBytes, int optimizedBytes, string modelName)
        {
            DateTime today = DateTime.UtcNow.Date;

            using (TimedLock tl = TimedLock.Lock(_tokenStatsData.TableLock))
            {
                TokenDailyDataRow row = _tokenStatsData.Select(r => r.Created.Date == today).FirstOrDefault();

                if (row == null)
                {
                    row = new TokenDailyDataRow
                    {
                        RequestCount = 0,
                        OriginalTokens = 0,
                        OptimizedTokens = 0
                    };

                    _tokenStatsData.Insert(row);
                }

                long originalTokens = (long)Math.Ceiling((double)originalBytes / CharsPerToken);
                long optimizedTokens = (long)Math.Ceiling((double)optimizedBytes / CharsPerToken);

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
                _tokenStatsData.Update(row);
            }
        }

        public ITokenDailyRecord GetToday()
        {
            DateTime today = DateTime.UtcNow.Date;
            TokenDailyDataRow row = _tokenStatsData.Select(r => r.Created.Date == today).FirstOrDefault();

            using (TimedLock tl = TimedLock.Lock(_tokenStatsData.TableLock))
            {
                if (row == null)
                {
                    row = new TokenDailyDataRow
                    {
                        RequestCount = 0,
                        OriginalTokens = 0,
                        OptimizedTokens = 0
                    };

                    _tokenStatsData.Insert(row);
                }
            }

            return ConvertDataRowToTokenDailyRecord(row);
        }

        public ITokenDailyRecord GetThisMonth()
        {
            DateTime startOfMonth = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            IEnumerable<TokenDailyDataRow> rows = _tokenStatsData.Select(r => r.Created.Date >= startOfMonth && r.Created.Date <= endOfMonth);

            TokenDailyRecord aggregate = new()
            {
                Date = DateTimeOffset.UtcNow.ToString(MonthFormat),
                RequestCount = 0,
                OriginalTokens = 0,
                OptimizedTokens = 0,
                PerModel = []
            };

            foreach (TokenDailyDataRow row in rows)
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

        public ITokenDailyRecord GetLifetime()
        {
            List<TokenDailyDataRow> rows = [.. _tokenStatsData.Select()];

            TokenDailyRecord aggregate = new()
            {
                Date = LifetimeAggregateLabel,
                RequestCount = 0,
                OriginalTokens = 0,
                OptimizedTokens = 0,
                PerModel = []
            };

            foreach (TokenDailyDataRow row in rows)
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

        private static TokenDailyRecord ConvertDataRowToTokenDailyRecord(TokenDailyDataRow row)
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
                Date = row.Created.ToString(TodayFormat),
                RequestCount = row.RequestCount,
                OriginalTokens = row.OriginalTokens,
                OptimizedTokens = row.OptimizedTokens,
                PerModel = perModelStats
            };
        }
    }
}

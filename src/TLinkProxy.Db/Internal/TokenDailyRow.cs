namespace TLinkProxy.Db.Internal
{
    internal sealed class TokenDailyRow
    {
        public long Id { get; set; }

        public int RequestCount { get; set; }

        public long OriginalTokens { get; set; }

        public long OptimizedTokens { get; set; }

        public Dictionary<string, TokenStatsModel> PerModel { get; set; } = [];

        public string CreatedDay { get; set; }
    }
}

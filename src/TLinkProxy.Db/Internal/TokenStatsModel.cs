namespace TLinkProxy.Db.Internal
{
    internal sealed class TokenStatsModel
    {
        public int RequestCount { get; set; }

        public long OriginalTokens { get; set; }

        public long OptimizedTokens { get; set; }
    }
}

namespace TLinkProxy.Abstractions
{
    public interface ITokenDailyRecord
    {
        string Date { get; set; }
        long OptimizedTokens { get; set; }
        long OriginalTokens { get; set; }
        Dictionary<string, IModelTokenStats> PerModel { get; set; }
        int RequestCount { get; set; }
        long TokensSaved { get; }
    }
}

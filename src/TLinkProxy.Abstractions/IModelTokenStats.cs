namespace TLinkProxy.Abstractions
{
    public interface IModelTokenStats
    {
        long OptimizedTokens { get; set; }
        long OriginalTokens { get; set; }
        int RequestCount { get; set; }
        long TokensSaved { get; }
    }
}

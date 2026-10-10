using TLinkProxy.Abstractions;

namespace TLinkProxy.Base.Models
{
    public sealed class ModelTokenStats : IModelTokenStats
    {
        public int RequestCount { get; set; }
        public long OriginalTokens { get; set; }
        public long OptimizedTokens { get; set; }
        public long TokensSaved => OriginalTokens - OptimizedTokens;
    }
}

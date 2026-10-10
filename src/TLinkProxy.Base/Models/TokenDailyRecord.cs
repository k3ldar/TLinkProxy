using TLinkProxy.Abstractions;
using System.Text.Json.Serialization;

namespace TLinkProxy.Base.Models
{
    public sealed class TokenDailyRecord : ITokenDailyRecord
    {
        public string Date { get; set; } = String.Empty;
        public int RequestCount { get; set; }
        public long OriginalTokens { get; set; }
        public long OptimizedTokens { get; set; }
        public long TokensSaved => OriginalTokens - OptimizedTokens;

        [JsonInclude]
        public Dictionary<string, IModelTokenStats> PerModel { get; set; } = [];
    }
}

using System.Text.Json.Serialization;

namespace TLinkProxy.Models
{
    public sealed class TokenPeriodSummaryDto
    {
        [JsonPropertyName("tokensProcessed")]
        public long TokensProcessed { get; set; }

        [JsonPropertyName("bytesProcessed")]
        public long BytesProcessed { get; set; }

        [JsonPropertyName("tokensSaved")]
        public long TokensSaved { get; set; }

        [JsonPropertyName("apiCalls")]
        public int ApiCalls { get; set; }
    }
}

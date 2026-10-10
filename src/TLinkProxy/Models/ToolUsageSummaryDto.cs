using System.Text.Json.Serialization;

namespace TLinkProxy.Models
{
    public sealed class ToolUsageSummaryDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = String.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}

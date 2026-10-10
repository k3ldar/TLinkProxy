using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{
    public class StreamOptions
    {
        [JsonPropertyName("include_usage")]
        public bool IncludeUsage { get; set; }
    }
}

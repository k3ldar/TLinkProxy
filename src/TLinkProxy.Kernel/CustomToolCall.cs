using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{
    public sealed class CustomToolCall
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("input")]
        public string Input { get; set; }
    }
}

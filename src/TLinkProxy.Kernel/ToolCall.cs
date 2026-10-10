using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{
    public class ToolCall
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("function")]
        public FunctionCall Function { get; set; }

        [JsonPropertyName("custom")]
        public CustomToolCall Custom { get; set; }

        [JsonIgnore]
        public string Name => Function?.Name ?? Custom?.Name;

        [JsonIgnore]
        public string Arguments => Function?.Arguments ?? Custom?.Input;
    }
}

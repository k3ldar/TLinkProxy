using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("tool_call_id")]
        public string ToolCallId { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<ToolCall> ToolCalls { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}

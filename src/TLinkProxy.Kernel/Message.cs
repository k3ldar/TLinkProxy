using TLinkProxy.Kernel.Converters;
using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        [JsonConverter(typeof(MessageContentJsonConverter))]
        public string Content { get; set; }

        [JsonPropertyName("tool_call_id")]
        public string ToolCallId { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<ToolCall> ToolCalls { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}

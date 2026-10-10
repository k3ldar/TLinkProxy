
using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{
    public sealed class OpenAIModel
    {
        [JsonPropertyName("messages")]
        public Message[] Messages { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonIgnore]
        public string Name { get; set; }

        [JsonPropertyName("max_completion_tokens")]
        public int MaxCompletionTokens { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("stream_options")]
        public StreamOptions StreamOptions { get; set; }

        [JsonPropertyName("tools")]
        public Tool[] Tools { get; set; }

        [JsonPropertyName("tool_choice")]
        public string ToolChoice { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonIgnore]
        public RoutingEnvelope Routing { get; set; }
    }
}

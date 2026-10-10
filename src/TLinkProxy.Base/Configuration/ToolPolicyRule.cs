using TLinkProxy.Abstractions;
using System.Text.Json.Serialization;

namespace TLinkProxy.Base.Configuration
{
    public sealed class ToolPolicyRule
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("action")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ToolPolicyAction Action { get; set; } = ToolPolicyAction.Allow;

        [JsonPropertyName("sensitiveKeys")]
        public List<string> SensitiveKeys { get; set; } = [];

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}

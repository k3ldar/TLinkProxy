using System.Text.Json.Serialization;

namespace TLinkProxy.Base.Configuration
{
    public sealed class ToolPolicySettings
    {
        [JsonPropertyName("rules")]
        public List<ToolPolicyRule> Rules { get; set; } = [];
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{
    public class RoutingEnvelope
    {
        [JsonPropertyName("requestType")]
        public string RequestType { get; set; }

        [JsonPropertyName("requiredCapabilities")]
        public string[] RequiredCapabilities { get; set; }

        [JsonPropertyName("timeoutMs")]
        public int? TimeoutMs { get; set; }

        [JsonPropertyName("preferredModel")]
        public string PreferredModel { get; set; }

        [JsonPropertyName("payload")]
        public JsonElement? Payload { get; set; }
    }
}

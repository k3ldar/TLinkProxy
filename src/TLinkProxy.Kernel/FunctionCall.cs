using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{
    public class FunctionCall
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arguments")]
        public string Arguments { get; set; }
    }
}

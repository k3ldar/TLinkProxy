using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{

    public class Tool
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("function")]
        public Function Function { get; set; }
    }
}

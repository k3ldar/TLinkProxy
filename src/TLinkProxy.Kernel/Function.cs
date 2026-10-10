using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{
    public class Function
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("parameters")]
        public Parameters Parameters { get; set; }
    }
}

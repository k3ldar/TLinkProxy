using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel
{

    public class Parameters
    {
        public Parameters()
        {
            Type = "object";
            AdditionalProperties = false;
            Required = [];
            Properties = new();
        }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("required")]
        public string[] Required { get; set; }

        [JsonPropertyName("properties")]
        public Properties Properties { get; set; }

        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; }
    }
}

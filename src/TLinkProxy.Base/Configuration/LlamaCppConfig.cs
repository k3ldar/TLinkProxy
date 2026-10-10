using TLinkProxy.Abstractions;
using System.Text.Json.Serialization;

namespace TLinkProxy.Base.Configuration
{
    public class LlamaCppConfig : ILlamaCppConfig
    {
        [JsonPropertyName("host")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Host { get; set; } = Constants.DefaultLlamaHost;

        [JsonPropertyName("port")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int Port { get; set; } = Constants.DefaultLlamaPort;

        [JsonPropertyName("gpuLayers")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int GpuLayers { get; set; }

        [JsonPropertyName("threads")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int Threads { get; set; }

        [JsonPropertyName("batchSize")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int BatchSize { get; set; }

        [JsonPropertyName("contextSize")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int ContextSize { get; set; } = Constants.DefaultLlamaContextSize;

        [JsonPropertyName("autoRestart")]
        [JsonConverter(typeof(StrictBoolJsonConverter))]
        public bool AutoRestart { get; set; }

        [JsonPropertyName("healthCheckSeconds")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int HealthCheckSeconds { get; set; } = Constants.DefaultLlamaHealthCheckSeconds;

        [JsonPropertyName("timeoutSeconds")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int TimeoutSeconds { get; set; }
    }
}

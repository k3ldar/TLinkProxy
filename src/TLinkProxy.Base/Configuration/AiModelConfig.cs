using TLinkProxy.Abstractions;
using System.Text.Json.Serialization;

namespace TLinkProxy.Base.Configuration
{
    public class AiModelConfig : IAiModelConfig
    {
        [JsonPropertyName("alias")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Alias { get; set; }

        [JsonPropertyName("kind")]
        [JsonConverter(typeof(LowercaseEnumJsonConverter))]
        public ModelKind Kind { get; set; }

        [JsonPropertyName("endpoint")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Endpoint { get; set; }

        [JsonPropertyName("model")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Model { get; set; }

        [JsonPropertyName("tags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[] Tags { get; set; }

        [JsonPropertyName("compute")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Compute { get; set; }

        [JsonPropertyName("latencyProfile")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LatencyProfile { get; set; }

        [JsonPropertyName("costPer1kInput")]
        [JsonConverter(typeof(StrictDoubleJsonConverter))]
        public double CostPer1mInput { get; set; }

        [JsonPropertyName("costPer1kOutput")]
        [JsonConverter(typeof(StrictDoubleJsonConverter))]
        public double CostPer1mOutput { get; set; }

        [JsonPropertyName("maxContext")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int MaxContext { get; set; }

        [JsonPropertyName("concurrencyLimit")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int ConcurrencyLimit { get; set; } = Constants.DefaultConcurrencyLimit;

        [JsonPropertyName("weight")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int Weight { get; set; } = Constants.DefaultWeight;

        [JsonPropertyName("apiKeyEnvVar")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ApiKeyEnvVar { get; set; }

        /// <summary>Kind-specific settings. Populated for <c>llamacpp</c> backends that the proxy should launch and supervise.</summary>
        [JsonPropertyName("details")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ILlamaCppConfig Details { get; set; }

        /// <summary>Whether requests may wait in a per-backend FIFO queue when the backend is saturated (default true).</summary>
        [JsonPropertyName("allowQueue")]
        [JsonConverter(typeof(StrictBoolJsonConverter))]
        public bool AllowQueue { get; set; } = true;

        /// <summary>Maximum number of requests waiting in this backend's queue.</summary>
        [JsonPropertyName("maxQueueDepth")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int MaxQueueDepth { get; set; } = Constants.DefaultQueueDepth;

        /// <summary>
        /// Indicates whether the model is enabled for use or not. When omitted, the backend is enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        [JsonConverter(typeof(StrictBoolJsonConverter))]
        public bool Enabled { get; set; } = true;

        [JsonIgnore]
        public bool IsConfigured { get; set; }

        /// <summary>
        /// Indicates the number of seconds to wait for a health probe response before considering the backend unhealthy. When omitted, the default is 5 seconds.
        /// </summary>
        [JsonPropertyName("healthProbeSeconds")]
        [JsonConverter(typeof(StrictIntJsonConverter))]
        public int HealthProbeSeconds { get; set; } = Constants.DefaultHealthProbeSeconds;

        /// <summary>
        /// Whether this backend should be started lazily on its first request and shut down once idle:
        /// a llama.cpp backend with a positive timeout. On-demand backends remain routable even while stopped.
        /// </summary>
        [JsonIgnore]
        public bool IsOnDemand =>
            Kind == ModelKind.LlamaCpp && Details != null && Details.TimeoutSeconds > 0;
    }
}

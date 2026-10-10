using TLinkProxy.Abstractions;
using System.Text.Json.Serialization;

namespace TLinkProxy.Models
{
    public sealed class ModelStatusDto
    {
        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("kind")]
        public ModelKind Kind { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("healthy")]
        public bool Healthy { get; set; }

        [JsonPropertyName("isOnDemand")]
        public bool IsOnDemand { get; set; }

        [JsonPropertyName("isRunning")]
        public bool IsRunning { get; set; }

        [JsonPropertyName("concurrencyLimit")]
        public int ConcurrencyLimit { get; set; }

        [JsonPropertyName("inFlight")]
        public int InFlight { get; set; }

        [JsonPropertyName("queueDepth")]
        public int QueueDepth { get; set; }

        [JsonPropertyName("maxQueueDepth")]
        public int MaxQueueDepth { get; set; }

        [JsonPropertyName("idleTimeoutSeconds")]
        public int IdleTimeoutSeconds { get; set; }

        [JsonPropertyName("inactiveAtUtc")]
        public DateTime? InactiveAtUtc { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace TLinkProxy.Models
{
    public sealed class DashboardMemoryHistoryDto
    {
        [JsonPropertyName("intervalSeconds")]
        public int IntervalSeconds { get; set; }

        [JsonPropertyName("samples")]
        public List<DashboardMemorySampleDto> Samples { get; set; } = [];
    }

    public sealed class DashboardMemorySampleDto
    {
        [JsonPropertyName("sampledAtUtc")]
        public DateTime SampledAtUtc { get; set; }

        [JsonPropertyName("managedHeapBytes")]
        public long ManagedHeapBytes { get; set; }

        [JsonPropertyName("gcCommittedBytes")]
        public long GcCommittedBytes { get; set; }

        [JsonPropertyName("workingSetBytes")]
        public long WorkingSetBytes { get; set; }

        [JsonPropertyName("privateMemoryBytes")]
        public long PrivateMemoryBytes { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace TLinkProxy.Models
{
    public sealed class DashboardDiagnosticsDto
    {
        [JsonPropertyName("sampledAtUtc")]
        public DateTime SampledAtUtc { get; set; }

        [JsonPropertyName("intervalSeconds")]
        public int IntervalSeconds { get; set; }

        [JsonPropertyName("managedHeapBytes")]
        public long ManagedHeapBytes { get; set; }

        [JsonPropertyName("gcCommittedBytes")]
        public long GcCommittedBytes { get; set; }

        [JsonPropertyName("totalAllocatedBytes")]
        public long TotalAllocatedBytes { get; set; }

        [JsonPropertyName("workingSetBytes")]
        public long WorkingSetBytes { get; set; }

        [JsonPropertyName("privateMemoryBytes")]
        public long PrivateMemoryBytes { get; set; }

        [JsonPropertyName("threadPoolThreads")]
        public int ThreadPoolThreads { get; set; }

        [JsonPropertyName("pendingWorkItems")]
        public long PendingWorkItems { get; set; }

        [JsonPropertyName("completedWorkItems")]
        public long CompletedWorkItems { get; set; }

        [JsonPropertyName("processes")]
        public List<DashboardProcessMemoryDto> Processes { get; set; } = [];
    }

    public sealed class DashboardProcessMemoryDto
    {
        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonPropertyName("processId")]
        public int ProcessId { get; set; }

        [JsonPropertyName("workingSetBytes")]
        public long WorkingSetBytes { get; set; }

        [JsonPropertyName("privateMemoryBytes")]
        public long PrivateMemoryBytes { get; set; }
    }
}

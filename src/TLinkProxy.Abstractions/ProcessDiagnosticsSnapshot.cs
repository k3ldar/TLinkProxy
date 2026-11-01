namespace TLinkProxy.Abstractions
{
    /// <summary>
    /// Point-in-time sample of host memory and thread-pool usage collected off the request path.
    /// </summary>
    public sealed class ProcessDiagnosticsSnapshot
    {
        public DateTime SampledAtUtc { get; set; }

        /// <summary>Managed heap size reported by the garbage collector (bytes).</summary>
        public long ManagedHeapBytes { get; set; }

        /// <summary>Total committed memory reported by the garbage collector (bytes).</summary>
        public long GcCommittedBytes { get; set; }

        /// <summary>Total managed bytes allocated since process start (bytes).</summary>
        public long TotalAllocatedBytes { get; set; }

        /// <summary>Operating-system working set for the host process (bytes).</summary>
        public long WorkingSetBytes { get; set; }

        /// <summary>Private memory for the host process (bytes).</summary>
        public long PrivateMemoryBytes { get; set; }

        /// <summary>Current number of threads in the thread pool.</summary>
        public int ThreadPoolThreads { get; set; }

        /// <summary>Number of work items queued to the thread pool and not yet processed.</summary>
        public long ThreadPoolPendingWorkItems { get; set; }

        /// <summary>Number of work items processed by the thread pool since process start.</summary>
        public long ThreadPoolCompletedItems { get; set; }

        /// <summary>Memory usage for each managed child process (e.g. llama.cpp).</summary>
        public List<ProcessMemorySample> Processes { get; set; } = [];
    }

    /// <summary>
    /// Memory usage for a single managed child process.
    /// </summary>
    public sealed class ProcessMemorySample
    {
        public string Alias { get; set; }

        public int ProcessId { get; set; }

        public long WorkingSetBytes { get; set; }

        public long PrivateMemoryBytes { get; set; }
    }
}

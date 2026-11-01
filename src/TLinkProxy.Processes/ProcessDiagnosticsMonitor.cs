using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using System.Diagnostics;

namespace TLinkProxy.Processes
{
    /// <summary>
    /// Periodically samples host memory and thread-pool statistics off the request path and
    /// exposes the most recent snapshot for the dashboard. The sampling interval is read from
    /// the settings store on every pass so it can be changed at runtime without a restart.
    /// </summary>
    internal sealed class ProcessDiagnosticsMonitor : IProcessDiagnostics
    {
        private readonly ISettingsService _settingsService;
        private readonly ILlamaCppProcessManager _processManager;
        private readonly Lock _gate = new();
        private ProcessDiagnosticsSnapshot _snapshot;
        private TimeSpan _interval;
        private CancellationTokenSource _cts;
        private Task _loopTask;

        public ProcessDiagnosticsMonitor(ISettingsService settingsService, ILlamaCppProcessManager processManager)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));

            _snapshot = new ProcessDiagnosticsSnapshot { SampledAtUtc = DateTime.UtcNow };
            _interval = TimeSpan.FromSeconds(Constants.DefaultDiagnosticsIntervalSeconds);
        }

        public ProcessDiagnosticsSnapshot Snapshot
        {
            get
            {
                lock (_gate)
                {
                    return _snapshot;
                }
            }
        }

        public TimeSpan Interval
        {
            get
            {
                lock (_gate)
                {
                    return _interval;
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loopTask = Task.Run(() => LoopAsync(_cts.Token), cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cts?.Cancel();

                if (_loopTask != null)
                {
                    await _loopTask.ConfigureAwait(false);
                }
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            try
            {
                _cts?.Dispose();
            }
            catch
            {
            }
        }

        private async Task LoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                TimeSpan interval = ResolveInterval();

                try
                {
                    IReadOnlyList<ProcessMemorySample> processes = _processManager.GetProcessMemory();
                    ProcessDiagnosticsSnapshot snapshot = CollectSnapshot(processes);

                    lock (_gate)
                    {
                        _snapshot = snapshot;
                        _interval = interval;
                    }
                }
                catch
                {
                    // Diagnostics sampling must never take the application down.
                }

                try
                {
                    await Task.Delay(interval, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private TimeSpan ResolveInterval()
        {
            try
            {
                string raw = _settingsService.GetSetting(Constants.DiagnosticsIntervalSecondsKey);

                if (Int32.TryParse(raw, out int seconds) && seconds > 0)
                {
                    return TimeSpan.FromSeconds(seconds);
                }
            }
            catch
            {
                // Fall back to the default when settings storage is unavailable.
            }

            return TimeSpan.FromSeconds(Constants.DefaultDiagnosticsIntervalSeconds);
        }

        private static ProcessDiagnosticsSnapshot CollectSnapshot(IReadOnlyList<ProcessMemorySample> processes)
        {
            GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();

            ProcessDiagnosticsSnapshot snapshot = new()
            {
                SampledAtUtc = DateTime.UtcNow,
                ManagedHeapBytes = gcInfo.HeapSizeBytes,
                GcCommittedBytes = gcInfo.TotalCommittedBytes,
                TotalAllocatedBytes = GC.GetTotalAllocatedBytes(precise: true),
                ThreadPoolThreads = ThreadPool.ThreadCount,
                ThreadPoolPendingWorkItems = ThreadPool.PendingWorkItemCount,
                ThreadPoolCompletedItems = ThreadPool.CompletedWorkItemCount,
                Processes = processes == null ? [] : [.. processes]
            };

            try
            {
                using Process current = Process.GetCurrentProcess();
                snapshot.WorkingSetBytes = current.WorkingSet64;
                snapshot.PrivateMemoryBytes = current.PrivateMemorySize64;
            }
            catch
            {
                // Process counters can be unavailable on some hosts; leave the values at zero.
            }

            return snapshot;
        }
    }
}

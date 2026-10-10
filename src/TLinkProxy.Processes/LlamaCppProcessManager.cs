using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Base.Configuration;
using TLinkProxy.Base.Events;
using TLinkProxy.Base.Models;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using static TLinkProxy.Base.Constants;

namespace TLinkProxy.Processes
{
    internal sealed class LlamaCppProcessManager : ILlamaCppProcessManager, IEventHandler<ModelsReloadedEvent>
    {
        private readonly IAiBackendRegistry _registry;
        private readonly IProxySessionLogger _logger;
        private readonly Lock _gate = new();
        private readonly Dictionary<string, Process> _processes = [];
        private readonly Dictionary<string, CancellationTokenSource> _cts = [];
        private readonly Dictionary<string, int> _restartAttempts = [];
        private readonly Dictionary<string, DateTime> _lastActivityUtc = [];
        private readonly Dictionary<string, SemaphoreSlim> _demandSignals = [];
        private readonly Dictionary<string, int> _startCounts = [];
        private readonly Dictionary<string, LlamaCppConfig> _managedDetails = [];
        private readonly string _modelsDirectory;
        private readonly string _llamaCppExecutable;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAiModelRuntimeStateStore _stateStore;
        private readonly IEventBus _eventBus;
        private readonly IDisposable _subscription;

        public LlamaCppProcessManager(IAiBackendRegistry registry, IProxySessionLogger logger, IHttpClientFactory httpClientFactory)
            : this(registry, logger, httpClientFactory, new AiModelRuntimeStateStore(), null)
        {
        }

        public LlamaCppProcessManager(IAiBackendRegistry registry, IProxySessionLogger logger, IHttpClientFactory httpClientFactory, IAiModelRuntimeStateStore stateStore, IEventBus eventBus)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            _eventBus = eventBus;
            _subscription = eventBus?.Subscribe(this);
            IAiSettings config = _registry.Current ?? throw new InvalidOperationException(ConfigFileMissingInvalid);
            string modelsDir = config.ModelPath;
            _modelsDirectory = modelsDir ?? throw new InvalidOperationException(ModelDirectoryInvalid);

            if (!Directory.Exists(_modelsDirectory))
            {
                Directory.CreateDirectory(_modelsDirectory);
            }

            _modelsDirectory = modelsDir;

            string llamaCppExecutable = config.Llamacpp ?? throw new InvalidOperationException(LlamaCppExecutableNotFound);

            if (!File.Exists(llamaCppExecutable))
            {
                throw new InvalidOperationException($"llama.cpp executable not found at: {llamaCppExecutable}");
            }

            _llamaCppExecutable = llamaCppExecutable;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<IAiModelConfig> modelConfigs = _registry.GetAllBackends();

            if (modelConfigs == null)
            {
                return Task.CompletedTask;
            }

            foreach (IAiModelConfig modelConfig in modelConfigs)
            {
                if (modelConfig.Kind != ModelKind.LlamaCpp ||
                    modelConfig?.Details == null)
                {
                    continue;
                }

                if (!modelConfig.Enabled)
                {
                    _registry.SetHealth(modelConfig.Alias, false);
                    continue;
                }

                if (modelConfig.IsOnDemand)
                {
                    continue;
                }

                StartInstance(modelConfig);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Ensures the on-demand llama.cpp backend for <paramref name="alias"/> is running.
        /// Always-on backends (zero timeout) are already supervised and need no action here.
        /// </summary>
        public async Task EnsureStartedAsync(IAiModelConfig modelConfig, CancellationToken ct)
        {
            if (modelConfig == null || !modelConfig.Enabled || !modelConfig.IsOnDemand)
            {
                return;
            }

            RecordActivity(modelConfig.Alias);

            int priorStartCount = GetStartCount(modelConfig.Alias);

            if (!IsManaged(modelConfig.Alias))
            {
                StartInstance(modelConfig);
            }

            if (!HasLiveProcess(modelConfig.Alias))
            {
                SignalDemand(modelConfig.Alias);
            }

            await WaitUntilRunningAsync(modelConfig.Alias, priorStartCount, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the last recorded request activity time for the backend, or <c>null</c>
        /// when it is not currently managed/running (e.g. an on-demand instance that is stopped).
        /// </summary>
        public DateTime? GetLastActivityUtc(string alias)
        {
            if (String.IsNullOrWhiteSpace(alias))
            {
                return null;
            }

            lock (_gate)
            {
                return _lastActivityUtc.TryGetValue(alias, out DateTime value) ? value : null;
            }
        }

        public void Handle(ModelsReloadedEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);

            if (@event.Models == null)
            {
                return;
            }

            foreach (IAiModelConfig modelConfig in @event.Models)
            {
                if (modelConfig == null || modelConfig.Kind != ModelKind.LlamaCpp || modelConfig.Details == null)
                {
                    continue;
                }

                if (!modelConfig.Enabled)
                {
                    StopManaged(modelConfig.Alias);
                    _registry.SetHealth(modelConfig.Alias, false);
                    _stateStore.SetRunning(modelConfig.Alias, false);
                    continue;
                }

                if (modelConfig.IsOnDemand)
                {
                    continue;
                }

                ReconcileAlwaysOn(modelConfig);
            }
        }

        private void StartInstance(IAiModelConfig modelConfig)
        {
            if (modelConfig == null || String.IsNullOrWhiteSpace(modelConfig.Alias) || modelConfig.Details == null)
            {
                return;
            }

            string alias = modelConfig.Alias;
            LlamaCppConfig details = ResolveDetails(_registry.Current, modelConfig);
            CancellationTokenSource cts = new();

            lock (_gate)
            {
                if (_cts.TryGetValue(alias, out CancellationTokenSource existing) && !existing.IsCancellationRequested)
                {
                    // Already managed.
                    cts.Dispose();
                    return;
                }

                _cts[alias] = cts;
                _restartAttempts[alias] = 0;
                _managedDetails[alias] = details;
            }

            _ = Task.Run(() => ManageInstanceAsync(modelConfig, cts, cts.Token));
        }

        private void ReconcileAlwaysOn(IAiModelConfig modelConfig)
        {
            string alias = modelConfig.Alias;

            if (IsManaged(alias) && HasLiveProcess(alias) && DetailsUnchanged(alias, modelConfig.Details))
            {
                _stateStore.SetRunning(alias, true);
                return;
            }

            StopManaged(alias);
            StartInstance(modelConfig);
        }

        private bool DetailsUnchanged(string alias, ILlamaCppConfig newDetails)
        {
            lock (_gate)
            {
                if (!_managedDetails.TryGetValue(alias, out LlamaCppConfig oldDetails))
                {
                    return false;
                }

                return DetailsEqual(oldDetails, newDetails);
            }
        }

        private static bool DetailsEqual(ILlamaCppConfig left, ILlamaCppConfig right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.GpuLayers == right.GpuLayers
                && left.Threads == right.Threads
                && left.BatchSize == right.BatchSize
                && left.AutoRestart == right.AutoRestart
                && left.TimeoutSeconds == right.TimeoutSeconds;
        }

        private void StopManaged(string alias)
        {
            CancellationTokenSource cts = null;
            Process proc = null;

            lock (_gate)
            {
                _cts.TryGetValue(alias, out cts);
                _processes.TryGetValue(alias, out proc);
            }

            cts?.Cancel();

            if (proc != null && !proc.HasExited)
            {
                KillProcess(proc);
            }
        }

        private async Task ManageInstanceAsync(IAiModelConfig modelConfig, CancellationTokenSource cts, CancellationToken ct)
        {
            try
            {
                await ManageInstanceCoreAsync(modelConfig, ct).ConfigureAwait(false);
            }
            finally
            {
                lock (_gate)
                {
                    if (_cts.TryGetValue(modelConfig.Alias, out CancellationTokenSource current) && ReferenceEquals(current, cts))
                    {
                        _cts.Remove(modelConfig.Alias);
                    }

                    _managedDetails.Remove(modelConfig.Alias);
                    _lastActivityUtc.Remove(modelConfig.Alias);

                    if (_demandSignals.Remove(modelConfig.Alias, out SemaphoreSlim signal))
                    {
                        signal.Dispose();
                    }
                }
            }
        }

        private async Task ManageInstanceCoreAsync(IAiModelConfig modelConfig, CancellationToken ct)
        {
            string alias = modelConfig.Alias;
            bool onDemand = modelConfig.Details.TimeoutSeconds > 0;
            TimeSpan idleTimeout = TimeSpan.FromSeconds(modelConfig.Details.TimeoutSeconds);
            TimeSpan backoff = TimeSpan.FromSeconds(1);

            if (onDemand)
            {
                await _logger.LogAsync(alias, $"[LlamaMgr] {alias} is on-demand; waiting for first request").ConfigureAwait(false);
                await WaitForDemandAsync(alias, ct).ConfigureAwait(false);

                if (ct.IsCancellationRequested)
                {
                    return;
                }
            }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!onDemand && await IsInstanceResponsiveAsync(modelConfig, ct).ConfigureAwait(false))
                    {
                        await _logger.LogAsync(alias, $"[LlamaMgr] {alias} is already running; adopting existing instance").ConfigureAwait(false);
                        _registry.SetHealth(alias, true);
                        SetRunningState(alias, true);
                        RecordActivity(alias);
                        return;
                    }

                    Uri hostUri = new(modelConfig.Endpoint, UriKind.Absolute);

                    if (onDemand && IsPortInUse(hostUri.Port))
                    {
                        await _logger.LogAsync(alias, $"[LlamaMgr] Port {hostUri.Port} is already in use; not starting {alias}").ConfigureAwait(false);
                        _registry.SetHealth(alias, false);
                        SetRunningState(alias, false);
                        return;
                    }

                    ProcessStartInfo psi = null;

                    try
                    {
                        psi = BuildStartInfo(modelConfig);
                    }
                    catch (InvalidOperationException)
                    {
                        // invalid configuration
                    }

                    using Process proc = new() { StartInfo = psi, EnableRaisingEvents = true };

                    if (!proc.Start())
                    {
                        await _logger.LogAsync(alias, $"[LlamaMgr] Failed to start process for {alias}").ConfigureAwait(false);
                        _registry.SetHealth(alias, false);
                        SetRunningState(alias, false);
                        return;
                    }

                    lock (_gate)
                    {
                        _processes[alias] = proc;
                        _restartAttempts[alias] = _restartAttempts.GetValueOrDefault(alias) + 1;
                        _startCounts[alias] = _startCounts.GetValueOrDefault(alias) + 1;
                    }

                    RecordActivity(alias);
                    _ = PipeOutputAsync(proc, alias, ct);

                    _registry.SetHealth(alias, true);
                    SetRunningState(alias, true);
                    await _logger.LogAsync(alias, $"[LlamaMgr] Started process {proc.Id} for {alias}").ConfigureAwait(false);

                    bool stoppedForIdle = false;

                    try
                    {
                        if (onDemand)
                        {
                            bool idle = await WaitForIdleOrExitAsync(proc, alias, idleTimeout, ct).ConfigureAwait(false);

                            if (ct.IsCancellationRequested)
                            {
                                return;
                            }

                            if (idle)
                            {
                                stoppedForIdle = true;
                                await _logger.LogAsync(alias, $"[LlamaMgr] {alias} idle for {idleTimeout.TotalSeconds:0}s; shutting down").ConfigureAwait(false);
                                KillProcess(proc);
                            }

                            await proc.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                        }
                        else
                        {
                            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Graceful shutdown initiated.
                        return;
                    }

                    lock (_gate)
                    {
                        _processes.Remove(alias);
                    }

                    _registry.SetHealth(alias, false);
                    SetRunningState(alias, false);

                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    if (onDemand)
                    {
                        if (!stoppedForIdle)
                        {
                            await _logger.LogAsync(alias, $"[LlamaMgr] Process {alias} exited (code {(proc.HasExited ? proc.ExitCode : -1)})").ConfigureAwait(false);
                        }

                        await WaitForDemandAsync(alias, ct).ConfigureAwait(false);

                        if (ct.IsCancellationRequested)
                        {
                            return;
                        }

                        continue;
                    }

                    await _logger.LogAsync(alias, $"[LlamaMgr] Process {alias} exited (code {(proc.HasExited ? proc.ExitCode : -1)})").ConfigureAwait(false);

                    if (!modelConfig.Details.AutoRestart)
                    {
                        return;
                    }

                    await _logger.LogAsync(alias, $"[LlamaMgr] Restarting {alias} in {backoff.TotalSeconds:0}s").ConfigureAwait(false);
                    await Task.Delay(backoff, ct).ConfigureAwait(false);
                    const double restartMultiplier = 2;
                    const double maxRestartDelaySeconds = 60;
                    backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * restartMultiplier, maxRestartDelaySeconds));
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(alias, $"[LlamaMgr] Exception while managing {alias}: {ex.Message}").ConfigureAwait(false);
                    _registry.SetHealth(alias, false);
                    SetRunningState(alias, false);
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Constants.DefaultRetryBackoffSeconds), ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        private async Task WaitForDemandAsync(string alias, CancellationToken ct)
        {
            SemaphoreSlim signal = GetOrCreateDemandSignal(alias);

            try
            {
                await signal.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested.
            }
        }

        private async Task WaitForIdleAsync(string alias, TimeSpan idleTimeout, CancellationToken ct)
        {
            TimeSpan pollInterval = TimeSpan.FromSeconds(Constants.OnDemandIdlePollSeconds);

            while (!ct.IsCancellationRequested)
            {
                TimeSpan elapsed = DateTime.UtcNow - GetLastActivity(alias);

                if (elapsed >= idleTimeout)
                {
                    return;
                }

                TimeSpan remaining = idleTimeout - elapsed;
                TimeSpan delay = remaining < pollInterval ? remaining : pollInterval;

                try
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task<bool> WaitForIdleOrExitAsync(Process proc, string alias, TimeSpan idleTimeout, CancellationToken ct)
        {
            Task exitTask = proc.WaitForExitAsync(CancellationToken.None);
            Task idleTask = WaitForIdleAsync(alias, idleTimeout, ct);
            Task winner = await Task.WhenAny(exitTask, idleTask).ConfigureAwait(false);
            return winner == idleTask;
        }

        private async Task WaitUntilRunningAsync(string alias, int priorStartCount, CancellationToken ct)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan timeout = TimeSpan.FromSeconds(Constants.OnDemandStartWaitSeconds);

            while (!ct.IsCancellationRequested && stopwatch.Elapsed < timeout)
            {
                if (HasLiveProcess(alias) || GetStartCount(alias) > priorStartCount || !IsManaged(alias))
                {
                    return;
                }

                try
                {
                    await Task.Delay(Constants.OnDemandStartPollMilliseconds, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private SemaphoreSlim GetOrCreateDemandSignal(string alias)
        {
            lock (_gate)
            {
                if (!_demandSignals.TryGetValue(alias, out SemaphoreSlim signal))
                {
                    signal = new SemaphoreSlim(0, 1);
                    _demandSignals[alias] = signal;
                }

                return signal;
            }
        }

        private void SignalDemand(string alias)
        {
            lock (_gate)
            {
                if (!_demandSignals.TryGetValue(alias, out SemaphoreSlim signal))
                {
                    signal = new SemaphoreSlim(0, 1);
                    _demandSignals[alias] = signal;
                }

                if (signal.CurrentCount == 0)
                {
                    signal.Release();
                }
            }
        }

        private void SetRunningState(string alias, bool isRunning)
        {
            _stateStore.SetRunning(alias, isRunning);
        }

        private void RecordActivity(string alias)
        {
            lock (_gate)
            {
                _lastActivityUtc[alias] = DateTime.UtcNow;
            }
        }

        private DateTime GetLastActivity(string alias)
        {
            lock (_gate)
            {
                return _lastActivityUtc.TryGetValue(alias, out DateTime value) ? value : DateTime.UtcNow;
            }
        }

        private int GetStartCount(string alias)
        {
            lock (_gate)
            {
                return _startCounts.GetValueOrDefault(alias);
            }
        }

        private bool HasLiveProcess(string alias)
        {
            lock (_gate)
            {
                return _processes.TryGetValue(alias, out Process proc) && proc != null && !proc.HasExited;
            }
        }

        private bool IsManaged(string alias)
        {
            lock (_gate)
            {
                return _cts.TryGetValue(alias, out CancellationTokenSource cts) && !cts.IsCancellationRequested;
            }
        }

        private static void KillProcess(Process proc)
        {
            if (proc == null || proc.HasExited)
            {
                return;
            }

            try
            {
                proc.Kill(entireProcessTree: true);
            }
            catch
            {
                // Process may have exited concurrently; ignore.
            }
        }

        private static LlamaCppConfig ResolveDetails(IAiSettings root, IAiModelConfig modelConfig)
        {
            ILlamaCppConfig details = modelConfig.Details;

            return new LlamaCppConfig
            {
                GpuLayers = details.GpuLayers,
                Threads = details.Threads,
                BatchSize = details.BatchSize,
                AutoRestart = details.AutoRestart,
                TimeoutSeconds = details.TimeoutSeconds
            };
        }

        private ProcessStartInfo BuildStartInfo(IAiModelConfig modelConfig)
        {
            ILlamaCppConfig instance = modelConfig.Details;

            ProcessStartInfo psi = new()
            {
                FileName = _llamaCppExecutable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            List<string> args = [];

            if (!String.IsNullOrWhiteSpace(modelConfig.Model))
            {
                string model = modelConfig.Model;

                if (!model.EndsWith(GgufFileExtension))
                    model = $"{model}.{GgufFileExtension}";

                model = Path.Combine(_modelsDirectory, model);

                if (!File.Exists(model))
                    throw new InvalidOperationException();

                args.Add($"--model \"{Path.Combine(_modelsDirectory, model)}\"");
            }

            Uri hostUri = new(modelConfig.Endpoint, UriKind.Absolute);

            if (!String.IsNullOrWhiteSpace(hostUri.Host))
            {
                args.Add($"--host {hostUri.Host}");
            }

            if (hostUri.Port > 0)
            {
                args.Add($"--port {hostUri.Port}");
            }

            if (instance.GpuLayers > 0)
            {
                args.Add($"--n-gpu-layers {instance.GpuLayers}");
            }

            if (instance.Threads > 0)
            {
                args.Add($"--threads {instance.Threads}");
            }

            if (instance.BatchSize > 0)
            {
                args.Add($"--batch-size {instance.BatchSize}");
            }

            if (modelConfig.MaxContext > 0)
            {
                args.Add($"--ctx-size {modelConfig.MaxContext}");
            }

            psi.Arguments = String.Join(Constants.Space, args);
            return psi;
        }

        private async Task PipeOutputAsync(Process proc, string alias, CancellationToken ct)
        {
            string logFile = GetLogFile(alias);
            await Task.WhenAll(
                PipeAsync(proc.StandardOutput, alias, logFile, isError: false, ct),
                PipeAsync(proc.StandardError, alias, logFile, isError: true, ct)).ConfigureAwait(false);
        }

        private async Task PipeAsync(StreamReader reader, string alias, string logFile, bool isError, CancellationToken ct)
        {
            try
            {
                string line;
                while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
                {
                    string prefix = isError ? $"[llama:{alias}:err]" : $"[llama:{alias}]";
                    await _logger.LogAsync(alias, $"{prefix} {line}").ConfigureAwait(false);
                    await File.AppendAllTextAsync(logFile, line + Environment.NewLine, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // Process may have been killed mid-read; ignore.
            }
        }

        private static string GetLogFile(string alias)
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) ?? Path.GetTempPath();
            string logDir = Path.Combine(root, Constants.DefaultAppDataDirectoryName, Constants.LogsDirectoryName);
            Directory.CreateDirectory(logDir);
            return Path.Combine(logDir, $"{alias}{Constants.SessionLogFileExtension}");
        }

        private static bool IsPortInUse(int port)
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] listeners = properties.GetActiveTcpListeners();
            return listeners.Any(endpoint => endpoint.Port == port);
        }

        private async Task<bool> IsInstanceResponsiveAsync(IAiModelConfig modelConfig, CancellationToken ct)
        {
            if (modelConfig == null || String.IsNullOrWhiteSpace(modelConfig.Endpoint) || modelConfig.Details == null)
            {
                return false;
            }

            try
            {
                HttpClient client = _httpClientFactory.CreateClient(BackendClientName);
                client.Timeout = TimeSpan.FromSeconds(DefaultHealthProbeSecondsTimeout);
                string url = modelConfig.Endpoint.TrimEnd(CharForwardSlash) + HealthPathLlamaCpp;
                using HttpRequestMessage request = new(HttpMethod.Get, url);
                using HttpResponseMessage response = await client.SendAsync(request, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            CancellationTokenSource[] ctss;
            Process[] procs;
            lock (_gate)
            {
                ctss = [.. _cts.Values];
                procs = [.. _processes.Values.Where(p => p != null && !p.HasExited)];
            }

            foreach (CancellationTokenSource cts in ctss)
            {
                cts.Cancel();
            }

            foreach (Process proc in procs)
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                    }

                    await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _subscription?.Dispose();
            }
            catch
            {
            }

            lock (_gate)
            {
                foreach (CancellationTokenSource cts in _cts.Values)
                {
                    cts.Cancel();
                    cts.Dispose();
                }

                foreach (Process proc in _processes.Values)
                {
                    try
                    {
                        proc?.Dispose();
                    }
                    catch
                    {
                    }
                }

                foreach (SemaphoreSlim signal in _demandSignals.Values)
                {
                    signal.Dispose();
                }

                _cts.Clear();
                _processes.Clear();
                _demandSignals.Clear();
                _lastActivityUtc.Clear();
                _managedDetails.Clear();
            }
        }
    }
}

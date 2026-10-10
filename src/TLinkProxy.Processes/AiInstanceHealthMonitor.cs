using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Base.Events;
using System.Net.Http.Headers;

namespace TLinkProxy.Processes
{
    internal sealed class AiInstanceHealthMonitor : IAiInstanceHealthMonitor, IEventHandler<ModelsReloadedEvent>
    {
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(Constants.DefaultBackendHealthCheckSeconds);

        private readonly IAiBackendRegistry _registry;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IEventBus _eventBus;
        private readonly IDisposable _subscription;
        private readonly Dictionary<string, DateTime> _nextCheck = [];
        private CancellationTokenSource _cts;
        private Task _loopTask;

        public AiInstanceHealthMonitor(IAiBackendRegistry registry, IHttpClientFactory httpFactory)
            : this(registry, httpFactory, null)
        {
        }

        public AiInstanceHealthMonitor(IAiBackendRegistry registry, IHttpClientFactory httpFactory, IEventBus eventBus)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _eventBus = eventBus;
            _subscription = eventBus?.Subscribe(this);
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

        public void Handle(ModelsReloadedEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);

            lock (_nextCheck)
            {
                _nextCheck.Clear();
            }
        }

        private async Task LoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    IReadOnlyList<IAiModelConfig> modelConfigs = _registry.GetAllBackends();

                    foreach (IAiModelConfig b in modelConfigs)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            break;
                        }

                        if (!b.Enabled)
                        {
                            _registry.SetHealth(b.Alias, false);
                            continue;
                        }

                        if (DateTime.UtcNow < GetNextCheck(b.Alias))
                        {
                            continue;
                        }

                        bool healthy = await CheckOneAsync(b, ct).ConfigureAwait(false);
                        _registry.SetHealth(b.Alias, healthy);
                        SetNextCheck(b, healthy);
                    }
                }
                catch
                {
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                }
                catch
                {
                    break;
                }
            }
        }

        private async Task<bool> CheckOneAsync(IAiModelConfig modelConfig, CancellationToken ct)
        {
            if (modelConfig == null || String.IsNullOrWhiteSpace(modelConfig.Endpoint) || (modelConfig.Kind == ModelKind.LlamaCpp && modelConfig.Details == null))
            {
                return false;
            }

            string path = modelConfig.Kind switch
            {
                ModelKind.OpenAI => Constants.HealthPathOpenAI,
                ModelKind.LlamaCpp => Constants.HealthPathLlamaCpp,
                _ => Constants.HealthPathDefault,
            };

            try
            {
                HttpClient client = _httpFactory.CreateClient(Constants.BackendClientName);
                client.Timeout = TimeSpan.FromSeconds(GetProbeTimeoutSeconds(modelConfig));
                using HttpRequestMessage request = new(HttpMethod.Get, modelConfig.Endpoint.TrimEnd(Constants.CharForwardSlash) + path);
                ConfigureAuthentication(modelConfig, request);

                HttpResponseMessage res = await client.SendAsync(request, ct).ConfigureAwait(false);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static int GetProbeTimeoutSeconds(IAiModelConfig modelConfig)
        {
            return modelConfig.HealthProbeSeconds > 0 ? modelConfig.HealthProbeSeconds : Constants.DefaultHealthProbeSecondsTimeout;
        }

        private static void ConfigureAuthentication(IAiModelConfig modelConfig, HttpRequestMessage request)
        {
            if (modelConfig.Kind != ModelKind.OpenAI)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(modelConfig.ApiKeyEnvVar))
            {
                return;
            }

            string key = Environment.GetEnvironmentVariable(modelConfig.ApiKeyEnvVar);

            if (String.IsNullOrWhiteSpace(key))
            {
                return;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue(Constants.AuthenticationSchemeBearer, key);
        }

        private static TimeSpan GetInterval(IAiModelConfig modelConfig)
        {
            if (modelConfig.Kind == ModelKind.LlamaCpp &&
                modelConfig.HealthProbeSeconds > 0)
            {
                return TimeSpan.FromSeconds(modelConfig.HealthProbeSeconds);
            }

            return DefaultInterval;
        }

        private DateTime GetNextCheck(string alias)
        {
            lock (_nextCheck)
            {
                return _nextCheck.TryGetValue(alias, out DateTime next) ? next : DateTime.MinValue;
            }
        }

        private void SetNextCheck(IAiModelConfig modelConfig, bool healthy)
        {
            // Back off slightly on unhealthy backends so we do not hammer a down endpoint.
            TimeSpan interval = healthy ? GetInterval(modelConfig) : TimeSpan.FromSeconds(modelConfig.HealthProbeSeconds);

            lock (_nextCheck)
            {
                _nextCheck[modelConfig.Alias] = DateTime.UtcNow + interval;
            }
        }

        public void Dispose()
        {
            try
            {
                _subscription?.Dispose();
                _cts?.Dispose();
            }
            catch
            {
            }
        }
    }
}

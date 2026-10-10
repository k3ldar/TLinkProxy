using Microsoft.AspNetCore.Mvc;
using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Models;

namespace TLinkProxy.Controllers
{
    public sealed class HomeController : Controller
    {
        private readonly IAiBackendRegistry _registry;
        private readonly IModelScheduler _scheduler;
        private readonly ILlamaCppProcessManager _processManager;
        private readonly ITokenStatsStore _dailyTokenStore;
        private readonly ISessionMetricStore _sessionMetricStore;

        public HomeController(IAiBackendRegistry registry, IModelScheduler scheduler, ILlamaCppProcessManager processManager, ITokenStatsStore dailyTokenStore, ISessionMetricStore sessionMetricStore)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _dailyTokenStore = dailyTokenStore ?? throw new ArgumentNullException(nameof(dailyTokenStore));
            _sessionMetricStore = sessionMetricStore ?? throw new ArgumentNullException(nameof(sessionMetricStore));
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("home/status")]
        public async Task<IActionResult> Status()
        {
            return Json(await BuildStatus());
        }

        private async Task<DashboardStatusModel> BuildStatus()
        {
            DashboardStatusModel result = new()
            {
                GeneratedAtUtc = DateTime.UtcNow
            };

            ITokenDailyRecord todaySummary = _dailyTokenStore.GetToday();
            ITokenDailyRecord monthSummary = _dailyTokenStore.GetThisMonth();
            ITokenDailyRecord lifetimeSummary = _dailyTokenStore.GetLifetime();
            List<ISessionMetricRecord> sessionMetrics = _sessionMetricStore.GetAll();

            List<ToolUsageSummaryDto> toolsCalled = [.. sessionMetrics
                .SelectMany(metric => metric.ToolInvocations ?? [])
                .Where(invocation => !String.IsNullOrWhiteSpace(invocation?.Name))
                .GroupBy(invocation => invocation.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ToolUsageSummaryDto
                {
                    Name = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)];

            result.Optimizer = new DashboardOptimizerSummaryDto
            {
                Today = CreatePeriodSummary(todaySummary),
                ThisMonth = CreatePeriodSummary(monthSummary),
                Lifetime = CreatePeriodSummary(lifetimeSummary),
                ToolsCalled = toolsCalled
            };

            IReadOnlyList<IAiModelConfig> modelConfigs = _registry.GetAllBackends();

            foreach (IAiModelConfig modelConfig in modelConfigs)
            {
                if (modelConfig == null)
                {
                    continue;
                }

                int concurrencyLimit = Math.Max(1, modelConfig.ConcurrencyLimit);
                int availableSlots = _scheduler.GetAvailableSlots(modelConfig.Alias);
                int inFlight = Math.Max(0, concurrencyLimit - availableSlots);

                ModelStatusDto dto = new()
                {
                    Alias = modelConfig.Alias,
                    Kind = modelConfig.Kind,
                    Model = modelConfig.Model,
                    Enabled = modelConfig.Enabled,
                    Healthy = _registry.IsHealthy(modelConfig.Alias),
                    IsOnDemand = modelConfig.IsOnDemand,
                    IsRunning = _registry.StateStore.GetRunning(modelConfig.Alias),
                    ConcurrencyLimit = concurrencyLimit,
                    InFlight = inFlight,
                    QueueDepth = _scheduler.GetQueueDepth(modelConfig.Alias),
                    MaxQueueDepth = modelConfig.AllowQueue ? Math.Max(0, modelConfig.MaxQueueDepth) : 0,
                    IdleTimeoutSeconds = modelConfig.Details?.TimeoutSeconds ?? 0,
                    InactiveAtUtc = null
                };

                if (dto.IsOnDemand && dto.IsRunning && dto.IdleTimeoutSeconds > 0)
                {
                    DateTime? lastActivityUtc = _processManager.GetLastActivityUtc(modelConfig.Alias);

                    if (lastActivityUtc.HasValue)
                    {
                        dto.InactiveAtUtc = lastActivityUtc.Value.AddSeconds(dto.IdleTimeoutSeconds);
                    }
                }

                result.Models.Add(dto);
            }

            return result;
        }

        private static TokenPeriodSummaryDto CreatePeriodSummary(ITokenDailyRecord record)
        {
            if (record == null)
            {
                return new TokenPeriodSummaryDto();
            }

            return new TokenPeriodSummaryDto
            {
                BytesProcessed = record.OriginalTokens * Constants.CharsPerToken,
                TokensProcessed = record.OriginalTokens,
                TokensSaved = record.TokensSaved,
                ApiCalls = record.RequestCount
            };
        }
    }
}

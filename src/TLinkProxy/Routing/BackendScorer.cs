using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Base.Configuration;
using TLinkProxy.Kernel;

namespace TLinkProxy.Routing
{
    /// <summary>
    /// Orders candidates by a weighted sum of capability match, latency, cost, configured weight,
    /// and current queue pressure.
    /// </summary>
    public sealed class BackendScorer : IBackendScorer
    {
        private const double WeightScale = 0.5;
        private readonly IModelScheduler _scheduler;
        private readonly ICostEstimator _costEstimator;

        public BackendScorer(IModelScheduler scheduler, ICostEstimator costEstimator)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _costEstimator = costEstimator ?? throw new ArgumentNullException(nameof(costEstimator));
        }

        public IAiModelConfig Score(RoutingEnvelope routing, OpenAIModel model, IReadOnlyList<IAiModelConfig> candidates)
        {
            AiModelConfig best = null;
            double bestScore = double.MinValue;

            foreach (AiModelConfig modelConfig in candidates)
            {
                double score = ScoreSingle(routing, model, modelConfig);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = modelConfig;
                }
            }

            return best;
        }

        public double ScoreSingle(RoutingEnvelope routing, OpenAIModel model, IAiModelConfig modelConfig)
        {
            if (modelConfig == null)
            {
                return double.MinValue;
            }

            double score = (modelConfig.Weight < 0 ? Constants.DefaultWeight : modelConfig.Weight) * WeightScale;

            score += modelConfig.LatencyProfile?.ToLowerInvariant() switch
            {
                Constants.LatencyFast => Constants.FastLatencyBonus,
                Constants.LatencyMedium => Constants.MediumLatencyBonus,
                Constants.LatencySlow => Constants.SlowLatencyBonus,
                _ => Constants.UnknownLatencyBonus,
            };

            if (routing?.RequestType != null &&
                modelConfig.Tags != null &&
                modelConfig.Tags.Contains(routing.RequestType, StringComparer.OrdinalIgnoreCase))
            {
                score += Constants.RequestTypeBonus;
            }

            // Higher estimated cost lowers the score.
            score -= _costEstimator.Estimate(model, modelConfig) * Constants.CostPenaltyMultiplier;

            // Queue pressure: deeper queues and fewer free slots lower the score.
            score -= _scheduler.GetQueueDepth(modelConfig.Alias) * Constants.QueuePenaltyMultiplier;
            score += _scheduler.GetAvailableSlots(modelConfig.Alias) * Constants.AvailableSlotsBonusMultiplier;

            if (modelConfig.Tags != null && modelConfig.Tags.Contains(Constants.RequestTypeToolCalling, StringComparer.OrdinalIgnoreCase))
            {
                score += Constants.ToolCallingBonus;
            }

            if (String.Equals(modelConfig.Compute, Constants.CapabilityGpu, StringComparison.OrdinalIgnoreCase))
            {
                score += Constants.GpuBonus;
            }

            return score;
        }
    }
}

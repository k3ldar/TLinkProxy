using TLinkProxy.Abstractions;
using TLinkProxy.Kernel;

namespace TLinkProxy.Routing
{
    public sealed class BackendRouter : IBackendRouter
    {
        private readonly IAiBackendRegistry _registry;
        private readonly IRuleEngine _ruleEngine;
        private readonly BackendScorer _scorer;
        private readonly ICostEstimator _costEstimator;

        public BackendRouter(
            IAiBackendRegistry registry,
            RuleEngine ruleEngine,
            BackendScorer scorer,
            ICostEstimator costEstimator)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
            _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
            _costEstimator = costEstimator ?? throw new ArgumentNullException(nameof(costEstimator));
        }

        public IAiModelConfig ChooseBackend(OpenAIModel modelContext, RoutingEnvelope routing)
        {
            IReadOnlyList<IAiModelConfig> candidates = ChooseCandidates(modelContext, routing);
            return candidates.Count > 0 ? candidates[0] : null;
        }

        public IReadOnlyList<IAiModelConfig> ChooseCandidates(OpenAIModel modelContext, RoutingEnvelope routing)
        {
            List<IAiModelConfig> healthy = [.. _registry.GetAllBackends().Where(b => b != null && _registry.IsHealthy(b.Alias))];

            if (healthy.Count == 0)
            {
                return [];
            }

            // Legacy behavior: when no routing envelope is present, an explicit model alias (or
            // backend model name) selects the backend directly.
            if ((routing == null || String.IsNullOrWhiteSpace(routing.PreferredModel)) &&
                !String.IsNullOrWhiteSpace(modelContext?.Model))
            {
                IAiModelConfig byAlias = _registry.GetBackendByAlias(modelContext.Model);

                if (byAlias != null && _registry.IsHealthy(byAlias.Alias))
                {
                    return [byAlias];
                }

                IAiModelConfig byModel = healthy.FirstOrDefault(b =>
                    String.Equals(b.Model, modelContext.Model, StringComparison.OrdinalIgnoreCase));

                if (byModel != null)
                {
                    return [byModel];
                }
            }

            int estimatedInputTokens = _costEstimator.EstimateInputTokens(modelContext);

            IReadOnlyList<IAiModelConfig> filtered = _ruleEngine.Apply(routing, modelContext?.Model, healthy, estimatedInputTokens);

            if (filtered.Count == 0)
            {
                return [];
            }

            return [.. filtered
                .Select(b => new { Backend = b, Score = _scorer.ScoreSingle(routing, modelContext, b) })
                .OrderByDescending(x => x.Score)
                .Select(x => x.Backend)];
        }
    }
}

using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Kernel;

namespace TLinkProxy.Routing
{
    /// <summary>
    /// Ordered rule pipeline that turns a routing envelope + candidate list into a filtered,
    /// ordered list of backends. Each stage may reorder or filter candidates.
    /// </summary>
    public sealed class RuleEngine : IRuleEngine
    {
        private static readonly Dictionary<string, string[]> RequestTypePreferences = new(StringComparer.OrdinalIgnoreCase)
        {
            [Constants.RequestTypeCodeReview] = [Constants.ModelAliasMfDoom, Constants.RequestTypeCodeReview],
            [Constants.RequestTypeTicketReview] = [Constants.ModelAliasQwen, Constants.RequestTypeTicketReview],
            [Constants.RequestTypeChat] = [Constants.ModelAliasLlamaLocal, Constants.RequestTypeGeneralChat, Constants.RequestTypeFastResponse],
            [Constants.RequestTypeAnalysis] = [Constants.ModelAliasDeepSeek, Constants.RequestTypeAnalysis, Constants.RequestTypeLongContext],
        };

        public IReadOnlyList<IAiModelConfig> Apply(
            RoutingEnvelope routing,
            string model,
            IReadOnlyList<IAiModelConfig> candidates,
            int estimatedInputTokens)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return [];
            }

            List<IAiModelConfig> list = [.. candidates];

            // Rule 1: explicit preferredModel moves that backend to the front.
            if (routing != null && !String.IsNullOrWhiteSpace(routing.PreferredModel))
            {
                IAiModelConfig preferred = list.FirstOrDefault(b =>
                    String.Equals(b.Alias, routing.PreferredModel, StringComparison.OrdinalIgnoreCase));

                if (preferred != null)
                {
                    list.Remove(preferred);
                    list.Insert(0, preferred);
                }
            }

            // Rule 2: requestType mapping reorders candidates by preference.
            if (routing != null && !String.IsNullOrWhiteSpace(routing.RequestType))
            {
                list = OrderByPreference(list, routing.RequestType);
            }

            // Rule 3: requiredCapabilities filter candidates.
            if (routing?.RequiredCapabilities != null)
            {
                foreach (string capability in routing.RequiredCapabilities)
                {
                    list = FilterByCapability(list, capability, estimatedInputTokens);
                }
            }

            return list;
        }

        private static List<IAiModelConfig> OrderByPreference(List<IAiModelConfig> list, string requestType)
        {
            if (!RequestTypePreferences.TryGetValue(requestType, out string[] preferences))
            {
                return list;
            }

            List<IAiModelConfig> ordered = new(list.Count);
            HashSet<IAiModelConfig> used = [];

            foreach (string preference in preferences)
            {
                foreach (IAiModelConfig modelConfig in list)
                {
                    if (used.Contains(modelConfig))
                    {
                        continue;
                    }

                    if (String.Equals(modelConfig.Alias, preference, StringComparison.OrdinalIgnoreCase) ||
                        (modelConfig.Tags != null && modelConfig.Tags.Contains(preference, StringComparer.OrdinalIgnoreCase)))
                    {
                        ordered.Add(modelConfig);
                        used.Add(modelConfig);
                    }
                }
            }

            // Append any remaining backends in original order.
            foreach (IAiModelConfig modelConfig in list)
            {
                if (!used.Contains(modelConfig))
                {
                    ordered.Add(modelConfig);
                }
            }

            return ordered;
        }

        private static List<IAiModelConfig> FilterByCapability(List<IAiModelConfig> list, string capability, int estimatedInputTokens)
        {
            string cap = capability?.Trim().ToLowerInvariant() ?? String.Empty;

            return cap switch
            {
                Constants.CapabilityGpu => [.. list.Where(b => !String.Equals(b.Compute, Constants.CapabilityCpu, StringComparison.OrdinalIgnoreCase))],
                Constants.CapabilityToolCalling => [.. list.Where(b => b.Tags != null && b.Tags.Contains(Constants.RequestTypeToolCalling, StringComparer.OrdinalIgnoreCase))],
                Constants.CapabilityLongContext => [.. list.Where(b => b.MaxContext >= estimatedInputTokens + Constants.DefaultLongContextBufferTokens)],
                // fast-response is a preference (handled by the scorer), not a hard filter.
                _ => list,
            };
        }
    }
}

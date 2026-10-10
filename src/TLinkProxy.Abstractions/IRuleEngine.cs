using TLinkProxy.Kernel;

namespace TLinkProxy.Abstractions
{
    public interface IRuleEngine
    {
        IReadOnlyList<IAiModelConfig> Apply(RoutingEnvelope routing, string model, IReadOnlyList<IAiModelConfig> candidates, int estimatedInputTokens);
    }
}

using TLinkProxy.Kernel;

namespace TLinkProxy.Abstractions
{
    public interface IBackendScorer
    {
        IAiModelConfig Score(RoutingEnvelope routing, OpenAIModel model, IReadOnlyList<IAiModelConfig> candidates);
        double ScoreSingle(RoutingEnvelope routing, OpenAIModel model, IAiModelConfig modelConfig);
    }
}

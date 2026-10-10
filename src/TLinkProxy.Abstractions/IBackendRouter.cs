using TLinkProxy.Kernel;

namespace TLinkProxy.Abstractions
{
    public interface IBackendRouter
    {
        /// <summary>Returns the single best backend for a request, or <c>null</c> if none match.</summary>
        IAiModelConfig ChooseBackend(OpenAIModel modelContext, RoutingEnvelope routing);

        /// <summary>Returns all matching backends in scored order (best first), for fallback.</summary>
        IReadOnlyList<IAiModelConfig> ChooseCandidates(OpenAIModel modelContext, RoutingEnvelope routing);
    }
}

using TLinkProxy.Kernel;

namespace TLinkProxy.Abstractions
{
    /// <summary>
    /// Estimates the monetary cost of routing a request to a specific backend.
    /// Used by the scorer to deprioritize expensive/slow backends.
    /// </summary>
    public interface ICostEstimator
    {
        double Estimate(OpenAIModel request, IAiModelConfig modelConfig);

        /// <summary>
        /// Estimates the number of input tokens for a request (chars/4 heuristic).
        /// </summary>
        int EstimateInputTokens(OpenAIModel request);
    }
}

using TLinkProxy.Kernel;

namespace TLinkProxy.Abstractions
{
    public interface IBackendModelSelector
    {
        /// <summary>
        /// Resolve the backend model string from the selected backend config (Model preferred, then Alias).
        /// Returns null if no model can be resolved.
        /// </summary>
        string ResolveModel(IAiModelConfig selectedBackend);

        /// <summary>
        /// Apply the resolved backend model to the provided OpenAIModel.
        /// Returns a change description string if a change was applied, otherwise null.
        /// </summary>
        string ApplyModel(OpenAIModel model, IAiModelConfig selectedBackend);
    }
}

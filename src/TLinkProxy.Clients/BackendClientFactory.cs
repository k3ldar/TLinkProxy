using Microsoft.Extensions.DependencyInjection;
using TLinkProxy.Abstractions;

namespace TLinkProxy.Clients
{
    public class BackendClientFactory
    {
        private readonly IServiceProvider _sp;

        public BackendClientFactory(IServiceProvider sp)
        {
            _sp = sp;
        }

        public IBackendClient CreateFor(IAiModelConfig modelConfig)
        {
            ArgumentNullException.ThrowIfNull(modelConfig);
            ModelKind kind = modelConfig.Kind;

            return kind switch
            {
                ModelKind.OpenAI => ActivatorUtilities.CreateInstance<OpenAIBackendClient>(_sp),
                ModelKind.LlamaCpp => ActivatorUtilities.CreateInstance<LlamaCppBackendClient>(_sp),
                ModelKind.Ollama => ActivatorUtilities.CreateInstance<OllamaBackendClient>(_sp),
                _ => ActivatorUtilities.CreateInstance<OllamaBackendClient>(_sp),
            };
        }
    }
}

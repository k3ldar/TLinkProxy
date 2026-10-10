using Microsoft.Extensions.DependencyInjection;
using PluginManager.Abstractions;
using TLinkProxy.Abstractions;
using TLinkProxy.Services.ContextOptimizers;

namespace TLinkProxy.Services
{
    public sealed class PluginInitialisation : IPlugin
    {
        #region IPlugin Methods

        public void Initialise(ILogger logger)
        {
            // from interface but unused in this context
        }

        public void Finalise()
        {
            // from interface but unused in this context
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IEventBus, InProcessEventBus>();
            services.AddSingleton<IProxySessionLogger, ProxySessionLogger>();
            services.AddSingleton<IContextOptimizerFactory, ContextOptimizerFactory>();
            services.AddSingleton<IToolCallPolicy, ToolCallPolicy>();
            services.AddSingleton<IToolCallInspector, ToolCallInspector>();
            services.AddSingleton<ISessionRegistry, SessionRegistry>();
            services.AddSingleton<IAiBackendRegistry, AiBackendRegistry>();
            services.AddSingleton<IBackendModelSelector, BackendModelSelector>();
            services.AddSingleton<IOpenAiRequestParser, OpenAiRequestParser>();
        }

        public ushort GetVersion()
        {
            return 1;
        }

        #endregion IPlugin Methods
    }
}

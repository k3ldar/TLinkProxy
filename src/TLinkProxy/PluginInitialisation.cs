using PluginManager.Abstractions;
using TLinkProxy.Abstractions;
using TLinkProxy.Routing;
using SharedPluginFeatures;

namespace TLinkProxy
{
    public sealed class PluginInitialisation : IPlugin, IInitialiseEvents
    {
        #region IPlugin Methods

        public void Initialise(PluginManager.Abstractions.ILogger logger)
        {
            // from interface but unused in this context
        }

        public void Finalise()
        {
            // from interface but unused in this context
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IRuleEngine, RuleEngine>();
            services.AddTransient<IBackendScorer, BackendScorer>();
            services.AddTransient<IBackendRouter, BackendRouter>();
        }

        public ushort GetVersion()
        {
            return 1;
        }

        #endregion IPlugin Methods

        #region IInitialiseEvents Methods

        public void AfterConfigure(in IApplicationBuilder app)
        {
            // from interface but unused in this context
        }

        public void AfterConfigureServices(in IServiceCollection services)
        {
            // from interface but unused in this context
        }

        public void BeforeConfigure(in IApplicationBuilder app)
        {
            // from interface but unused in this context
        }

        public void BeforeConfigureServices(in IServiceCollection services)
        {
            // from interface but unused in this context
        }

        public void Configure(in IApplicationBuilder app)
        {
            // from interface but unused in this context
        }

        #endregion IInitialiseEvents Methods
    }
}

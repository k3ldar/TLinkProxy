using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PluginManager.Abstractions;
using TLinkProxy.Abstractions;
using TLinkProxy.Base.Models;
using SharedPluginFeatures;

namespace TLinkProxy.Processes
{
    public sealed class PluginInitialisation : IPlugin, IInitialiseEvents
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
            services.AddSingleton<ILlamaCppProcessManager, LlamaCppProcessManager>();
            services.AddSingleton<IAiInstanceHealthMonitor, AiInstanceHealthMonitor>();
            services.AddHostedService(sp => sp.GetRequiredService<ILlamaCppProcessManager>());
            services.AddHostedService(sp => sp.GetRequiredService<IAiInstanceHealthMonitor>());
            services.AddSingleton<IAiModelRuntimeStateStore, AiModelRuntimeStateStore>();
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

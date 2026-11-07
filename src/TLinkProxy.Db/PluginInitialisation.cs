using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PluginManager.Abstractions;
using TLinkProxy.Abstractions;
using TLinkProxy.Db.Internal;
using SharedPluginFeatures;

namespace TLinkProxy.Db
{
    public sealed class PluginInitialisation : IPlugin, IInitialiseEvents
    {
        private const string SqliteDatabaseFileName = "TLinkProxy.db";

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
            string databaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), nameof(TLinkProxy));
            Directory.CreateDirectory(databaseDirectory);

            SqliteDatabase database = new(Path.Combine(databaseDirectory, SqliteDatabaseFileName));
            database.EnsureSchema();
            services.AddSingleton(database);

            // service registrations providing higher-level functionality over the database
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IAiModelStore, AiModelStore>();
            services.AddSingleton<ITokenStatsStore, TokenStatsStore>();
            services.AddSingleton<ISessionMetricStore, SessionMetricStore>();
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

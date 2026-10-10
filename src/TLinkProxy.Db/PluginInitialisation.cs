using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PluginManager.Abstractions;
using TLinkProxy.Abstractions;
using TLinkProxy.Db.Internal;
using TLinkProxy.Db.Tables;
using SharedPluginFeatures;
using SimpleDB;

namespace TLinkProxy.Db
{
    public sealed class PluginInitialisation : IPlugin, IInitialiseEvents
    {
        private const string DatabaseEncryptionKeyEnvVar = "PF_DB_ENCRYPTION_KEY";
        private const int DefaultEncryptionKeyLength = 32;
        private const string DatabasePathName = "Database";

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
            // tables
            services.AddSingleton(typeof(TableRowDefinition), typeof(SettingsDataRow));
            services.AddSingleton(typeof(TableRowDefinition), typeof(AIModelDataRow));
            services.AddSingleton(typeof(TableRowDefinition), typeof(TokenDailyDataRow));

            // service registrations for wrapping table access and providing higher-level functionality
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
            string encryptionKey = Environment.GetEnvironmentVariable(DatabaseEncryptionKeyEnvVar);

            if (String.IsNullOrWhiteSpace(encryptionKey))
            {
                encryptionKey = Shared.Utilities.GetRandomWord(DefaultEncryptionKeyLength); // Generate a random 32-character encryption key
                Environment.SetEnvironmentVariable(DatabaseEncryptionKeyEnvVar, encryptionKey, EnvironmentVariableTarget.User);
            }

            string databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), nameof(TLinkProxy), DatabasePathName);

            if (!Directory.Exists(databasePath))
            {
                Directory.CreateDirectory(databasePath);
            }

            services.AddSimpleDB(databasePath, encryptionKey);
        }

        public void Configure(in IApplicationBuilder app)
        {
            // from interface but unused in this context
        }

        #endregion IInitialiseEvents Methods
    }
}

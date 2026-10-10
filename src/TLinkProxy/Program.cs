using AspNetCore.PluginManager;
using PluginManager;
using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Costing;
using TLinkProxy.Internal;
using TLinkProxy.Routing;
using TLinkProxy.Scheduling;
using Shared.Classes;
using System.Diagnostics.CodeAnalysis;

namespace TLinkProxy
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        private const int ThreadHangTimeout = 5;
        private const int ThreadMaximumRunning = 50;
        private const int ThreadMaximumPoolSize = 500;
        private const string DefaultScheme = "DefaultAuthSchemeName";
        private const string SessionCookieName = "pf_session";
        private const string AccessDeniedRoute = "/Error/AccessDenied";
        private const string LoginRoute = "/Login/";
        private const int SessionCookieExpireDays = 28;

        public static void Main(string[] args)
        {
            ThreadManager.Initialise(new SharedLib.Win.WindowsCpuUsage());
            ThreadManager.MaximumPoolSize = ThreadMaximumPoolSize;
            ThreadManager.MaximumRunningThreads = ThreadMaximumRunning;
            ThreadManager.ThreadCpuChangeNotification = 0;
            ThreadManager.ThreadHangTimeout = TimeSpan.FromMinutes(ThreadHangTimeout);

            PluginManagerService.UsePlugin(typeof(SimpleDB.PluginInitialisation));
            PluginManagerService.UsePlugin(typeof(Db.PluginInitialisation));
            PluginManagerService.UsePlugin(typeof(Base.PluginInitialisation));
            PluginManagerService.UsePlugin(typeof(Clients.PluginInitialisation));
            PluginManagerService.UsePlugin(typeof(Proxy.PluginInitialisation));
            PluginManagerService.UsePlugin(typeof(Processes.PluginInitialisation));
            PluginManagerService.UsePlugin(typeof(Configuration.PluginInitialisation));
            PluginManagerService.UsePlugin(typeof(Services.PluginInitialisation));

            PluginManagerConfiguration configuration = new()
            {
                ServiceConfigurator = new ServiceConfigurator()
            };

            // Initialise the plugin manager service
            PluginManagerService.Initialise(configuration);

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Allow running as a Windows Service (e.g. "sc.exe create TLinkProxy ...").
            builder.Host.UseWindowsService();

            PluginManagerService.ConfigureServices(builder.Services);

            // Add services to the container.
            IMvcBuilder mvcBuilder = builder.Services.AddControllersWithViews();


            builder.Services.AddSingleton<RuleEngine>();
            builder.Services.AddSingleton<BackendScorer>();
            builder.Services.AddSingleton<ICostEstimator, CostEstimator>();
            builder.Services.AddSingleton<IBackendRouter, BackendRouter>();
            builder.Services.AddSingleton<IModelScheduler, ModelScheduler>();
            builder.Services.AddHttpClient(Constants.BackendClientName);

            builder.Services.AddAuthentication(DefaultScheme)
                .AddCookie(DefaultScheme, options =>
                {
                    options.Cookie.Name = SessionCookieName;
                    options.AccessDeniedPath = AccessDeniedRoute;
                    options.LoginPath = LoginRoute;
                    options.ExpireTimeSpan = TimeSpan.FromDays(SessionCookieExpireDays);
                });

            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                //options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = false;
            });

            mvcBuilder.ConfigurePluginManager()
                .AddRazorRuntimeCompilation();

            string proxyUrl = Environment.GetEnvironmentVariable(Constants.ProxyUrlEnvVar) ?? Constants.DefaultProxyUrl;
            //builder.WebHost.UseUrls(proxyUrl);

            WebApplication app = builder.Build();
            PluginManagerService.Configure(app);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler(Constants.ErrorRoutePath);
            }

            app.UseRouting();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthorization();
            app.MapControllerRoute(
                name: Constants.DefaultRouteName,
                pattern: Constants.DefaultRoutePattern);
            app.UsePluginManager();

            app.MapStaticAssets();
            app.UsePluginManager();
            app.Run();
        }
    }
}

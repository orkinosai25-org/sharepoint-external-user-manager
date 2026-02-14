using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharePointExternalUserManager.Functions.Middleware;
using SharePointExternalUserManager.Functions.Services;
using SharePointExternalUserManager.Functions.Services.Search;
using SharePointExternalUserManager.Functions.Services.RateLimiting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(workerApplication =>
    {
        // Add authentication middleware
        workerApplication.UseMiddleware<AuthenticationMiddleware>();
        
        // Add rate limiting middleware
        workerApplication.UseMiddleware<RateLimitingMiddleware>();
        
        // Add licensing enforcement middleware
        workerApplication.UseMiddleware<LicenseEnforcementMiddleware>();
    })
    .ConfigureServices(services =>
    {
        // Add Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Register services
        services.AddSingleton<ILicensingService, LicensingService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IRateLimitingService, RateLimitingService>();

        // TODO: Add more services as needed
        // services.AddSingleton<ITenantService, TenantService>();
        // services.AddSingleton<IGraphApiService, GraphApiService>();
    })
    .Build();

host.Run();

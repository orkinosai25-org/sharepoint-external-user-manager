using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharePointExternalUserManager.Functions.Middleware;
using SharePointExternalUserManager.Functions.Services;
using SharePointExternalUserManager.Functions.Services.Search;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(workerApplication =>
    {
        // Add authentication middleware
        workerApplication.UseMiddleware<AuthenticationMiddleware>();
        
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

        // TODO: Add more services as needed
        // services.AddSingleton<ITenantService, TenantService>();
        // services.AddSingleton<IGraphApiService, GraphApiService>();
    })
    .Build();

host.Run();

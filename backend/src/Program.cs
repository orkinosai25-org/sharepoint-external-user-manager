using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharePointExternalUserManager.Functions.Middleware;
using SharePointExternalUserManager.Functions.Services;

var builder = FunctionsApplication.CreateBuilder(args);

// Configure Functions Web Application with middleware
builder.ConfigureFunctionsWebApplication(workerApplication =>
{
    // Add authentication middleware
    workerApplication.UseMiddleware<AuthenticationMiddleware>();
    
    // Add licensing enforcement middleware
    workerApplication.UseMiddleware<LicenseEnforcementMiddleware>();
});

// Add Application Insights
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register services
builder.Services.AddSingleton<ILicensingService, LicensingService>();

// TODO: Add more services as needed
// builder.Services.AddSingleton<ITenantService, TenantService>();
// builder.Services.AddSingleton<IGraphApiService, GraphApiService>();

builder.Build().Run();

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.IntegrationTests.Mocks;
using SharePointExternalUserManager.Api.Services;

namespace SharePointExternalUserManager.Api.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
/// <typeparam name="TProgram">The program type (typically Program from the API project)</typeparam>
public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AzureAd:ClientId", "test-client-id" },
                { "AzureAd:TenantId", "common" },
                { "AzureAd:ClientSecret", "test-secret" },
                { "MicrosoftGraph:Scopes", "User.Read" },
                { "ConnectionStrings:DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=SharePointExternalUserManagerIntegrationTests;Trusted_Connection=True;MultipleActiveResultSets=true" },
                { "Stripe:SecretKey", "sk_test_fake_key" },
                { "Stripe:WebhookSecret", "whsec_test_secret" },
                { "AzureOpenAI:UseDemoMode", "true" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using an in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase($"InMemoryTestDb_{Guid.NewGuid()}");
            });

            // Replace Graph-dependent services with mocks
            services.Replace(ServiceDescriptor.Scoped<ISharePointService, MockSharePointService>());

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();
        });
    }
}

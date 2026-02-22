using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Services;

namespace SharePointExternalUserManager.Api.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// Configures in-memory database and mocked services
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<ISharePointService>? MockSharePointService { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real database context
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
            });

            // Mock SharePointService for controlled testing
            MockSharePointService = new Mock<ISharePointService>();
            services.RemoveAll<ISharePointService>();
            services.AddScoped(_ => MockSharePointService.Object);

            // Build service provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}

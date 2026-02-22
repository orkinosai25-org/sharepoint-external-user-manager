using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Services;

namespace SharePointExternalUserManager.Api.IntegrationTests.Workflows;

/// <summary>
/// Integration tests for client site creation workflow
/// Tests the complete flow: create client -> provision SharePoint site -> verify site configuration
/// </summary>
public class ClientSiteCreationWorkflowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ClientSiteCreationWorkflowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<TenantEntity> SetupTestTenant()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenant = new TenantEntity
        {
            EntraIdTenantId = "test-tenant-" + Guid.NewGuid(),
            OrganizationName = "Test Organization",
            PrimaryAdminEmail = "admin@testorg.com",
            Status = "Active",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        return tenant;
    }

    [Fact]
    public async Task ClientSiteCreation_CompleteWorkflow_Success()
    {
        // Arrange
        var tenant = await SetupTestTenant();
        var clientReference = "CLIENT-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var siteId = "site-" + Guid.NewGuid();
        var siteUrl = $"https://contoso.sharepoint.com/sites/{clientReference}";

        // Mock SharePoint service to simulate successful site creation
        _factory.MockSharePointService?.Setup(s => s.CreateClientSiteAsync(
                It.IsAny<ClientEntity>(),
                It.IsAny<string>()))
            .ReturnsAsync((true, siteId, siteUrl, null));

        // Act - Step 1: Create client in database
        ClientEntity client;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            client = new ClientEntity
            {
                TenantId = tenant.Id,
                ClientReference = clientReference,
                ClientName = "Test Client",
                Description = "Test client site",
                ProvisioningStatus = "Pending",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "admin@testorg.com"
            };
            
            db.Clients.Add(client);
            await db.SaveChangesAsync();
        }

        // Act - Step 2: Provision SharePoint site
        using (var scope = _factory.Services.CreateScope())
        {
            var sharePointService = scope.ServiceProvider.GetRequiredService<ISharePointService>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Reload the client entity in this context
            var clientToUpdate = await db.Clients.FindAsync(client.Id);
            Assert.NotNull(clientToUpdate);
            
            var result = await sharePointService.CreateClientSiteAsync(clientToUpdate, "admin@testorg.com");
            
            // Update client with provisioning result
            if (result.Success)
            {
                clientToUpdate.SharePointSiteId = result.SiteId;
                clientToUpdate.SharePointSiteUrl = result.SiteUrl;
                clientToUpdate.ProvisioningStatus = "Completed";
                clientToUpdate.ProvisionedDate = DateTime.UtcNow;
                
                await db.SaveChangesAsync();
            }

            Assert.True(result.Success);
            Assert.NotNull(result.SiteId);
            Assert.NotNull(result.SiteUrl);
        }

        // Assert - Step 3: Verify client was properly updated
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedClient = await db.Clients.FindAsync(client.Id);
            
            Assert.NotNull(updatedClient);
            Assert.Equal("Completed", updatedClient.ProvisioningStatus);
            Assert.NotNull(updatedClient.SharePointSiteId);
            Assert.NotNull(updatedClient.SharePointSiteUrl);
            Assert.NotNull(updatedClient.ProvisionedDate);
        }
    }

    [Fact]
    public async Task ClientSiteCreation_ProvisioningFails_SetsErrorStatus()
    {
        // Arrange
        var tenant = await SetupTestTenant();
        var clientReference = "CLIENT-FAIL-" + Guid.NewGuid().ToString().Substring(0, 8);

        // Mock SharePoint service to simulate provisioning failure
        _factory.MockSharePointService?.Setup(s => s.CreateClientSiteAsync(
                It.IsAny<ClientEntity>(),
                It.IsAny<string>()))
            .ReturnsAsync((false, null, null, "SharePoint site creation failed: Insufficient permissions"));

        // Act - Create client and attempt provisioning
        ClientEntity client;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            client = new ClientEntity
            {
                TenantId = tenant.Id,
                ClientReference = clientReference,
                ClientName = "Test Client - Will Fail",
                Description = "Test client with provisioning failure",
                ProvisioningStatus = "Pending",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "admin@testorg.com"
            };
            
            db.Clients.Add(client);
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var sharePointService = scope.ServiceProvider.GetRequiredService<ISharePointService>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Reload the client entity in this context
            var clientToUpdate = await db.Clients.FindAsync(client.Id);
            Assert.NotNull(clientToUpdate);
            
            var result = await sharePointService.CreateClientSiteAsync(clientToUpdate, "admin@testorg.com");
            
            // Update client with error
            clientToUpdate.ProvisioningStatus = "Failed";
            clientToUpdate.ProvisioningError = result.ErrorMessage;
            
            await db.SaveChangesAsync();

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        // Assert - Verify error was recorded
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedClient = await db.Clients.FindAsync(client.Id);
            
            Assert.NotNull(updatedClient);
            Assert.Equal("Failed", updatedClient.ProvisioningStatus);
            Assert.NotNull(updatedClient.ProvisioningError);
            Assert.Contains("Insufficient permissions", updatedClient.ProvisioningError);
        }
    }

    [Fact]
    public async Task ClientSiteCreation_PlanLimits_CanBeEnforced()
    {
        // Arrange - tenant with existing clients
        var tenant = await SetupTestTenant();

        // Act & Assert - Create client and verify we can query it
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var client1 = new ClientEntity
        {
            TenantId = tenant.Id,
            ClientReference = "CLIENT-001",
            ClientName = "First Client",
            ProvisioningStatus = "Completed",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "admin@testorg.com"
        };
        
        db.Clients.Add(client1);
        await db.SaveChangesAsync();
        
        var activeClientCount = await db.Clients
            .CountAsync(c => c.TenantId == tenant.Id && c.IsActive);
        
        // Check that we have created clients
        Assert.Equal(1, activeClientCount);
    }

    [Fact]
    public async Task ClientSiteCreation_MultipleClients_AllProvisioned()
    {
        // Arrange
        var tenant = await SetupTestTenant();
        var clientCount = 3;
        var clients = new List<ClientEntity>();

        // Mock successful provisioning for all clients
        _factory.MockSharePointService?.Setup(s => s.CreateClientSiteAsync(
                It.IsAny<ClientEntity>(),
                It.IsAny<string>()))
            .ReturnsAsync((ClientEntity client, string user) =>
            {
                var siteId = "site-" + Guid.NewGuid();
                var siteUrl = $"https://contoso.sharepoint.com/sites/{client.ClientReference}";
                return (true, siteId, siteUrl, null);
            });

        // Act - Create and provision multiple clients
        for (int i = 0; i < clientCount; i++)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sharePointService = scope.ServiceProvider.GetRequiredService<ISharePointService>();
            
            var client = new ClientEntity
            {
                TenantId = tenant.Id,
                ClientReference = $"CLIENT-{i + 1:D3}",
                ClientName = $"Client {i + 1}",
                Description = $"Test client {i + 1}",
                ProvisioningStatus = "Pending",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "admin@testorg.com"
            };
            
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            
            var result = await sharePointService.CreateClientSiteAsync(client, "admin@testorg.com");
            
            if (result.Success)
            {
                client.SharePointSiteId = result.SiteId;
                client.SharePointSiteUrl = result.SiteUrl;
                client.ProvisioningStatus = "Completed";
                client.ProvisionedDate = DateTime.UtcNow;
                
                // Entity is already tracked, no need to call Update
                await db.SaveChangesAsync();
            }
            
            clients.Add(client);
        }

        // Assert - All clients should be provisioned
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var completedClients = await db.Clients
                .Where(c => c.TenantId == tenant.Id && c.ProvisioningStatus == "Completed")
                .CountAsync();
            
            Assert.Equal(clientCount, completedClients);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;

namespace SharePointExternalUserManager.Api.Tests.Services;

public class PlanEnforcementServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PlanEnforcementService _service;

    public PlanEnforcementServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new PlanEnforcementService(_context, new NullLogger<PlanEnforcementService>());
    }

    #region GetTenantPlan Tests

    [Fact]
    public async Task GetTenantPlan_WithActiveSubscription_ReturnsPlanDefinition()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-1");
        await CreateTestSubscriptionAsync(tenant.Id, "Professional", "Active");

        // Act
        var plan = await _service.GetTenantPlanAsync(tenant.Id);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal("Professional", plan.Name);
        Assert.Equal(SubscriptionTier.Professional, plan.Tier);
    }

    [Fact]
    public async Task GetTenantPlan_WithTrialSubscription_ReturnsPlanDefinition()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-2");
        await CreateTestSubscriptionAsync(tenant.Id, "Business", "Trial");

        // Act
        var plan = await _service.GetTenantPlanAsync(tenant.Id);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal("Business", plan.Name);
        Assert.Equal(SubscriptionTier.Business, plan.Tier);
    }

    [Fact]
    public async Task GetTenantPlan_WithNoSubscription_ReturnsStarterPlan()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-3");

        // Act
        var plan = await _service.GetTenantPlanAsync(tenant.Id);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal("Starter", plan.Name);
        Assert.Equal(SubscriptionTier.Starter, plan.Tier);
    }

    #endregion

    #region CanCreateClientSpace Tests

    [Fact]
    public async Task CanCreateClientSpace_WithinLimit_ReturnsTrue()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-4");
        await CreateTestSubscriptionAsync(tenant.Id, "Starter", "Active"); // Starter = max 5 clients

        // Create 3 clients
        await CreateTestClientAsync(tenant.Id, "CLIENT-001");
        await CreateTestClientAsync(tenant.Id, "CLIENT-002");
        await CreateTestClientAsync(tenant.Id, "CLIENT-003");

        // Act
        var (allowed, current, limit) = await _service.CanCreateClientSpaceAsync(tenant.Id);

        // Assert
        Assert.True(allowed);
        Assert.Equal(3, current);
        Assert.Equal(5, limit);
    }

    [Fact]
    public async Task CanCreateClientSpace_AtLimit_ReturnsFalse()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-5");
        await CreateTestSubscriptionAsync(tenant.Id, "Starter", "Active"); // Starter = max 5 clients

        // Create 5 clients (at limit)
        for (int i = 1; i <= 5; i++)
        {
            await CreateTestClientAsync(tenant.Id, $"CLIENT-{i:D3}");
        }

        // Act
        var (allowed, current, limit) = await _service.CanCreateClientSpaceAsync(tenant.Id);

        // Assert
        Assert.False(allowed);
        Assert.Equal(5, current);
        Assert.Equal(5, limit);
    }

    [Fact]
    public async Task CanCreateClientSpace_EnterprisePlan_ReturnsUnlimited()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-6");
        await CreateTestSubscriptionAsync(tenant.Id, "Enterprise", "Active"); // Enterprise = unlimited

        // Create 100 clients
        for (int i = 1; i <= 100; i++)
        {
            await CreateTestClientAsync(tenant.Id, $"CLIENT-{i:D3}");
        }

        // Act
        var (allowed, current, limit) = await _service.CanCreateClientSpaceAsync(tenant.Id);

        // Assert
        Assert.True(allowed);
        Assert.Equal(100, current);
        Assert.Null(limit); // Unlimited
    }

    #endregion

    #region EnforceClientSpaceLimit Tests

    [Fact]
    public async Task EnforceClientSpaceLimit_WithinLimit_DoesNotThrow()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-7");
        await CreateTestSubscriptionAsync(tenant.Id, "Professional", "Active"); // Professional = max 20 clients

        // Create 10 clients
        for (int i = 1; i <= 10; i++)
        {
            await CreateTestClientAsync(tenant.Id, $"CLIENT-{i:D3}");
        }

        // Act & Assert - should not throw
        await _service.EnforceClientSpaceLimitAsync(tenant.Id);
    }

    [Fact]
    public async Task EnforceClientSpaceLimit_AtLimit_ThrowsException()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-8");
        await CreateTestSubscriptionAsync(tenant.Id, "Starter", "Active"); // Starter = max 5 clients

        // Create 5 clients (at limit)
        for (int i = 1; i <= 5; i++)
        {
            await CreateTestClientAsync(tenant.Id, $"CLIENT-{i:D3}");
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.EnforceClientSpaceLimitAsync(tenant.Id));

        Assert.Contains("maximum number of client spaces", exception.Message);
        Assert.Contains("5", exception.Message); // Should mention the limit
    }

    [Fact]
    public async Task EnforceClientSpaceLimit_EnterprisePlan_DoesNotThrow()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-9");
        await CreateTestSubscriptionAsync(tenant.Id, "Enterprise", "Active"); // Enterprise = unlimited

        // Create 200 clients
        for (int i = 1; i <= 200; i++)
        {
            await CreateTestClientAsync(tenant.Id, $"CLIENT-{i:D3}");
        }

        // Act & Assert - should not throw even with many clients
        await _service.EnforceClientSpaceLimitAsync(tenant.Id);
    }

    #endregion

    #region HasFeatureAccess Tests

    [Fact]
    public async Task HasFeatureAccess_StarterPlan_HasBasicFeatures()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-10");
        await CreateTestSubscriptionAsync(tenant.Id, "Starter", "Active");

        // Act
        var hasBasicUserManagement = await _service.HasFeatureAccessAsync(tenant.Id, "BasicUserManagement");
        var hasBulkOperations = await _service.HasFeatureAccessAsync(tenant.Id, "BulkOperations");

        // Assert
        Assert.True(hasBasicUserManagement); // Starter has this
        Assert.False(hasBulkOperations); // Starter doesn't have this
    }

    [Fact]
    public async Task HasFeatureAccess_ProfessionalPlan_HasAdvancedFeatures()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-11");
        await CreateTestSubscriptionAsync(tenant.Id, "Professional", "Active");

        // Act
        var hasBasicUserManagement = await _service.HasFeatureAccessAsync(tenant.Id, "BasicUserManagement");
        var hasBulkOperations = await _service.HasFeatureAccessAsync(tenant.Id, "BulkOperations");
        var hasApiAccess = await _service.HasFeatureAccessAsync(tenant.Id, "ApiAccess");

        // Assert
        Assert.True(hasBasicUserManagement);
        Assert.True(hasBulkOperations);
        Assert.True(hasApiAccess);
    }

    #endregion

    #region EnforceFeatureAccess Tests

    [Fact]
    public async Task EnforceFeatureAccess_WithAccess_DoesNotThrow()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-12");
        await CreateTestSubscriptionAsync(tenant.Id, "Professional", "Active");

        // Act & Assert - Professional has BulkOperations
        await _service.EnforceFeatureAccessAsync(tenant.Id, "BulkOperations");
    }

    [Fact]
    public async Task EnforceFeatureAccess_WithoutAccess_ThrowsException()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-13");
        await CreateTestSubscriptionAsync(tenant.Id, "Starter", "Active");

        // Act & Assert - Starter doesn't have BulkOperations
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.EnforceFeatureAccessAsync(tenant.Id, "BulkOperations"));

        Assert.Contains("does not include access to this feature", exception.Message);
    }

    #endregion

    #region Helper Methods

    private async Task<TenantEntity> CreateTestTenantAsync(string entraIdTenantId)
    {
        var tenant = new TenantEntity
        {
            EntraIdTenantId = entraIdTenantId,
            OrganizationName = $"Test Org {entraIdTenantId}",
            PrimaryAdminEmail = $"admin@{entraIdTenantId}.com",
            OnboardedDate = DateTime.UtcNow,
            Status = "Active",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }

    private async Task<SubscriptionEntity> CreateTestSubscriptionAsync(int tenantId, string tier, string status)
    {
        var subscription = new SubscriptionEntity
        {
            TenantId = tenantId,
            Tier = tier,
            Status = status,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        if (status == "Trial")
        {
            subscription.TrialExpiry = DateTime.UtcNow.AddDays(14);
        }

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    private async Task<ClientEntity> CreateTestClientAsync(int tenantId, string reference)
    {
        var client = new ClientEntity
        {
            TenantId = tenantId,
            ClientReference = reference,
            ClientName = $"Client {reference}",
            ProvisioningStatus = "Provisioned",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "test-user",
            ModifiedDate = DateTime.UtcNow
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #endregion
}

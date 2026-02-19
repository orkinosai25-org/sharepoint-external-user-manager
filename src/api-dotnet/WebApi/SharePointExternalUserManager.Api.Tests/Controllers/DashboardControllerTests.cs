using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;
using SharePointExternalUserManager.Api.Controllers;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Models.ExternalUsers;
using SharePointExternalUserManager.Functions.Models.Libraries;
using SharePointExternalUserManager.Functions.Models.Lists;

namespace SharePointExternalUserManager.Api.Tests.Controllers;

public class DashboardControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DashboardController _controller;
    private readonly MockSharePointService _sharePointService;

    public DashboardControllerTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _sharePointService = new MockSharePointService();
        _controller = new DashboardController(
            _context,
            _sharePointService,
            new NullLogger<DashboardController>());
    }

    [Fact]
    public async Task GetSummary_WithValidTenantAndData_ReturnsOk()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var upn = "testuser@example.com";

        // Add test tenant to database
        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = upn,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Professional",
            Status = "Trial", // Changed to Trial to match TrialExpiry logic
            StartDate = DateTime.UtcNow,
            TrialExpiry = DateTime.UtcNow.AddDays(10)
        };
        _context.Subscriptions.Add(subscription);

        // Add test clients
        var client1 = new ClientEntity
        {
            TenantId = tenant.Id,
            ClientReference = "CLI-001",
            ClientName = "Test Client 1",
            SharePointSiteId = "site1",
            SharePointSiteUrl = "https://test.sharepoint.com/sites/client1",
            ProvisioningStatus = "Provisioned",
            IsActive = true,
            CreatedBy = upn
        };

        var client2 = new ClientEntity
        {
            TenantId = tenant.Id,
            ClientReference = "CLI-002",
            ClientName = "Test Client 2",
            SharePointSiteId = "site2",
            SharePointSiteUrl = "https://test.sharepoint.com/sites/client2",
            ProvisioningStatus = "Provisioned",
            IsActive = true,
            CreatedBy = upn
        };

        _context.Clients.AddRange(client1, client2);
        await _context.SaveChangesAsync();

        // Setup mock SharePoint service with external users
        _sharePointService.AddExternalUsers("site1", new List<ExternalUserDto>
        {
            new ExternalUserDto { Id = "user1", Email = "user1@external.com", Status = "Active" },
            new ExternalUserDto { Id = "user2", Email = "user2@external.com", Status = "PendingAcceptance" }
        });

        _sharePointService.AddExternalUsers("site2", new List<ExternalUserDto>
        {
            new ExternalUserDto { Id = "user3", Email = "user3@external.com", Status = "Active" }
        });

        SetupControllerContext(tenantId, userId, upn);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DashboardSummaryResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        var summary = response.Data;
        Assert.Equal(2, summary.TotalClientSpaces);
        Assert.Equal(3, summary.TotalExternalUsers);
        Assert.Equal(1, summary.ActiveInvitations); // Only PendingAcceptance status
        Assert.Equal("Professional", summary.PlanTier);
        Assert.Equal("Trial", summary.Status);
        Assert.True(summary.IsActive);
        Assert.NotNull(summary.TrialDaysRemaining);
        // Allow ±1 day tolerance due to test execution timing (DateTime.UtcNow may vary slightly)
        Assert.True(summary.TrialDaysRemaining.Value >= 9 && summary.TrialDaysRemaining.Value <= 11);
        Assert.NotEmpty(summary.QuickActions);
    }

    [Fact]
    public async Task GetSummary_WithNoClients_ReturnsZeroCounts()
    {
        // Arrange
        var tenantId = "test-tenant-id-2";
        var userId = "test-user-id";
        var upn = "testuser@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = upn,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Free",
            Status = "Trial",
            TrialExpiry = DateTime.UtcNow.AddDays(5)
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        SetupControllerContext(tenantId, userId, upn);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DashboardSummaryResponse>>(okResult.Value);
        Assert.True(response.Success);

        var summary = response.Data!;
        Assert.Equal(0, summary.TotalClientSpaces);
        Assert.Equal(0, summary.TotalExternalUsers);
        Assert.Equal(0, summary.ActiveInvitations);
        Assert.Equal("Free", summary.PlanTier);
        Assert.Equal("Trial", summary.Status);
        Assert.True(summary.IsActive);
        
        // Should have trial expiring warning in quick actions
        Assert.Contains(summary.QuickActions, a => a.Id == "trial-expiring");
    }

    [Fact]
    public async Task GetSummary_WithMissingTenantClaim_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContext(null, "user-id", "user@example.com");

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Equal("AUTH_ERROR", response.Error?.Code);
    }

    [Fact]
    public async Task GetSummary_WithNonExistentTenant_ReturnsNotFound()
    {
        // Arrange
        SetupControllerContext("non-existent-tenant", "user-id", "user@example.com");

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal("TENANT_NOT_FOUND", response.Error?.Code);
    }

    [Fact]
    public async Task GetSummary_CalculatesUsagePercentagesCorrectly()
    {
        // Arrange
        var tenantId = "test-tenant-usage";
        var userId = "test-user-id";
        var upn = "testuser@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = upn,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        // Starter plan with limits: MaxClientSpaces = 5, MaxExternalUsers = 50
        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Starter",
            Status = "Active"
        };
        _context.Subscriptions.Add(subscription);

        // Add 3 clients (3/5 = 60%)
        for (int i = 1; i <= 3; i++)
        {
            var client = new ClientEntity
            {
                TenantId = tenant.Id,
                ClientReference = $"CLI-{i:000}",
                ClientName = $"Test Client {i}",
                SharePointSiteId = $"site{i}",
                SharePointSiteUrl = $"https://test.sharepoint.com/sites/client{i}",
                ProvisioningStatus = "Provisioned",
                IsActive = true,
                CreatedBy = upn
            };
            _context.Clients.Add(client);

            // Add 3 external users per client = 9 total (9/50 = 18%)
            _sharePointService.AddExternalUsers($"site{i}", new List<ExternalUserDto>
            {
                new ExternalUserDto { Id = $"user{i}1", Email = $"user{i}1@external.com", Status = "Active" },
                new ExternalUserDto { Id = $"user{i}2", Email = $"user{i}2@external.com", Status = "Active" },
                new ExternalUserDto { Id = $"user{i}3", Email = $"user{i}3@external.com", Status = "Active" }
            });
        }

        await _context.SaveChangesAsync();
        SetupControllerContext(tenantId, userId, upn);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DashboardSummaryResponse>>(okResult.Value);
        var summary = response.Data!;

        Assert.Equal(3, summary.TotalClientSpaces);
        Assert.Equal(9, summary.TotalExternalUsers);
        Assert.Equal(60, summary.Limits.ClientSpacesUsagePercent);
        Assert.Equal(18, summary.Limits.ExternalUsersUsagePercent);
    }

    [Fact]
    public async Task GetSummary_WithExpiredTrial_ReturnsCorrectStatus()
    {
        // Arrange
        var tenantId = "test-tenant-expired";
        var userId = "test-user-id";
        var upn = "testuser@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = upn,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Free",
            Status = "Trial",
            TrialExpiry = DateTime.UtcNow.AddDays(2) // Expires in 2 days
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        SetupControllerContext(tenantId, userId, upn);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DashboardSummaryResponse>>(okResult.Value);
        var summary = response.Data!;

        Assert.Equal(2, summary.TrialDaysRemaining);
        Assert.Contains(summary.QuickActions, a => a.Id == "trial-expiring" && a.Priority == "warning");
    }

    private void SetupControllerContext(string? tenantId, string userId, string upn)
    {
        var claims = new List<Claim>();
        
        if (tenantId != null)
            claims.Add(new Claim("tid", tenantId));
        
        claims.Add(new Claim("oid", userId));
        claims.Add(new Claim("upn", upn));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

// Mock SharePoint service for testing
public class MockSharePointService : ISharePointService
{
    private readonly Dictionary<string, List<ExternalUserDto>> _externalUsers = new();

    public void AddExternalUsers(string siteId, List<ExternalUserDto> users)
    {
        _externalUsers[siteId] = users;
    }

    public Task<List<ExternalUserDto>> GetExternalUsersAsync(string siteId)
    {
        return Task.FromResult(_externalUsers.ContainsKey(siteId) 
            ? _externalUsers[siteId] 
            : new List<ExternalUserDto>());
    }

    // Not implemented for dashboard tests - throw NotImplementedException
    public Task<(bool Success, string? SiteId, string? SiteUrl, string? ErrorMessage)> CreateClientSiteAsync(
        ClientEntity client, string userEmail) 
        => throw new NotImplementedException();

    public Task<(bool Success, ExternalUserDto? User, string? ErrorMessage)> InviteExternalUserAsync(
        string siteId, string email, string? displayName, string permissionLevel, string? message, string invitedBy) 
        => throw new NotImplementedException();

    public Task<(bool Success, string? ErrorMessage)> RemoveExternalUserAsync(string siteId, string email) 
        => throw new NotImplementedException();

    public Task<List<LibraryResponse>> GetLibrariesAsync(string siteId) 
        => throw new NotImplementedException();

    public Task<LibraryResponse> CreateLibraryAsync(string siteId, string name, string? description) 
        => throw new NotImplementedException();

    public Task<List<ListResponse>> GetListsAsync(string siteId) 
        => throw new NotImplementedException();

    public Task<ListResponse> CreateListAsync(string siteId, string name, string? description, string? template) 
        => throw new NotImplementedException();
}

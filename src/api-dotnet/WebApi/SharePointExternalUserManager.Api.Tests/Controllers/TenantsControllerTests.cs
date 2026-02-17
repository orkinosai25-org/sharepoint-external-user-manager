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

namespace SharePointExternalUserManager.Api.Tests.Controllers;

public class TenantsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TenantsController _controller;
    private readonly IAuditLogService _auditLogService;

    public TenantsControllerTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _auditLogService = new MockAuditLogService();
        _controller = new TenantsController(
            _context,
            _auditLogService,
            new NullLogger<TenantsController>());
    }

    [Fact]
    public async Task GetMe_WithValidClaims_ReturnsOk()
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
            Tier = "Free",
            Status = "Active"
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        SetupControllerContext(tenantId, userId, upn);

        // Act
        var result = await _controller.GetMe();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task GetMe_WithMissingClaims_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContext(null, "user-id", "user@example.com");

        // Act
        var result = await _controller.GetMe();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Equal("AUTH_ERROR", response.Error?.Code);
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var tenantId = "new-tenant-id";
        var request = new TenantRegistrationRequest
        {
            OrganizationName = "New Organization",
            PrimaryAdminEmail = "admin@neworg.com",
            Settings = new TenantSettings
            {
                SharePointTenantUrl = "neworg.sharepoint.com"
            }
        };

        SetupControllerContext(tenantId, "user-id", request.PrimaryAdminEmail);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ApiResponse<TenantRegistrationResponse>>(createdResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(request.OrganizationName, response.Data.OrganizationName);
        Assert.Equal("Free", response.Data.SubscriptionTier);
        Assert.NotNull(response.Data.TrialExpiryDate);

        // Verify tenant was created in database
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);
        Assert.NotNull(tenant);
        Assert.Equal(request.OrganizationName, tenant.OrganizationName);
    }

    [Fact]
    public async Task Register_WithExistingTenant_ReturnsConflict()
    {
        // Arrange
        var tenantId = "existing-tenant-id";
        var existingTenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Existing Org",
            PrimaryAdminEmail = "admin@existing.com",
            Status = "Active"
        };
        _context.Tenants.Add(existingTenant);
        await _context.SaveChangesAsync();

        var request = new TenantRegistrationRequest
        {
            OrganizationName = "New Organization",
            PrimaryAdminEmail = "admin@neworg.com"
        };

        SetupControllerContext(tenantId, "user-id", request.PrimaryAdminEmail);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Equal("TENANT_ALREADY_EXISTS", response.Error?.Code);
    }

    [Fact]
    public async Task Register_WithMissingOrganizationName_ReturnsBadRequest()
    {
        // Arrange
        var request = new TenantRegistrationRequest
        {
            OrganizationName = "", // Empty
            PrimaryAdminEmail = "admin@org.com"
        };

        SetupControllerContext("tenant-id", "user-id", "user@example.com");

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("VALIDATION_ERROR", response.Error?.Code);
    }

    [Fact]
    public async Task Register_WithMissingAdminEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new TenantRegistrationRequest
        {
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "" // Empty
        };

        SetupControllerContext("tenant-id", "user-id", "user@example.com");

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("VALIDATION_ERROR", response.Error?.Code);
    }

    [Fact]
    public async Task Register_CreatesSubscriptionWith30DayTrial()
    {
        // Arrange
        var tenantId = "trial-tenant-id";
        var request = new TenantRegistrationRequest
        {
            OrganizationName = "Trial Organization",
            PrimaryAdminEmail = "admin@trial.com"
        };

        SetupControllerContext(tenantId, "user-id", request.PrimaryAdminEmail);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ApiResponse<TenantRegistrationResponse>>(createdResult.Value);
        
        // Verify trial expiry is approximately 30 days from now
        Assert.NotNull(response.Data.TrialExpiryDate);
        var expectedTrialEnd = DateTime.UtcNow.AddDays(30);
        var actualTrialEnd = response.Data.TrialExpiryDate.Value;
        
        // Allow 1 minute tolerance for test execution time
        Assert.True(Math.Abs((actualTrialEnd - expectedTrialEnd).TotalMinutes) < 1);
    }

    private void SetupControllerContext(string? tenantId, string? userId, string? upn)
    {
        var claims = new List<Claim>();
        
        if (tenantId != null)
            claims.Add(new Claim("tid", tenantId));
        if (userId != null)
            claims.Add(new Claim("oid", userId));
        if (upn != null)
            claims.Add(new Claim("upn", upn));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

/// <summary>
/// Mock audit log service for testing
/// </summary>
internal class MockAuditLogService : IAuditLogService
{
    public Task LogActionAsync(int tenantId, string userId, string userEmail, string actionType,
        string entityType, string entityId, string description, string? ipAddress,
        string? correlationId, string result)
    {
        return Task.CompletedTask;
    }
}

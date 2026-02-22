using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;
using Moq;
using SharePointExternalUserManager.Api.Controllers;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;
using Stripe;
using ApiSubscriptionStatus = SharePointExternalUserManager.Api.Controllers.SubscriptionStatus;

namespace SharePointExternalUserManager.Api.Tests.Controllers;

public class SubscriptionControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStripeService> _mockStripeService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly SubscriptionController _controller;

    public SubscriptionControllerTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"SubscriptionTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _mockStripeService = new Mock<IStripeService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        
        _controller = new SubscriptionController(
            _context,
            _mockStripeService.Object,
            _mockAuditLogService.Object,
            new NullLogger<SubscriptionController>());
    }

    private void SetupControllerContext(string tenantId, string userId, string email)
    {
        var claims = new List<Claim>
        {
            new Claim("http://schemas.microsoft.com/identity/claims/tenantid", tenantId),
            new Claim("tid", tenantId),
            new Claim("oid", userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("upn", email),
            new Claim("email", email),
            new Claim(ClaimTypes.Email, email)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext 
            { 
                User = user,
                Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
            }
        };
    }

    [Fact]
    public async Task GetMySubscription_WithActiveSubscription_ReturnsSubscriptionDetails()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        SetupControllerContext(tenantId, "user-id", "test@example.com");

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "test@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Professional",
            Status = ApiSubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            StripeSubscriptionId = "sub_test123",
            StripeCustomerId = "cus_test123"
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMySubscription();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SubscriptionStatusResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("Professional", response.Data.Tier);
        Assert.Equal(ApiSubscriptionStatus.Active, response.Data.Status);
        Assert.True(response.Data.IsActive);
    }

    [Fact]
    public async Task GetMySubscription_WithNoSubscription_ReturnsDefaultStarterPlan()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        SetupControllerContext(tenantId, "user-id", "test@example.com");

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "test@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetMySubscription();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SubscriptionStatusResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Starter", response.Data.Tier);
        Assert.Equal(ApiSubscriptionStatus.None, response.Data.Status);
        Assert.False(response.Data.IsActive);
    }

    [Fact]
    public async Task GetMySubscription_WithMissingTenantId_ReturnsUnauthorized()
    {
        // Arrange - no tenant ID in claims
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.GetMySubscription();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task ChangePlan_ToHigherTier_UpdatesSubscription()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "user-id";
        var email = "test@example.com";
        SetupControllerContext(tenantId, userId, email);

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = email,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        // Trial subscription with no Stripe ID
        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Starter",
            Status = ApiSubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow.AddDays(-5),
            StripeSubscriptionId = null,
            StripeCustomerId = null
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var request = new ChangePlanRequest { NewPlanTier = "Professional" };

        // Act
        var result = await _controller.ChangePlan(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);

        var updatedSubscription = await _context.Subscriptions.FindAsync(subscription.Id);
        Assert.Equal("Professional", updatedSubscription.Tier);
        
        _mockAuditLogService.Verify(x => x.LogActionAsync(
            tenant.Id,
            userId,
            email,
            "PLAN_CHANGED",
            "Subscription",
            subscription.Id.ToString(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            "Success"), Times.Once);
    }

    [Fact]
    public async Task ChangePlan_ToSameTier_ReturnsBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        SetupControllerContext(tenantId, "user-id", "test@example.com");

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "test@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Professional",
            Status = ApiSubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30)
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var request = new ChangePlanRequest { NewPlanTier = "Professional" };

        // Act
        var result = await _controller.ChangePlan(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("ALREADY_ON_PLAN", response.Error.Code);
    }

    [Fact]
    public async Task ChangePlan_ToEnterprise_ReturnsBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        SetupControllerContext(tenantId, "user-id", "test@example.com");

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "test@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Professional",
            Status = ApiSubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30)
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var request = new ChangePlanRequest { NewPlanTier = "Enterprise" };

        // Act
        var result = await _controller.ChangePlan(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("ENTERPRISE_REQUIRES_SALES", response.Error.Code);
    }

    [Fact]
    public async Task ChangePlan_WithStripeSubscription_ReturnsBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        SetupControllerContext(tenantId, "user-id", "test@example.com");

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "test@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Starter",
            Status = ApiSubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            StripeSubscriptionId = "sub_test123"
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var request = new ChangePlanRequest { NewPlanTier = "Professional" };

        // Act
        var result = await _controller.ChangePlan(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("USE_CHECKOUT", response.Error.Code);
    }

    [Fact]
    public async Task CancelSubscription_WithActiveSubscription_CancelsSuccessfully()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "user-id";
        var email = "test@example.com";
        SetupControllerContext(tenantId, userId, email);

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = email,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Professional",
            Status = ApiSubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            StripeSubscriptionId = "sub_test123"
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _mockStripeService
            .Setup(x => x.CancelSubscriptionAsync("sub_test123"))
            .ReturnsAsync(new Subscription { Id = "sub_test123", Status = "canceled" });

        // Act
        var result = await _controller.CancelSubscription();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);

        var cancelledSubscription = await _context.Subscriptions.FindAsync(subscription.Id);
        Assert.NotNull(cancelledSubscription);
        Assert.Equal(ApiSubscriptionStatus.Cancelled, cancelledSubscription.Status);
        Assert.NotNull(cancelledSubscription.EndDate);
        Assert.NotNull(cancelledSubscription.GracePeriodEnd);
        
        _mockStripeService.Verify(x => x.CancelSubscriptionAsync("sub_test123"), Times.Once);
        _mockAuditLogService.Verify(x => x.LogActionAsync(
            tenant.Id,
            userId,
            email,
            "SUBSCRIPTION_CANCELLED",
            "Subscription",
            subscription.Id.ToString(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            "Success"), Times.Once);
    }

    [Fact]
    public async Task CancelSubscription_WithNoActiveSubscription_ReturnsBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        SetupControllerContext(tenantId, "user-id", "test@example.com");

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "test@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.CancelSubscription();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("NO_ACTIVE_SUBSCRIPTION", response.Error.Code);
    }

    [Fact]
    public async Task CancelSubscription_WithoutStripeId_CancelsLocallyOnly()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "user-id";
        var email = "test@example.com";
        SetupControllerContext(tenantId, userId, email);

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = email,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        // Trial subscription without Stripe
        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Starter",
            Status = ApiSubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow.AddDays(-5),
            StripeSubscriptionId = null
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.CancelSubscription();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);

        var cancelledSubscription = await _context.Subscriptions.FindAsync(subscription.Id);
        Assert.NotNull(cancelledSubscription);
        Assert.Equal(ApiSubscriptionStatus.Cancelled, cancelledSubscription.Status);
        
        // Should not call Stripe service
        _mockStripeService.Verify(x => x.CancelSubscriptionAsync(It.IsAny<string>()), Times.Never);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

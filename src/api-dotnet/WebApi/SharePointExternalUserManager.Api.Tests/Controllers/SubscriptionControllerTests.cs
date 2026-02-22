using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using SharePointExternalUserManager.Api.Controllers;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;
using System.Security.Claims;
using Stripe;
using ApiSubscriptionStatus = SharePointExternalUserManager.Api.Controllers.SubscriptionStatus;

namespace SharePointExternalUserManager.Api.Tests.Controllers;

public class SubscriptionControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStripeService> _mockStripeService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly IConfiguration _configuration;
    private readonly SubscriptionController _controller;

    public SubscriptionControllerTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);

        // Mock services
        _mockStripeService = new Mock<IStripeService>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        // Mock configuration with Stripe price IDs
        var configData = new Dictionary<string, string?>
        {
            { "Stripe:Price:Starter:Monthly", "price_starter_monthly" },
            { "Stripe:Price:Starter:Annual", "price_starter_annual" },
            { "Stripe:Price:Professional:Monthly", "price_professional_monthly" },
            { "Stripe:Price:Professional:Annual", "price_professional_annual" },
            { "Stripe:Price:Business:Monthly", "price_business_monthly" },
            { "Stripe:Price:Business:Annual", "price_business_annual" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _controller = new SubscriptionController(
            _context,
            _mockStripeService.Object,
            _mockAuditLogService.Object,
            _configuration,
            new NullLogger<SubscriptionController>());

        // Setup HTTP context with authenticated user
        SetupAuthenticatedUser("test-tenant-id", "test-user@example.com");
    }

    private void SetupAuthenticatedUser(string tenantId, string email)
    {
        var claims = new List<Claim>
        {
            new Claim("http://schemas.microsoft.com/identity/claims/tenantid", tenantId),
            new Claim("tid", tenantId),
            new Claim("upn", email),
            new Claim("email", email),
            new Claim("oid", "test-user-id"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Email, email)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal,
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("api.example.com")
                },
                Connection =
                {
                    RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1")
                }
            }
        };
    }

    [Fact]
    public async Task GetMySubscription_WithoutSubscription_ReturnsDefaultPlan()
    {
        // Arrange
        await SeedTestTenant();

        // Act
        var result = await _controller.GetMySubscription();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SubscriptionStatusResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("Starter", response.Data.Tier);
        Assert.Equal(ApiSubscriptionStatus.None, response.Data.Status);
        Assert.False(response.Data.IsActive);
    }

    [Fact]
    public async Task GetMySubscription_WithActiveSubscription_ReturnsSubscriptionDetails()
    {
        // Arrange
        var tenant = await SeedTestTenant();
        await SeedTestSubscription(tenant.Id, "Professional", ApiSubscriptionStatus.Active);

        // Act
        var result = await _controller.GetMySubscription();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SubscriptionStatusResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Professional", response.Data.Tier);
        Assert.Equal(ApiSubscriptionStatus.Active, response.Data.Status);
        Assert.True(response.Data.IsActive);
    }

    [Fact]
    public async Task ChangePlan_WithoutActiveSubscription_ReturnsBadRequest()
    {
        // Arrange
        await SeedTestTenant();
        
        var request = new ChangePlanRequest
        {
            NewPlanTier = "Professional"
        };

        // Act
        var result = await _controller.ChangePlan(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("NO_ACTIVE_SUBSCRIPTION", response.Error.Code);
    }

    [Fact]
    public async Task ChangePlan_ToEnterprise_ReturnsBadRequest()
    {
        // Arrange
        var tenant = await SeedTestTenant();
        await SeedTestSubscription(tenant.Id, "Starter", ApiSubscriptionStatus.Active);
        
        var request = new ChangePlanRequest
        {
            NewPlanTier = "Enterprise"
        };

        // Act
        var result = await _controller.ChangePlan(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("ENTERPRISE_REQUIRES_SALES", response.Error.Code);
    }

    [Fact]
    public async Task ChangePlan_ToSamePlan_ReturnsBadRequest()
    {
        // Arrange
        var tenant = await SeedTestTenant();
        await SeedTestSubscription(tenant.Id, "Professional", ApiSubscriptionStatus.Active);
        
        var request = new ChangePlanRequest
        {
            NewPlanTier = "Professional"
        };

        // Act
        var result = await _controller.ChangePlan(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("ALREADY_ON_PLAN", response.Error.Code);
    }

    [Fact]
    public async Task ChangePlan_WithoutStripeSubscription_UpdatesLocally()
    {
        // Arrange
        var tenant = await SeedTestTenant();
        await SeedTestSubscription(tenant.Id, "Starter", ApiSubscriptionStatus.Trial);
        
        var request = new ChangePlanRequest
        {
            NewPlanTier = "Professional"
        };

        // Act
        var result = await _controller.ChangePlan(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);

        // Verify subscription was updated in database
        var updatedSub = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id);
        Assert.NotNull(updatedSub);
        Assert.Equal("Professional", updatedSub.Tier);
    }

    [Fact]
    public async Task ChangePlan_WithStripeSubscription_UpdatesViaStripe()
    {
        // Arrange
        var tenant = await SeedTestTenant();
        var subscription = await SeedTestSubscription(
            tenant.Id, 
            "Starter", 
            ApiSubscriptionStatus.Active,
            "cus_test123",
            "sub_test123");
        
        var request = new ChangePlanRequest
        {
            NewPlanTier = "Professional"
        };

        // Mock Stripe subscription with monthly pricing
        var mockStripeSubscription = new Subscription
        {
            Id = "sub_test123",
            Items = new StripeList<SubscriptionItem>
            {
                Data = new List<SubscriptionItem>
                {
                    new SubscriptionItem
                    {
                        Id = "si_test123",
                        Price = new Price
                        {
                            Id = "price_starter_monthly",
                            Recurring = new PriceRecurring
                            {
                                Interval = "month"
                            }
                        }
                    }
                }
            }
        };

        _mockStripeService
            .Setup(s => s.GetSubscriptionAsync("sub_test123"))
            .ReturnsAsync(mockStripeSubscription);

        _mockStripeService
            .Setup(s => s.UpdateSubscriptionAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockStripeSubscription);

        // Act
        var result = await _controller.ChangePlan(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);

        // Verify Stripe service was called
        _mockStripeService.Verify(s => s.UpdateSubscriptionAsync(
            "sub_test123",
            "price_professional_monthly"), Times.Once);
    }

    [Fact]
    public async Task CancelSubscription_WithoutActiveSubscription_ReturnsBadRequest()
    {
        // Arrange
        await SeedTestTenant();

        // Act
        var result = await _controller.CancelSubscription();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("NO_ACTIVE_SUBSCRIPTION", response.Error.Code);
    }

    [Fact]
    public async Task CancelSubscription_WithStripeSubscription_CancelsViaStripe()
    {
        // Arrange
        var tenant = await SeedTestTenant();
        await SeedTestSubscription(
            tenant.Id,
            "Professional",
            ApiSubscriptionStatus.Active,
            "cus_test123",
            "sub_test123");

        var mockCancelledSubscription = new Subscription
        {
            Id = "sub_test123",
            Status = "canceled"
        };

        _mockStripeService
            .Setup(s => s.CancelSubscriptionAsync("sub_test123"))
            .ReturnsAsync(mockCancelledSubscription);

        // Act
        var result = await _controller.CancelSubscription();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);

        // Verify Stripe service was called
        _mockStripeService.Verify(s => s.CancelSubscriptionAsync("sub_test123"), Times.Once);

        // Verify subscription was updated in database
        var updatedSub = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id);
        Assert.NotNull(updatedSub);
        Assert.Equal(ApiSubscriptionStatus.Cancelled, updatedSub.Status);
        Assert.NotNull(updatedSub.GracePeriodEnd);
    }

    [Fact]
    public async Task CancelSubscription_WithoutStripeSubscription_UpdatesLocally()
    {
        // Arrange
        var tenant = await SeedTestTenant();
        await SeedTestSubscription(tenant.Id, "Starter", ApiSubscriptionStatus.Trial);

        // Act
        var result = await _controller.CancelSubscription();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);

        // Verify subscription was updated in database
        var updatedSub = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id);
        Assert.NotNull(updatedSub);
        Assert.Equal(ApiSubscriptionStatus.Cancelled, updatedSub.Status);
    }

    // Helper methods
    private async Task<TenantEntity> SeedTestTenant()
    {
        var tenant = new TenantEntity
        {
            EntraIdTenantId = "test-tenant-id",
            OrganizationName = "Test Organization",
            PrimaryAdminEmail = "admin@test.com",
            Status = "Active",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }

    private async Task<SubscriptionEntity> SeedTestSubscription(
        int tenantId,
        string tier,
        string status,
        string? stripeCustomerId = null,
        string? stripeSubscriptionId = null)
    {
        var subscription = new SubscriptionEntity
        {
            TenantId = tenantId,
            Tier = tier,
            Status = status,
            StartDate = DateTime.UtcNow,
            StripeCustomerId = stripeCustomerId,
            StripeSubscriptionId = stripeSubscriptionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

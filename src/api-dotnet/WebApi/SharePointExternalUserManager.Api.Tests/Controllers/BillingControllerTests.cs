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
using System.Security.Claims;
using Stripe;
using Stripe.Checkout;

namespace SharePointExternalUserManager.Api.Tests.Controllers;

public class BillingControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IStripeService> _mockStripeService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly BillingController _controller;

    public BillingControllerTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);

        // Mock services
        _mockStripeService = new Mock<IStripeService>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _controller = new BillingController(
            _context,
            _mockStripeService.Object,
            _mockAuditLogService.Object,
            new NullLogger<BillingController>());

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
    public void GetPlans_ReturnsAvailablePlans()
    {
        // Act
        var result = _controller.GetPlans(false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PlansResponse>(okResult.Value);
        Assert.NotNull(response.Plans);
        Assert.NotEmpty(response.Plans);
        Assert.Contains(response.Plans, p => p.Tier == SubscriptionTier.Starter);
        Assert.Contains(response.Plans, p => p.Tier == SubscriptionTier.Professional);
        Assert.Contains(response.Plans, p => p.Tier == SubscriptionTier.Business);
    }

    [Fact]
    public void GetPlans_WithEnterpriseFlag_IncludesEnterprise()
    {
        // Act
        var result = _controller.GetPlans(true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PlansResponse>(okResult.Value);
        Assert.Contains(response.Plans, p => p.Tier == SubscriptionTier.Enterprise);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithoutTenant_ReturnsForbidden()
    {
        // Arrange
        var request = new CreateCheckoutSessionRequest
        {
            PlanTier = SubscriptionTier.Starter,
            IsAnnual = false,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        // Act
        var result = await _controller.CreateCheckoutSession(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithEnterpriseTier_ReturnsBadRequest()
    {
        // Arrange
        await SeedTestTenant();
        
        var request = new CreateCheckoutSessionRequest
        {
            PlanTier = SubscriptionTier.Enterprise,
            IsAnnual = false,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        // Act
        var result = await _controller.CreateCheckoutSession(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithValidRequest_CreatesSession()
    {
        // Arrange
        await SeedTestTenant();
        
        var request = new CreateCheckoutSessionRequest
        {
            PlanTier = SubscriptionTier.Starter,
            IsAnnual = false,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        var mockSession = new Session
        {
            Id = "sess_test123",
            Url = "https://checkout.stripe.com/session/sess_test123"
        };

        _mockStripeService
            .Setup(s => s.CreateCheckoutSessionAsync(
                It.IsAny<string>(),
                It.IsAny<SubscriptionTier>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockSession);

        // Act
        var result = await _controller.CreateCheckoutSession(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CreateCheckoutSessionResponse>(okResult.Value);
        Assert.Equal(mockSession.Id, response.SessionId);
        Assert.Equal(mockSession.Url, response.CheckoutUrl);

        // Verify Stripe service was called
        _mockStripeService.Verify(s => s.CreateCheckoutSessionAsync(
            "test-tenant-id",
            SubscriptionTier.Starter,
            false,
            request.SuccessUrl,
            request.CancelUrl), Times.Once);
    }

    [Fact]
    public async Task GetSubscriptionStatus_WithoutSubscription_ReturnsDefaultPlan()
    {
        // Arrange
        await SeedTestTenant();

        // Act
        var result = await _controller.GetSubscriptionStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SubscriptionStatusResponse>(okResult.Value);
        Assert.Equal("Starter", response.Tier);
        Assert.Equal("None", response.Status);
        Assert.False(response.IsActive);
    }

    [Fact]
    public async Task GetSubscriptionStatus_WithActiveSubscription_ReturnsSubscriptionDetails()
    {
        // Arrange
        var tenant = await SeedTestTenant();
        await SeedTestSubscription(tenant.Id, "Professional", "Active");

        // Act
        var result = await _controller.GetSubscriptionStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SubscriptionStatusResponse>(okResult.Value);
        Assert.Equal("Professional", response.Tier);
        Assert.Equal("Active", response.Status);
        Assert.True(response.IsActive);
        Assert.NotNull(response.Limits);
        Assert.NotNull(response.Features);
    }

    [Fact]
    public async Task CreateCustomerPortal_WithoutStripeCustomer_ReturnsBadRequest()
    {
        // Arrange
        await SeedTestTenant();
        
        var request = new CustomerPortalRequest
        {
            ReturnUrl = "https://example.com/return"
        };

        // Act
        var result = await _controller.CreateCustomerPortal(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateCustomerPortal_WithStripeCustomer_CreatesPortalSession()
    {
        // Arrange
        var tenant = await SeedTestTenant();
        await SeedTestSubscription(tenant.Id, "Professional", "Active", "cus_test123", "sub_test123");
        
        var request = new CustomerPortalRequest
        {
            ReturnUrl = "https://example.com/return"
        };

        var mockPortalSession = new Stripe.BillingPortal.Session
        {
            Id = "bps_test123",
            Url = "https://billing.stripe.com/session/bps_test123"
        };

        _mockStripeService
            .Setup(s => s.CreateCustomerPortalSessionAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockPortalSession);

        // Act
        var result = await _controller.CreateCustomerPortal(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CustomerPortalResponse>(okResult.Value);
        Assert.Equal(mockPortalSession.Url, response.PortalUrl);

        // Verify Stripe service was called
        _mockStripeService.Verify(s => s.CreateCustomerPortalSessionAsync(
            "cus_test123",
            request.ReturnUrl), Times.Once);
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

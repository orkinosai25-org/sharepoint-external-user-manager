using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using System.Text;
using Xunit;
using Moq;
using SharePointExternalUserManager.Api.Controllers;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
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
            .UseInMemoryDatabase(databaseName: $"BillingTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _mockStripeService = new Mock<IStripeService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        
        _controller = new BillingController(
            _context,
            _mockStripeService.Object,
            _mockAuditLogService.Object,
            new NullLogger<BillingController>());
    }

    private void SetupControllerContext(string tenantId, string userId, string email)
    {
        var claims = new List<Claim>
        {
            new Claim("http://schemas.microsoft.com/identity/claims/tenantid", tenantId),
            new Claim("tid", tenantId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("oid", userId),
            new Claim(ClaimTypes.Email, email),
            new Claim("upn", email)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public void GetPlans_ReturnsAvailablePlans()
    {
        // Act
        var result = _controller.GetPlans(includeEnterprise: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PlansResponse>(okResult.Value);
        Assert.NotNull(response.Plans);
        Assert.NotEmpty(response.Plans);
        Assert.DoesNotContain(response.Plans, p => p.Tier == SubscriptionTier.Enterprise);
    }

    [Fact]
    public void GetPlans_WithIncludeEnterprise_ReturnsAllPlans()
    {
        // Act
        var result = _controller.GetPlans(includeEnterprise: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PlansResponse>(okResult.Value);
        Assert.NotNull(response.Plans);
        Assert.Contains(response.Plans, p => p.Tier == SubscriptionTier.Enterprise);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithValidRequest_ReturnsSessionUrl()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var email = "test@example.com";
        
        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = email,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        SetupControllerContext(tenantId, userId, email);

        var request = new CreateCheckoutSessionRequest
        {
            PlanTier = SubscriptionTier.Professional,
            IsAnnual = false,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        var mockSession = new Session
        {
            Id = "sess_test123",
            Url = "https://checkout.stripe.com/test"
        };

        _mockStripeService
            .Setup(x => x.CreateCheckoutSessionAsync(
                tenantId,
                SubscriptionTier.Professional,
                false,
                request.SuccessUrl,
                request.CancelUrl))
            .ReturnsAsync(mockSession);

        // Act
        var result = await _controller.CreateCheckoutSession(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CreateCheckoutSessionResponse>(okResult.Value);
        Assert.Equal(mockSession.Id, response.SessionId);
        Assert.Equal(mockSession.Url, response.CheckoutUrl);
        
        _mockAuditLogService.Verify(x => x.LogActionAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            "CreateCheckoutSession",
            "Billing",
            mockSession.Id,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            "Success"), Times.Once);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithEnterprisePlan_ReturnsBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        SetupControllerContext(tenantId, "user-id", "test@example.com");

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
        var errorResponse = badRequestResult.Value;
        Assert.NotNull(errorResponse);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithMissingTenantId_ReturnsUnauthorized()
    {
        // Arrange - no tenant ID in claims
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var request = new CreateCheckoutSessionRequest
        {
            PlanTier = SubscriptionTier.Professional,
            IsAnnual = false,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        // Act
        var result = await _controller.CreateCheckoutSession(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSubscriptionStatus_WithActiveTenant_ReturnsSubscription()
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
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-30),
            StripeSubscriptionId = "sub_test123",
            StripeCustomerId = "cus_test123"
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSubscriptionStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SubscriptionStatusResponse>(okResult.Value);
        Assert.Equal("Professional", response.Tier);
        Assert.Equal("Active", response.Status);
        Assert.True(response.IsActive);
        Assert.Equal("sub_test123", response.StripeSubscriptionId);
    }

    [Fact]
    public async Task GetSubscriptionStatus_WithNoSubscription_ReturnsStarterPlan()
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
        var result = await _controller.GetSubscriptionStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SubscriptionStatusResponse>(okResult.Value);
        Assert.Equal("Starter", response.Tier);
        Assert.Equal("None", response.Status);
        Assert.False(response.IsActive);
    }

    [Fact]
    public async Task StripeWebhook_WithoutSignature_ReturnsBadRequest()
    {
        // Arrange
        var json = "{\"type\": \"test.event\"}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Body = stream }
            }
        };

        // Act
        var result = await _controller.StripeWebhook();

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StripeWebhook_WithInvalidSignature_ReturnsBadRequest()
    {
        // Arrange
        var json = "{\"type\": \"test.event\"}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = 
                { 
                    Body = stream,
                    Headers = { ["Stripe-Signature"] = "invalid_signature" }
                }
            }
        };

        _mockStripeService
            .Setup(x => x.VerifyWebhookSignature(json, "invalid_signature"))
            .Returns((Event?)null);

        // Act
        var result = await _controller.StripeWebhook();

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StripeWebhook_CheckoutSessionCompleted_CreatesSubscription()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "test@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var sessionData = new Session
        {
            Id = "sess_test123",
            SubscriptionId = "sub_test123",
            CustomerId = "cus_test123",
            Metadata = new Dictionary<string, string>
            {
                { "tenant_id", tenantId },
                { "plan_tier", "Professional" }
            }
        };

        var stripeEvent = new Event
        {
            Id = "evt_test123",
            Type = "checkout.session.completed",
            Data = new EventData { Object = sessionData }
        };

        var json = "{\"type\": \"checkout.session.completed\"}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = 
                { 
                    Body = stream,
                    Headers = { ["Stripe-Signature"] = "valid_signature" }
                }
            }
        };

        _mockStripeService
            .Setup(x => x.VerifyWebhookSignature(json, "valid_signature"))
            .Returns(stripeEvent);

        // Act
        var result = await _controller.StripeWebhook();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == "sub_test123");
        
        Assert.NotNull(subscription);
        Assert.Equal("Professional", subscription.Tier);
        Assert.Equal("Active", subscription.Status);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

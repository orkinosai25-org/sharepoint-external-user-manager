using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SharePointExternalUserManager.Api.Controllers;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using Xunit;

namespace SharePointExternalUserManager.Api.Tests.Controllers;

public class AiAssistantControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<AiAssistantController>> _mockLogger;
    private readonly AiAssistantController _controller;

    public AiAssistantControllerTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);

        // Create mocks
        _mockLogger = new Mock<ILogger<AiAssistantController>>();

        // Create controller with null services for tests that only use GetUsageStats
        // GetUsageStats doesn't use AiAssistantService, AiRateLimitService, or PromptTemplateService
        // If testing other endpoints, these services must be properly mocked
        _controller = new AiAssistantController(
            _context,
            null!, // AiAssistantService not used in GetUsageStats
            null!, // AiRateLimitService not used in GetUsageStats
            null!, // PromptTemplateService not used in GetUsageStats
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetUsageStats_WithValidTenant_ReturnsStatsWithMessageLimits()
    {
        // Arrange
        var tenantId = 1;
        var tenant = new TenantEntity
        {
            Id = tenantId,
            OrganizationName = "Test Tenant",
            EntraIdTenantId = Guid.NewGuid().ToString(),
            PrimaryAdminEmail = "admin@test.com"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenantId,
            Tier = "Starter",
            Status = "Active",
            StartDate = DateTime.UtcNow.AddMonths(-1)
        };
        _context.Subscriptions.Add(subscription);

        var settings = new AiSettingsEntity
        {
            TenantId = tenantId,
            IsEnabled = true,
            MaxRequestsPerHour = 100,
            MonthlyTokenBudget = 10000,
            TokensUsedThisMonth = 5000
        };
        _context.AiSettings.Add(settings);

        // Add some conversation logs
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 15; i++)
        {
            _context.AiConversationLogs.Add(new AiConversationLogEntity
            {
                TenantId = tenantId,
                ConversationId = $"conv-{i}",
                Mode = "InProduct",
                UserPrompt = "Test",
                AssistantResponse = "Response",
                TokensUsed = 100,
                ResponseTimeMs = 500,
                Timestamp = startOfMonth.AddDays(i)
            });
        }

        await _context.SaveChangesAsync();

        // Set up user claims
        var claims = new List<Claim>
        {
            new Claim("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.GetUsageStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<AiUsageStats>(okResult.Value);

        Assert.Equal(tenantId, stats.TenantId);
        Assert.Equal(15, stats.MessagesThisMonth);
        Assert.Equal(20, stats.MaxMessagesPerMonth); // Starter plan has 20 messages
        Assert.Equal(75, stats.MessageLimitUsedPercentage); // 15/20 * 100
        Assert.Equal("Starter", stats.PlanTier);
    }

    [Fact]
    public async Task GetUsageStats_WithProfessionalPlan_ReturnsCorrectLimits()
    {
        // Arrange
        var tenantId = 2;
        var tenant = new TenantEntity
        {
            Id = tenantId,
            OrganizationName = "Pro Tenant",
            EntraIdTenantId = Guid.NewGuid().ToString(),
            PrimaryAdminEmail = "admin@pro.com"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenantId,
            Tier = "Professional",
            Status = "Active",
            StartDate = DateTime.UtcNow.AddMonths(-1)
        };
        _context.Subscriptions.Add(subscription);

        await _context.SaveChangesAsync();

        // Set up user claims
        var claims = new List<Claim>
        {
            new Claim("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.GetUsageStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<AiUsageStats>(okResult.Value);

        Assert.Equal(1000, stats.MaxMessagesPerMonth); // Professional plan has 1000 messages
        Assert.Equal("Professional", stats.PlanTier);
    }

    [Fact]
    public async Task GetUsageStats_WithEnterprisePlan_ReturnsUnlimited()
    {
        // Arrange
        var tenantId = 3;
        var tenant = new TenantEntity
        {
            Id = tenantId,
            OrganizationName = "Enterprise Tenant",
            EntraIdTenantId = Guid.NewGuid().ToString(),
            PrimaryAdminEmail = "admin@enterprise.com"
        };
        _context.Tenants.Add(tenant);

        var subscription = new SubscriptionEntity
        {
            TenantId = tenantId,
            Tier = "Enterprise",
            Status = "Active",
            StartDate = DateTime.UtcNow.AddMonths(-1)
        };
        _context.Subscriptions.Add(subscription);

        await _context.SaveChangesAsync();

        // Set up user claims
        var claims = new List<Claim>
        {
            new Claim("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.GetUsageStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<AiUsageStats>(okResult.Value);

        Assert.Null(stats.MaxMessagesPerMonth); // Enterprise plan is unlimited
        Assert.Equal("Enterprise", stats.PlanTier);
    }

    [Fact]
    public async Task GetUsageStats_WithNoSubscription_ReturnsStatsWithoutLimits()
    {
        // Arrange
        var tenantId = 4;
        var tenant = new TenantEntity
        {
            Id = tenantId,
            OrganizationName = "No Subscription Tenant",
            EntraIdTenantId = Guid.NewGuid().ToString(),
            PrimaryAdminEmail = "admin@nosub.com"
        };
        _context.Tenants.Add(tenant);

        await _context.SaveChangesAsync();

        // Set up user claims
        var claims = new List<Claim>
        {
            new Claim("TenantId", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.GetUsageStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<AiUsageStats>(okResult.Value);

        Assert.Null(stats.MaxMessagesPerMonth);
        Assert.Null(stats.PlanTier);
    }

    [Fact]
    public async Task GetUsageStats_WithMissingTenantClaim_ReturnsBadRequest()
    {
        // Arrange
        var claims = new List<Claim>(); // No TenantId claim
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.GetUsageStats();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Tenant ID not found in claims", badRequestResult.Value);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

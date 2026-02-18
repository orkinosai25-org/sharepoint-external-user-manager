using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Xunit;
using SharePointExternalUserManager.Api.Attributes;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Tests.Attributes;

public class RequiresPlanAttributeTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ServiceProvider _serviceProvider;

    public RequiresPlanAttributeTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);

        // Setup service provider
        var services = new ServiceCollection();
        services.AddSingleton(_context);
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithSufficientTier_AllowsAccess()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        await SetupTenantWithSubscription(tenantId, "Pro", "Active");

        var attribute = new RequiresPlanAttribute("Starter");
        var context = CreateActionExecutingContext(tenantId);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithInsufficientTier_BlocksAccess()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        await SetupTenantWithSubscription(tenantId, "Free", "Active");

        var attribute = new RequiresPlanAttribute("Pro", "AI Assistant");
        var context = CreateActionExecutingContext(tenantId);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.False(nextCalled);
        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("UPGRADE_REQUIRED", response.Error.Code);
        Assert.Contains("AI Assistant", response.Error.Message);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithExpiredTrial_BlocksAccess()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "admin@test.com",
            Status = "Active",
            CreatedDate = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Pro",
            Status = "Trial",
            TrialExpiry = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            CreatedDate = DateTime.UtcNow
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var attribute = new RequiresPlanAttribute("Pro");
        var context = CreateActionExecutingContext(tenantId);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.False(nextCalled);
        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("TRIAL_EXPIRED", response.Error.Code);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithInactiveSubscription_BlocksAccess()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        await SetupTenantWithSubscription(tenantId, "Pro", "Cancelled");

        var attribute = new RequiresPlanAttribute("Pro");
        var context = CreateActionExecutingContext(tenantId);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.False(nextCalled);
        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("SUBSCRIPTION_INACTIVE", response.Error.Code);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithMissingTenantClaim_ReturnsUnauthorized()
    {
        // Arrange
        var attribute = new RequiresPlanAttribute("Pro");
        var context = CreateActionExecutingContext(null); // No tenant claim
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.False(nextCalled);
        Assert.NotNull(context.Result);
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(context.Result);

        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("AUTH_ERROR", response.Error.Code);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithNonExistentTenant_ReturnsNotFound()
    {
        // Arrange
        var attribute = new RequiresPlanAttribute("Pro");
        var context = CreateActionExecutingContext("non-existent-tenant");
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.False(nextCalled);
        Assert.NotNull(context.Result);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(context.Result);

        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("TENANT_NOT_FOUND", response.Error.Code);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithActiveTrial_AllowsAccess()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "admin@test.com",
            Status = "Active",
            CreatedDate = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Pro",
            Status = "Trial",
            TrialExpiry = DateTime.UtcNow.AddDays(10), // Active trial
            CreatedDate = DateTime.UtcNow
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var attribute = new RequiresPlanAttribute("Pro");
        var context = CreateActionExecutingContext(tenantId);
        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    private async Task SetupTenantWithSubscription(string tenantId, string tier, string status)
    {
        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "admin@test.com",
            Status = "Active",
            CreatedDate = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = tier,
            Status = status,
            CreatedDate = DateTime.UtcNow
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
    }

    private ActionExecutingContext CreateActionExecutingContext(string? tenantId)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };

        if (!string.IsNullOrEmpty(tenantId))
        {
            var claims = new List<Claim>
            {
                new Claim("tid", tenantId),
                new Claim("oid", "test-user-id")
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private ActionExecutedContext CreateActionExecutedContext(ActionExecutingContext executingContext)
    {
        return new ActionExecutedContext(
            executingContext,
            new List<IFilterMetadata>(),
            new object());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}

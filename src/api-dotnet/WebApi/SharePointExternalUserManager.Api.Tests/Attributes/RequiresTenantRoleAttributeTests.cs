using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharePointExternalUserManager.Api.Attributes;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using Xunit;

namespace SharePointExternalUserManager.Api.Tests.Attributes;

public class RequiresTenantRoleAttributeTests
{
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

    public RequiresTenantRoleAttributeTests()
    {
        // Use in-memory database for testing
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
    }

    private ApplicationDbContext CreateDbContext()
    {
        var context = new ApplicationDbContext(_dbContextOptions);
        context.Database.EnsureCreated();
        return context;
    }

    private ActionExecutingContext CreateActionExecutingContext(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        var httpContext = new DefaultHttpContext
        {
            User = user
        };

        // Setup service provider with DbContext
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(dbContext);
        serviceCollection.AddLogging();
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

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

    [Fact]
    public async Task OnActionExecutionAsync_AllowsOwner_WhenOwnerRoleRequired()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "admin@test.com"
        };
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var tenantUser = new TenantUserEntity
        {
            TenantId = tenant.Id,
            EntraIdUserId = userId,
            Email = "owner@test.com",
            Role = TenantRole.Owner,
            IsActive = true
        };
        dbContext.TenantUsers.Add(tenantUser);
        await dbContext.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId)
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var attribute = new RequiresTenantRoleAttribute(TenantRole.Owner, TenantRole.Admin);
        var context = CreateActionExecutingContext(user, dbContext);

        var executedNext = false;
        ActionExecutionDelegate next = () =>
        {
            executedNext = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                new object()));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(executedNext);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_DeniesViewer_WhenAdminRoleRequired()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "admin@test.com"
        };
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var tenantUser = new TenantUserEntity
        {
            TenantId = tenant.Id,
            EntraIdUserId = userId,
            Email = "viewer@test.com",
            Role = TenantRole.Viewer,
            IsActive = true
        };
        dbContext.TenantUsers.Add(tenantUser);
        await dbContext.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId)
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var attribute = new RequiresTenantRoleAttribute("Test Operation", TenantRole.Owner, TenantRole.Admin);
        var context = CreateActionExecutingContext(user, dbContext);

        var executedNext = false;
        ActionExecutionDelegate next = () =>
        {
            executedNext = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                new object()));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.False(executedNext);
        Assert.NotNull(context.Result);
        
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_GrantsOwnerRole_ToPrimaryAdmin()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var adminEmail = "admin@test.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = adminEmail
        };
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        // No explicit TenantUser record for primary admin

        var claims = new List<Claim>
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId),
            new Claim("upn", adminEmail)
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var attribute = new RequiresTenantRoleAttribute(TenantRole.Owner);
        var context = CreateActionExecutingContext(user, dbContext);

        var executedNext = false;
        ActionExecutionDelegate next = () =>
        {
            executedNext = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                new object()));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(executedNext);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ReturnsUnauthorized_WhenMissingTenantClaim()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var userId = "test-user-id";

        var claims = new List<Claim>
        {
            new Claim("oid", userId)
            // Missing tid claim
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var attribute = new RequiresTenantRoleAttribute(TenantRole.Viewer);
        var context = CreateActionExecutingContext(user, dbContext);

        var executedNext = false;
        ActionExecutionDelegate next = () =>
        {
            executedNext = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                new List<IFilterMetadata>(),
                new object()));
        };

        // Act
        await attribute.OnActionExecutionAsync(context, next);

        // Assert
        Assert.False(executedNext);
        Assert.NotNull(context.Result);
        
        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
    }
}

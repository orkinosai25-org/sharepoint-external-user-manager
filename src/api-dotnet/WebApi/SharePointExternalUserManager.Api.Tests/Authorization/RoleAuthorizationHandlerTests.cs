using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;
using SharePointExternalUserManager.Api.Authorization;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Tests.Authorization;

public class RoleAuthorizationHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RoleAuthorizationHandler _handler;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RoleAuthorizationHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _httpContextAccessor = new HttpContextAccessor();
        _handler = new RoleAuthorizationHandler(
            _httpContextAccessor,
            _context,
            new NullLogger<RoleAuthorizationHandler>());
    }

    [Fact]
    public async Task HandleRequirementAsync_PrimaryAdmin_Succeeds()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var email = "admin@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = email,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId),
            new Claim("email", email)
        }, "TestAuth"));

        var httpContext = new DefaultHttpContext { User = user };
        _httpContextAccessor.HttpContext = httpContext;

        var requirement = new RoleRequirement("TenantOwner", "TenantAdmin");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_TenantAdminRole_Succeeds()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var email = "admin@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "other@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var tenantUser = new TenantUserEntity
        {
            TenantId = tenant.Id,
            UserId = userId,
            Email = email,
            Role = UserRole.TenantAdmin,
            CreatedDate = DateTime.UtcNow
        };
        _context.TenantUsers.Add(tenantUser);
        await _context.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId),
            new Claim("email", email)
        }, "TestAuth"));

        var httpContext = new DefaultHttpContext { User = user };
        _httpContextAccessor.HttpContext = httpContext;

        var requirement = new RoleRequirement("TenantOwner", "TenantAdmin");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_ViewerRole_Fails_WhenAdminRequired()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var email = "viewer@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "admin@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var tenantUser = new TenantUserEntity
        {
            TenantId = tenant.Id,
            UserId = userId,
            Email = email,
            Role = UserRole.Viewer,
            CreatedDate = DateTime.UtcNow
        };
        _context.TenantUsers.Add(tenantUser);
        await _context.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId),
            new Claim("email", email)
        }, "TestAuth"));

        var httpContext = new DefaultHttpContext { User = user };
        _httpContextAccessor.HttpContext = httpContext;

        var requirement = new RoleRequirement("TenantOwner", "TenantAdmin");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_ViewerRole_Succeeds_WhenViewerAllowed()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var email = "viewer@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "admin@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var tenantUser = new TenantUserEntity
        {
            TenantId = tenant.Id,
            UserId = userId,
            Email = email,
            Role = UserRole.Viewer,
            CreatedDate = DateTime.UtcNow
        };
        _context.TenantUsers.Add(tenantUser);
        await _context.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId),
            new Claim("email", email)
        }, "TestAuth"));

        var httpContext = new DefaultHttpContext { User = user };
        _httpContextAccessor.HttpContext = httpContext;

        var requirement = new RoleRequirement("TenantOwner", "TenantAdmin", "Viewer");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_MissingTenantId_Fails()
    {
        // Arrange
        var userId = "test-user-id";
        var email = "user@example.com";

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("oid", userId),
            new Claim("email", email)
        }, "TestAuth"));

        var httpContext = new DefaultHttpContext { User = user };
        _httpContextAccessor.HttpContext = httpContext;

        var requirement = new RoleRequirement("TenantOwner");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserNotInTenant_Fails()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var email = "user@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "admin@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // User exists in token but not in TenantUsers table
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId),
            new Claim("email", email)
        }, "TestAuth"));

        var httpContext = new DefaultHttpContext { User = user };
        _httpContextAccessor.HttpContext = httpContext;

        var requirement = new RoleRequirement("TenantAdmin");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

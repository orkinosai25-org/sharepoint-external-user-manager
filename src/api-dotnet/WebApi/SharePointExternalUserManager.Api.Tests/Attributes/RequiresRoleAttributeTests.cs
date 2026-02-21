using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;
using SharePointExternalUserManager.Api.Attributes;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Tests.Attributes;

public class RequiresRoleAttributeTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ServiceCollection _services;

    public RequiresRoleAttributeTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        
        // Setup service collection
        _services = new ServiceCollection();
        _services.AddSingleton(_context);
        _services.AddLogging();
    }

    [Fact]
    public async Task OnActionExecutionAsync_MissingTenantClaim_ReturnsUnauthorized()
    {
        // Arrange
        var attribute = new RequiresRoleAttribute(TenantRole.Viewer);
        var context = CreateActionExecutingContext(null, "user-123");

        // Act
        await attribute.OnActionExecutionAsync(context, () => Task.FromResult(CreateActionExecutedContext(context)));

        // Assert
        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("AUTH_ERROR", response.Error.Code);
    }

    [Fact]
    public async Task OnActionExecutionAsync_MissingUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        var attribute = new RequiresRoleAttribute(TenantRole.Viewer);
        var context = CreateActionExecutingContext("tenant-123", null);

        // Act
        await attribute.OnActionExecutionAsync(context, () => Task.FromResult(CreateActionExecutedContext(context)));

        // Assert
        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("AUTH_ERROR", response.Error.Code);
    }

    [Fact]
    public async Task OnActionExecutionAsync_TenantNotFound_ReturnsNotFound()
    {
        // Arrange
        var attribute = new RequiresRoleAttribute(TenantRole.Viewer);
        var context = CreateActionExecutingContext("nonexistent-tenant", "user-123");

        // Act
        await attribute.OnActionExecutionAsync(context, () => Task.FromResult(CreateActionExecutedContext(context)));

        // Assert
        var result = Assert.IsType<NotFoundObjectResult>(context.Result);
        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("TENANT_NOT_FOUND", response.Error.Code);
    }

    [Fact]
    public async Task OnActionExecutionAsync_UserNotInTenant_ReturnsForbidden()
    {
        // Arrange
        var tenantId = "tenant-123";
        var userId = "user-123";
        
        // Create tenant but no user role
        await CreateTestTenantAsync(tenantId);
        
        var attribute = new RequiresRoleAttribute(TenantRole.Viewer);
        var context = CreateActionExecutingContext(tenantId, userId);

        // Act
        await attribute.OnActionExecutionAsync(context, () => Task.FromResult(CreateActionExecutedContext(context)));

        // Assert
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("ACCESS_DENIED", response.Error.Code);
    }

    [Fact]
    public async Task OnActionExecutionAsync_InsufficientRole_ReturnsForbidden()
    {
        // Arrange
        var tenantId = "tenant-123";
        var userId = "user-123";
        
        // Create tenant and assign Viewer role
        var tenant = await CreateTestTenantAsync(tenantId);
        await CreateTestUserRoleAsync(tenant.Id, userId, "viewer@test.com", TenantRole.Viewer);
        
        // Require TenantAdmin role
        var attribute = new RequiresRoleAttribute(TenantRole.TenantAdmin);
        var context = CreateActionExecutingContext(tenantId, userId);

        // Act
        await attribute.OnActionExecutionAsync(context, () => Task.FromResult(CreateActionExecutedContext(context)));

        // Assert
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("INSUFFICIENT_PERMISSIONS", response.Error.Code);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ViewerAccessingViewerResource_Succeeds()
    {
        // Arrange
        var tenantId = "tenant-123";
        var userId = "user-123";
        
        var tenant = await CreateTestTenantAsync(tenantId);
        await CreateTestUserRoleAsync(tenant.Id, userId, "viewer@test.com", TenantRole.Viewer);
        
        var attribute = new RequiresRoleAttribute(TenantRole.Viewer);
        var context = CreateActionExecutingContext(tenantId, userId);
        var executed = false;

        // Act
        await attribute.OnActionExecutionAsync(context, () =>
        {
            executed = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        });

        // Assert
        Assert.Null(context.Result); // No early return
        Assert.True(executed); // Action was executed
    }

    [Fact]
    public async Task OnActionExecutionAsync_AdminAccessingAdminResource_Succeeds()
    {
        // Arrange
        var tenantId = "tenant-123";
        var userId = "admin-123";
        
        var tenant = await CreateTestTenantAsync(tenantId);
        await CreateTestUserRoleAsync(tenant.Id, userId, "admin@test.com", TenantRole.TenantAdmin);
        
        var attribute = new RequiresRoleAttribute(TenantRole.TenantAdmin);
        var context = CreateActionExecutingContext(tenantId, userId);
        var executed = false;

        // Act
        await attribute.OnActionExecutionAsync(context, () =>
        {
            executed = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        });

        // Assert
        Assert.Null(context.Result);
        Assert.True(executed);
    }

    [Fact]
    public async Task OnActionExecutionAsync_OwnerAccessingViewerResource_Succeeds()
    {
        // Arrange
        var tenantId = "tenant-123";
        var userId = "owner-123";
        
        var tenant = await CreateTestTenantAsync(tenantId);
        await CreateTestUserRoleAsync(tenant.Id, userId, "owner@test.com", TenantRole.TenantOwner);
        
        // Owner accessing Viewer resource (hierarchical)
        var attribute = new RequiresRoleAttribute(TenantRole.Viewer);
        var context = CreateActionExecutingContext(tenantId, userId);
        var executed = false;

        // Act
        await attribute.OnActionExecutionAsync(context, () =>
        {
            executed = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        });

        // Assert
        Assert.Null(context.Result);
        Assert.True(executed);
    }

    [Fact]
    public async Task OnActionExecutionAsync_InactiveUser_ReturnsForbidden()
    {
        // Arrange
        var tenantId = "tenant-123";
        var userId = "user-123";
        
        var tenant = await CreateTestTenantAsync(tenantId);
        var user = await CreateTestUserRoleAsync(tenant.Id, userId, "user@test.com", TenantRole.TenantAdmin);
        
        // Deactivate user
        user.IsActive = false;
        await _context.SaveChangesAsync();
        
        var attribute = new RequiresRoleAttribute(TenantRole.Viewer);
        var context = CreateActionExecutingContext(tenantId, userId);

        // Act
        await attribute.OnActionExecutionAsync(context, () => Task.FromResult(CreateActionExecutedContext(context)));

        // Assert
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("ACCESS_DENIED", response.Error.Code);
    }

    private async Task<TenantEntity> CreateTestTenantAsync(string entraIdTenantId)
    {
        var tenant = new TenantEntity
        {
            EntraIdTenantId = entraIdTenantId,
            OrganizationName = "Test Organization",
            PrimaryAdminEmail = "admin@test.com",
            Status = "Active",
            OnboardedDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }

    private async Task<TenantUserEntity> CreateTestUserRoleAsync(int tenantId, string azureAdObjectId, string upn, TenantRole role)
    {
        var user = new TenantUserEntity
        {
            TenantId = tenantId,
            AzureAdObjectId = azureAdObjectId,
            UserPrincipalName = upn,
            DisplayName = "Test User",
            Role = role,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedBy = "system"
        };

        _context.TenantUsers.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private ActionExecutingContext CreateActionExecutingContext(string? tenantId, string? userId)
    {
        var claims = new List<Claim>();
        if (tenantId != null)
            claims.Add(new Claim("tid", tenantId));
        if (userId != null)
            claims.Add(new Claim("oid", userId));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal,
            RequestServices = _services.BuildServiceProvider()
        };

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
    }
}

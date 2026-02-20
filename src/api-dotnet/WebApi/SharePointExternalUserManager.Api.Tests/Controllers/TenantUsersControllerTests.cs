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
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Tests.Controllers;

public class TenantUsersControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TenantUsersController _controller;

    public TenantUsersControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _controller = new TenantUsersController(
            _context,
            new NullLogger<TenantUsersController>());
    }

    [Fact]
    public async Task GetMyRole_PrimaryAdmin_ReturnsTenantOwner()
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

        SetupUserContext(tenantId, userId, email, "Admin User");

        // Act
        var result = await _controller.GetMyRole();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TenantUserResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("TenantOwner", response.Data!.Role);
        Assert.Equal(email, response.Data.Email);
    }

    [Fact]
    public async Task GetMyRole_TenantUser_ReturnsAssignedRole()
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
            DisplayName = "Test Viewer",
            Role = UserRole.Viewer,
            CreatedDate = DateTime.UtcNow
        };
        _context.TenantUsers.Add(tenantUser);
        await _context.SaveChangesAsync();

        SetupUserContext(tenantId, userId, email, "Test Viewer");

        // Act
        var result = await _controller.GetMyRole();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TenantUserResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Viewer", response.Data!.Role);
        Assert.Equal(email, response.Data.Email);
    }

    [Fact]
    public async Task GetMyRole_UserNotInTenant_ReturnsNotFound()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var email = "nouser@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "admin@example.com",
            Status = "Active"
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        SetupUserContext(tenantId, userId, email, "No User");

        // Act
        var result = await _controller.GetMyRole();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal("USER_NOT_FOUND", response.Error!.Code);
    }

    [Fact]
    public async Task AddTenantUser_ValidRequest_CreatesUser()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var currentUserId = "admin-user-id";
        var currentUserEmail = "admin@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = currentUserEmail,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        SetupUserContext(tenantId, currentUserId, currentUserEmail, "Admin User");

        var request = new AddTenantUserRequest
        {
            UserId = "new-user-id",
            Email = "newuser@example.com",
            DisplayName = "New User",
            Role = "TenantAdmin"
        };

        // Act
        var result = await _controller.AddTenantUser(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TenantUserResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("TenantAdmin", response.Data!.Role);
        Assert.Equal("newuser@example.com", response.Data.Email);

        // Verify in database
        var dbUser = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.UserId == "new-user-id" && tu.TenantId == tenant.Id);
        Assert.NotNull(dbUser);
        Assert.Equal(UserRole.TenantAdmin, dbUser.Role);
    }

    [Fact]
    public async Task AddTenantUser_DuplicateUser_ReturnsConflict()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var currentUserId = "admin-user-id";
        var currentUserEmail = "admin@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = currentUserEmail,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var existingUser = new TenantUserEntity
        {
            TenantId = tenant.Id,
            UserId = "existing-user-id",
            Email = "existing@example.com",
            Role = UserRole.Viewer,
            CreatedDate = DateTime.UtcNow
        };
        _context.TenantUsers.Add(existingUser);
        await _context.SaveChangesAsync();

        SetupUserContext(tenantId, currentUserId, currentUserEmail, "Admin User");

        var request = new AddTenantUserRequest
        {
            UserId = "existing-user-id",
            Email = "existing@example.com",
            Role = "TenantAdmin"
        };

        // Act
        var result = await _controller.AddTenantUser(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Equal("USER_EXISTS", response.Error!.Code);
    }

    [Fact]
    public async Task UpdateTenantUser_ValidRequest_UpdatesRole()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var currentUserId = "admin-user-id";
        var currentUserEmail = "admin@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = currentUserEmail,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var tenantUser = new TenantUserEntity
        {
            TenantId = tenant.Id,
            UserId = "user-id",
            Email = "user@example.com",
            Role = UserRole.Viewer,
            CreatedDate = DateTime.UtcNow
        };
        _context.TenantUsers.Add(tenantUser);
        await _context.SaveChangesAsync();

        SetupUserContext(tenantId, currentUserId, currentUserEmail, "Admin User");

        var request = new UpdateTenantUserRequest
        {
            Role = "TenantAdmin"
        };

        // Act
        var result = await _controller.UpdateTenantUser(tenantUser.Id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TenantUserResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("TenantAdmin", response.Data!.Role);

        // Verify in database
        var dbUser = await _context.TenantUsers.FindAsync(tenantUser.Id);
        Assert.Equal(UserRole.TenantAdmin, dbUser!.Role);
    }

    [Fact]
    public async Task RemoveTenantUser_ValidRequest_RemovesUser()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var currentUserId = "admin-user-id";
        var currentUserEmail = "admin@example.com";

        var tenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = currentUserEmail,
            Status = "Active"
        };
        _context.Tenants.Add(tenant);

        var tenantUser = new TenantUserEntity
        {
            TenantId = tenant.Id,
            UserId = "user-to-remove",
            Email = "remove@example.com",
            Role = UserRole.Viewer,
            CreatedDate = DateTime.UtcNow
        };
        _context.TenantUsers.Add(tenantUser);
        await _context.SaveChangesAsync();

        SetupUserContext(tenantId, currentUserId, currentUserEmail, "Admin User");

        // Act
        var result = await _controller.RemoveTenantUser(tenantUser.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);

        // Verify removed from database
        var dbUser = await _context.TenantUsers.FindAsync(tenantUser.Id);
        Assert.Null(dbUser);
    }

    [Fact]
    public async Task RemoveTenantUser_RemoveSelf_ReturnsBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var currentUserId = "admin-user-id";
        var currentUserEmail = "admin@example.com";

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
            UserId = currentUserId, // Same as current user
            Email = currentUserEmail,
            Role = UserRole.TenantAdmin,
            CreatedDate = DateTime.UtcNow
        };
        _context.TenantUsers.Add(tenantUser);
        await _context.SaveChangesAsync();

        SetupUserContext(tenantId, currentUserId, currentUserEmail, "Admin User");

        // Act
        var result = await _controller.RemoveTenantUser(tenantUser.Id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("CANNOT_REMOVE_SELF", response.Error!.Code);
    }

    private void SetupUserContext(string tenantId, string userId, string email, string name)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId),
            new Claim("email", email),
            new Claim("name", name)
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

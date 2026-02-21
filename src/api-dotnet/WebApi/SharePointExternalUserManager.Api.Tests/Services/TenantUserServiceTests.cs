using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Tests.Services;

public class TenantUserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TenantUserService _service;

    public TenantUserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new TenantUserService(_context, new NullLogger<TenantUserService>());
    }

    [Fact]
    public async Task GetTenantUsersAsync_ReturnsAllActiveUsers()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-1", "user1@test.com", TenantRole.TenantOwner, true);
        await CreateTestUserAsync(tenant.Id, "user-2", "user2@test.com", TenantRole.TenantAdmin, true);
        await CreateTestUserAsync(tenant.Id, "user-3", "user3@test.com", TenantRole.Viewer, true);
        await CreateTestUserAsync(tenant.Id, "user-4", "user4@test.com", TenantRole.Viewer, false); // Inactive

        // Act
        var result = await _service.GetTenantUsersAsync(tenant.Id);

        // Assert
        Assert.Equal(3, result.Count); // Should exclude inactive user
        Assert.All(result, u => Assert.True(u.IsActive));
    }

    [Fact]
    public async Task GetTenantUsersAsync_OrdersByRoleDescThenName()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-1", "charlie@test.com", TenantRole.Viewer, true);
        await CreateTestUserAsync(tenant.Id, "user-2", "alice@test.com", TenantRole.TenantOwner, true);
        await CreateTestUserAsync(tenant.Id, "user-3", "bob@test.com", TenantRole.TenantAdmin, true);

        // Act
        var result = await _service.GetTenantUsersAsync(tenant.Id);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(TenantRole.TenantOwner, result[0].Role); // Highest role first
        Assert.Equal(TenantRole.TenantAdmin, result[1].Role);
        Assert.Equal(TenantRole.Viewer, result[2].Role);
    }

    [Fact]
    public async Task GetTenantUserAsync_UserExists_ReturnsUser()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-123", "user@test.com", TenantRole.TenantAdmin, true);

        // Act
        var result = await _service.GetTenantUserAsync(tenant.Id, "user-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-123", result.AzureAdObjectId);
        Assert.Equal(TenantRole.TenantAdmin, result.Role);
    }

    [Fact]
    public async Task GetTenantUserAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");

        // Act
        var result = await _service.GetTenantUserAsync(tenant.Id, "nonexistent-user");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AssignRoleAsync_NewUser_CreatesUser()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        var request = new AssignRoleRequest
        {
            AzureAdObjectId = "new-user",
            UserPrincipalName = "newuser@test.com",
            DisplayName = "New User",
            Role = TenantRole.TenantAdmin
        };

        // Act
        var result = await _service.AssignRoleAsync(tenant.Id, request, "admin-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-user", result.AzureAdObjectId);
        Assert.Equal("newuser@test.com", result.UserPrincipalName);
        Assert.Equal(TenantRole.TenantAdmin, result.Role);
        Assert.True(result.IsActive);

        // Verify in database
        var dbUser = await _context.TenantUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == "new-user");
        Assert.NotNull(dbUser);
        Assert.Equal(tenant.Id, dbUser.TenantId);
    }

    [Fact]
    public async Task AssignRoleAsync_ExistingActiveUser_UpdatesRole()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-123", "user@test.com", TenantRole.Viewer, true);

        var request = new AssignRoleRequest
        {
            AzureAdObjectId = "user-123",
            UserPrincipalName = "user@test.com",
            DisplayName = "Updated User",
            Role = TenantRole.TenantAdmin
        };

        // Act
        var result = await _service.AssignRoleAsync(tenant.Id, request, "admin-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TenantRole.TenantAdmin, result.Role); // Role updated

        // Verify in database
        var dbUser = await _context.TenantUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == "user-123");
        Assert.NotNull(dbUser);
        Assert.Equal(TenantRole.TenantAdmin, dbUser.Role);
    }

    [Fact]
    public async Task AssignRoleAsync_ExistingInactiveUser_ReactivatesAndUpdates()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-123", "user@test.com", TenantRole.Viewer, false);

        var request = new AssignRoleRequest
        {
            AzureAdObjectId = "user-123",
            UserPrincipalName = "user@test.com",
            DisplayName = "Reactivated User",
            Role = TenantRole.TenantAdmin
        };

        // Act
        var result = await _service.AssignRoleAsync(tenant.Id, request, "admin-123");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        Assert.Equal(TenantRole.TenantAdmin, result.Role);

        // Verify in database
        var dbUser = await _context.TenantUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == "user-123");
        Assert.NotNull(dbUser);
        Assert.True(dbUser.IsActive);
    }

    [Fact]
    public async Task UpdateRoleAsync_UserExists_UpdatesRole()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-123", "user@test.com", TenantRole.Viewer, true);

        // Act
        var result = await _service.UpdateRoleAsync(tenant.Id, "user-123", TenantRole.TenantOwner);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TenantRole.TenantOwner, result.Role);

        // Verify in database
        var dbUser = await _context.TenantUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == "user-123");
        Assert.NotNull(dbUser);
        Assert.Equal(TenantRole.TenantOwner, dbUser.Role);
    }

    [Fact]
    public async Task UpdateRoleAsync_UserDoesNotExist_ThrowsException()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UpdateRoleAsync(tenant.Id, "nonexistent-user", TenantRole.TenantAdmin));
    }

    [Fact]
    public async Task RemoveUserAsync_UserExists_SoftDeletes()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-123", "user@test.com", TenantRole.Viewer, true);

        // Act
        var result = await _service.RemoveUserAsync(tenant.Id, "user-123");

        // Assert
        Assert.True(result);

        // Verify user is soft deleted
        var dbUser = await _context.TenantUsers.FirstOrDefaultAsync(u => u.AzureAdObjectId == "user-123");
        Assert.NotNull(dbUser);
        Assert.False(dbUser.IsActive);
    }

    [Fact]
    public async Task RemoveUserAsync_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");

        // Act
        var result = await _service.RemoveUserAsync(tenant.Id, "nonexistent-user");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasRoleAsync_UserHasRequiredRole_ReturnsTrue()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-123", "user@test.com", TenantRole.TenantAdmin, true);

        // Act
        var result = await _service.HasRoleAsync(tenant.Id, "user-123", TenantRole.TenantAdmin);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasRoleAsync_UserHasHigherRole_ReturnsTrue()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-123", "user@test.com", TenantRole.TenantOwner, true);

        // Act - Check if owner has admin privileges
        var result = await _service.HasRoleAsync(tenant.Id, "user-123", TenantRole.TenantAdmin);

        // Assert
        Assert.True(result); // Owner > Admin
    }

    [Fact]
    public async Task HasRoleAsync_UserHasLowerRole_ReturnsFalse()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-123", "user@test.com", TenantRole.Viewer, true);

        // Act
        var result = await _service.HasRoleAsync(tenant.Id, "user-123", TenantRole.TenantAdmin);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasRoleAsync_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");

        // Act
        var result = await _service.HasRoleAsync(tenant.Id, "nonexistent-user", TenantRole.Viewer);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserRoleAsync_UserExists_ReturnsRole()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");
        await CreateTestUserAsync(tenant.Id, "user-123", "user@test.com", TenantRole.TenantAdmin, true);

        // Act
        var result = await _service.GetUserRoleAsync(tenant.Id, "user-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TenantRole.TenantAdmin, result.Value);
    }

    [Fact]
    public async Task GetUserRoleAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var tenant = await CreateTestTenantAsync("tenant-123");

        // Act
        var result = await _service.GetUserRoleAsync(tenant.Id, "nonexistent-user");

        // Assert
        Assert.Null(result);
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

    private async Task<TenantUserEntity> CreateTestUserAsync(
        int tenantId,
        string azureAdObjectId,
        string upn,
        TenantRole role,
        bool isActive)
    {
        var user = new TenantUserEntity
        {
            TenantId = tenantId,
            AzureAdObjectId = azureAdObjectId,
            UserPrincipalName = upn,
            DisplayName = $"User {azureAdObjectId}",
            Role = role,
            IsActive = isActive,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedBy = "system"
        };

        _context.TenantUsers.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

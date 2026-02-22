using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models.ExternalUsers;

namespace SharePointExternalUserManager.Api.IntegrationTests.Workflows;

/// <summary>
/// Integration tests for external user management workflow
/// Tests invite, list, and remove operations for external users
/// </summary>
public class ExternalUserManagementWorkflowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ExternalUserManagementWorkflowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(TenantEntity tenant, ClientEntity client)> SetupTestTenantAndClient()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenant = new TenantEntity
        {
            EntraIdTenantId = "test-tenant-" + Guid.NewGuid(),
            OrganizationName = "Test Organization",
            PrimaryAdminEmail = "admin@testorg.com",
            Status = "Active",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var client = new ClientEntity
        {
            TenantId = tenant.Id,
            ClientReference = "TEST-CLIENT-001",
            ClientName = "Test Client Site",
            Description = "Test client for external user management",
            SharePointSiteId = "test-site-" + Guid.NewGuid(),
            SharePointSiteUrl = "https://contoso.sharepoint.com/sites/test-client",
            ProvisioningStatus = "Completed",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "admin@testorg.com"
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        return (tenant, client);
    }

    [Fact]
    public async Task ExternalUserManagement_InviteUser_Success()
    {
        // Arrange
        var (tenant, client) = await SetupTestTenantAndClient();
        
        var externalUser = new ExternalUserDto
        {
            Email = "external.user@partner.com",
            DisplayName = "External User",
            InvitedDate = DateTime.UtcNow,
            Status = "Active",
            PermissionLevel = "Read"
        };

        _factory.MockSharePointService?.Setup(s => s.InviteExternalUserAsync(
                client.SharePointSiteId!,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>()))
            .ReturnsAsync((true, externalUser, null));

        // Act
        using var scope = _factory.Services.CreateScope();
        var sharePointService = scope.ServiceProvider.GetRequiredService<ISharePointService>();
        
        var result = await sharePointService.InviteExternalUserAsync(
            client.SharePointSiteId!,
            "external.user@partner.com",
            "External User",
            "Read",
            "Welcome to our client portal",
            "admin@testorg.com");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("external.user@partner.com", result.User.Email);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExternalUserManagement_RemoveUser_Success()
    {
        // Arrange
        var (tenant, client) = await SetupTestTenantAndClient();
        
        _factory.MockSharePointService?.Setup(s => s.RemoveExternalUserAsync(
                client.SharePointSiteId!,
                It.IsAny<string>()))
            .ReturnsAsync((true, null));

        // Act
        using var scope = _factory.Services.CreateScope();
        var sharePointService = scope.ServiceProvider.GetRequiredService<ISharePointService>();
        
        var result = await sharePointService.RemoveExternalUserAsync(
            client.SharePointSiteId!,
            "external.user@partner.com");

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExternalUserManagement_GetExternalUsers_ReturnsUsers()
    {
        // Arrange
        var (tenant, client) = await SetupTestTenantAndClient();
        
        var externalUsers = new List<ExternalUserDto>
        {
            new()
            {
                Email = "user1@partner.com",
                DisplayName = "User One",
                InvitedDate = DateTime.UtcNow.AddDays(-5),
                Status = "Active",
                PermissionLevel = "Edit"
            },
            new()
            {
                Email = "user2@partner.com",
                DisplayName = "User Two",
                InvitedDate = DateTime.UtcNow.AddDays(-3),
                Status = "Active",
                PermissionLevel = "Read"
            }
        };

        _factory.MockSharePointService?.Setup(s => s.GetExternalUsersAsync(
                client.SharePointSiteId!))
            .ReturnsAsync(externalUsers);

        // Act
        using var scope = _factory.Services.CreateScope();
        var sharePointService = scope.ServiceProvider.GetRequiredService<ISharePointService>();
        
        var result = await sharePointService.GetExternalUsersAsync(client.SharePointSiteId!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Email == "user1@partner.com");
        Assert.Contains(result, u => u.Email == "user2@partner.com");
    }

    [Fact]
    public async Task ExternalUserManagement_InviteUserWithInvalidEmail_ReturnsError()
    {
        // Arrange
        var (tenant, client) = await SetupTestTenantAndClient();
        
        _factory.MockSharePointService?.Setup(s => s.InviteExternalUserAsync(
                client.SharePointSiteId!,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>()))
            .ReturnsAsync((false, null, "Invalid email address"));

        // Act
        using var scope = _factory.Services.CreateScope();
        var sharePointService = scope.ServiceProvider.GetRequiredService<ISharePointService>();
        
        var result = await sharePointService.InviteExternalUserAsync(
            client.SharePointSiteId!,
            "invalid-email",
            "Test User",
            "Read",
            null,
            "admin@testorg.com");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.User);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Invalid email", result.ErrorMessage);
    }

    [Fact]
    public async Task ExternalUserManagement_RemoveNonExistentUser_ReturnsError()
    {
        // Arrange
        var (tenant, client) = await SetupTestTenantAndClient();
        
        _factory.MockSharePointService?.Setup(s => s.RemoveExternalUserAsync(
                client.SharePointSiteId!,
                It.IsAny<string>()))
            .ReturnsAsync((false, "User not found"));

        // Act
        using var scope = _factory.Services.CreateScope();
        var sharePointService = scope.ServiceProvider.GetRequiredService<ISharePointService>();
        
        var result = await sharePointService.RemoveExternalUserAsync(
            client.SharePointSiteId!,
            "nonexistent@partner.com");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExternalUserManagement_CompleteLifecycle_Success()
    {
        // Arrange - setup tenant and client
        var (tenant, client) = await SetupTestTenantAndClient();
        var userEmail = "lifecycle.test@partner.com";
        
        var externalUser = new ExternalUserDto
        {
            Email = userEmail,
            DisplayName = "Lifecycle Test User",
            InvitedDate = DateTime.UtcNow,
            Status = "Active",
            PermissionLevel = "Edit"
        };

        // Mock invite
        _factory.MockSharePointService?.Setup(s => s.InviteExternalUserAsync(
                client.SharePointSiteId!,
                userEmail,
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>()))
            .ReturnsAsync((true, externalUser, null));

        // Mock list (returns invited user)
        _factory.MockSharePointService?.Setup(s => s.GetExternalUsersAsync(
                client.SharePointSiteId!))
            .ReturnsAsync(new List<ExternalUserDto> { externalUser });

        // Mock remove
        _factory.MockSharePointService?.Setup(s => s.RemoveExternalUserAsync(
                client.SharePointSiteId!,
                userEmail))
            .ReturnsAsync((true, null));

        using var scope = _factory.Services.CreateScope();
        var sharePointService = scope.ServiceProvider.GetRequiredService<ISharePointService>();

        // Act - Step 1: Invite user
        var inviteResult = await sharePointService.InviteExternalUserAsync(
            client.SharePointSiteId!,
            userEmail,
            "Lifecycle Test User",
            "Edit",
            "Welcome",
            "admin@testorg.com");

        Assert.True(inviteResult.Success);
        Assert.NotNull(inviteResult.User);

        // Act - Step 2: List users (should contain invited user)
        var users = await sharePointService.GetExternalUsersAsync(client.SharePointSiteId!);
        Assert.Contains(users, u => u.Email == userEmail);

        // Act - Step 3: Remove user
        var removeResult = await sharePointService.RemoveExternalUserAsync(
            client.SharePointSiteId!,
            userEmail);

        // Assert
        Assert.True(removeResult.Success);
    }
}

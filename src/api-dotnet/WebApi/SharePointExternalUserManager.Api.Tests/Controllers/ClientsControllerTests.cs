using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using Xunit;
using SharePointExternalUserManager.Api.Controllers;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Models.Clients;
using SharePointExternalUserManager.Functions.Models.ExternalUsers;

namespace SharePointExternalUserManager.Api.Tests.Controllers;

public class ClientsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ClientsController _controller;
    private readonly Mock<ISharePointService> _mockSharePointService;
    private readonly IAuditLogService _auditLogService;

    public ClientsControllerTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _mockSharePointService = new Mock<ISharePointService>();
        _auditLogService = new MockAuditLogService();
        
        _controller = new ClientsController(
            _context,
            _mockSharePointService.Object,
            _auditLogService,
            new NullLogger<ClientsController>());
    }

    #region GetClients Tests

    [Fact]
    public async Task GetClients_WithValidTenant_ReturnsClients()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var upn = "testuser@example.com";

        var tenant = await CreateTestTenantAsync(tenantId);
        var client1 = await CreateTestClientAsync(tenant.Id, "CLIENT-001", "Client One");
        var client2 = await CreateTestClientAsync(tenant.Id, "CLIENT-002", "Client Two");

        SetupControllerContext(tenantId, userId, upn);

        // Act
        var result = await _controller.GetClients();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<ClientResponse>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data.Count);
    }

    [Fact]
    public async Task GetClients_WithMissingTenantClaim_ReturnsUnauthorized()
    {
        // Arrange
        SetupControllerContext(null, "user-id", "user@example.com");

        // Act
        var result = await _controller.GetClients();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Equal("AUTH_ERROR", response.Error?.Code);
    }

    [Fact]
    public async Task GetClients_WithNonExistentTenant_ReturnsNotFound()
    {
        // Arrange
        SetupControllerContext("non-existent-tenant", "user-id", "user@example.com");

        // Act
        var result = await _controller.GetClients();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal("TENANT_NOT_FOUND", response.Error?.Code);
    }

    #endregion

    #region CreateClient Tests

    [Fact]
    public async Task CreateClient_WithValidRequest_CreatesClient()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var upn = "testuser@example.com";

        var tenant = await CreateTestTenantAsync(tenantId);
        SetupControllerContext(tenantId, userId, upn);

        var request = new CreateClientRequest
        {
            ClientReference = "CLIENT-TEST",
            ClientName = "Test Client",
            Description = "Test Description"
        };

        // Mock SharePoint site creation
        _mockSharePointService
            .Setup(s => s.CreateClientSiteAsync(It.IsAny<ClientEntity>(), It.IsAny<string>()))
            .ReturnsAsync((true, "site-id-123", "https://tenant.sharepoint.com/sites/client-test", (string?)null));

        // Act
        var result = await _controller.CreateClient(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ApiResponse<ClientResponse>>(createdResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(request.ClientReference, response.Data.ClientReference);
        Assert.Equal(request.ClientName, response.Data.ClientName);
        Assert.Equal("Provisioned", response.Data.ProvisioningStatus);
        Assert.NotNull(response.Data.SharePointSiteUrl);
    }

    [Fact]
    public async Task CreateClient_WithDuplicateReference_ReturnsConflict()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        var upn = "testuser@example.com";

        var tenant = await CreateTestTenantAsync(tenantId);
        await CreateTestClientAsync(tenant.Id, "CLIENT-DUP", "Existing Client");

        SetupControllerContext(tenantId, userId, upn);

        var request = new CreateClientRequest
        {
            ClientReference = "CLIENT-DUP", // Duplicate
            ClientName = "New Client",
            Description = "Test"
        };

        // Act
        var result = await _controller.CreateClient(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Equal("CLIENT_EXISTS", response.Error?.Code);
    }

    #endregion

    #region GetExternalUsers Tests

    [Fact]
    public async Task GetExternalUsers_WithValidClient_ReturnsUsers()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var tenant = await CreateTestTenantAsync(tenantId);
        var client = await CreateTestClientAsync(tenant.Id, "CLIENT-001", "Client One");
        client.SharePointSiteId = "site-id-123";
        await _context.SaveChangesAsync();

        SetupControllerContext(tenantId, "user-id", "user@example.com");

        var mockUsers = new List<ExternalUserDto>
        {
            new() { Email = "external1@example.com", DisplayName = "External User 1" },
            new() { Email = "external2@example.com", DisplayName = "External User 2" }
        };

        _mockSharePointService
            .Setup(s => s.GetExternalUsersAsync(client.SharePointSiteId))
            .ReturnsAsync(mockUsers);

        // Act
        var result = await _controller.GetExternalUsers(client.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<ExternalUserDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data.Count);
    }

    [Fact]
    public async Task GetExternalUsers_WithUnprovisionedSite_ReturnsBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var tenant = await CreateTestTenantAsync(tenantId);
        var client = await CreateTestClientAsync(tenant.Id, "CLIENT-001", "Client One");
        // No SharePointSiteId set

        SetupControllerContext(tenantId, "user-id", "user@example.com");

        // Act
        var result = await _controller.GetExternalUsers(client.Id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("SITE_NOT_PROVISIONED", response.Error?.Code);
    }

    #endregion

    #region Helper Methods

    private async Task<TenantEntity> CreateTestTenantAsync(string entraIdTenantId)
    {
        var tenant = new TenantEntity
        {
            EntraIdTenantId = entraIdTenantId,
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

    private async Task<ClientEntity> CreateTestClientAsync(int tenantId, string reference, string name)
    {
        var client = new ClientEntity
        {
            TenantId = tenantId,
            ClientReference = reference,
            ClientName = name,
            Description = "Test client",
            ProvisioningStatus = "Provisioned",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    private void SetupControllerContext(string? tenantId, string? userId, string? upn)
    {
        var claims = new List<Claim>();
        
        if (tenantId != null)
            claims.Add(new Claim("tid", tenantId));
        if (userId != null)
            claims.Add(new Claim("oid", userId));
        if (upn != null)
            claims.Add(new Claim("upn", upn));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #endregion
}

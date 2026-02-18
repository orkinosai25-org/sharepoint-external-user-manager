using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SharePointExternalUserManager.Api.Controllers;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Tests.Controllers;

public class ConsentControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ConsentController _controller;
    private readonly IConfiguration _configuration;

    public ConsentControllerTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);

        // Mock configuration
        var configData = new Dictionary<string, string?>
        {
            { "AzureAd:ClientId", "test-client-id" },
            { "AzureAd:TenantId", "common" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _controller = new ConsentController(
            _context,
            new NullLogger<ConsentController>(),
            _configuration);

        // Setup HTTP context
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("api.example.com")
                }
            }
        };
    }

    [Fact]
    public void GetConsentUrl_ReturnsConsentUrlWithDefaults()
    {
        // Act
        var result = _controller.GetConsentUrl(null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        
        var data = response.Data as dynamic;
        Assert.NotNull(data);
        
        var consentUrl = data.GetType().GetProperty("consentUrl")?.GetValue(data)?.ToString();
        Assert.NotNull(consentUrl);
        Assert.Contains("login.microsoftonline.com", consentUrl);
        Assert.Contains("test-client-id", consentUrl);
        Assert.Contains("adminconsent", consentUrl);
    }

    [Fact]
    public void GetConsentUrl_WithCustomRedirectUri_UsesCustomUri()
    {
        // Arrange
        var customRedirectUri = "https://custom.example.com/callback";

        // Act
        var result = _controller.GetConsentUrl(customRedirectUri);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        
        var data = response.Data as dynamic;
        var consentUrl = data.GetType().GetProperty("consentUrl")?.GetValue(data)?.ToString();
        Assert.Contains(Uri.EscapeDataString(customRedirectUri), consentUrl);
    }

    [Fact]
    public async Task ConsentCallback_WithValidConsent_ReturnsSuccess()
    {
        // Arrange
        var tenantId = "test-tenant-id";

        // Act
        var result = await _controller.ConsentCallback("True", tenantId, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ConsentCallback_WithError_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ConsentCallback(
            null, 
            null, 
            "access_denied", 
            "User denied consent");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("access_denied", response.Error.Code);
    }

    [Fact]
    public async Task ConsentCallback_WithoutAdminConsent_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ConsentCallback("False", "tenant-id", null, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("INVALID_CONSENT", response.Error.Code);
    }

    [Fact]
    public async Task ConsentCallback_WithExistingTenant_UpdatesTenant()
    {
        // Arrange
        var tenantId = "existing-tenant-id";
        var existingTenant = new TenantEntity
        {
            EntraIdTenantId = tenantId,
            OrganizationName = "Test Org",
            PrimaryAdminEmail = "admin@test.com",
            Status = "Active",
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            ModifiedDate = DateTime.UtcNow.AddDays(-1)
        };
        _context.Tenants.Add(existingTenant);
        await _context.SaveChangesAsync();

        var originalModifiedDate = existingTenant.ModifiedDate;

        // Act
        var result = await _controller.ConsentCallback("True", tenantId, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);

        // Verify tenant was updated
        var updatedTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);
        Assert.NotNull(updatedTenant);
        Assert.True(updatedTenant.ModifiedDate > originalModifiedDate);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

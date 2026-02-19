using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Moq;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using Xunit;

namespace SharePointExternalUserManager.Api.Tests.Services;

public class SharePointValidationServiceTests
{
    private readonly Mock<ILogger<SharePointService>> _mockLogger;
    private readonly SharePointService _service;

    public SharePointValidationServiceTests()
    {
        // Create a null GraphServiceClient for these tests since we only test URL validation
        // before any Graph API calls are made. These tests validate input parameters and
        // URL format, which don't require Graph API access. 
        // For tests that need Graph API interaction, a proper mock would be required.
        var graphClient = null as GraphServiceClient;
        _mockLogger = new Mock<ILogger<SharePointService>>();
        _service = new SharePointService(graphClient!, _mockLogger.Object);
    }

    [Fact]
    public async Task ValidateSiteAsync_WithEmptyUrl_ReturnsInvalidUrl()
    {
        // Act
        var result = await _service.ValidateSiteAsync("");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(SiteValidationErrorCode.InvalidUrl, result.ErrorCode);
        Assert.Contains("cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateSiteAsync_WithInvalidUrlFormat_ReturnsInvalidUrl()
    {
        // Act
        var result = await _service.ValidateSiteAsync("not-a-valid-url");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(SiteValidationErrorCode.InvalidUrl, result.ErrorCode);
        Assert.Contains("not a valid URL format", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateSiteAsync_WithNonSharePointUrl_ReturnsInvalidUrl()
    {
        // Act
        var result = await _service.ValidateSiteAsync("https://example.com/site");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(SiteValidationErrorCode.InvalidUrl, result.ErrorCode);
        Assert.Contains("SharePoint site", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateSiteAsync_WithFakeSharePointDomain_ReturnsInvalidUrl()
    {
        // Act
        var result = await _service.ValidateSiteAsync("https://fakesharepoint.com.example.com/sites/test");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(SiteValidationErrorCode.InvalidUrl, result.ErrorCode);
        Assert.Contains("SharePoint site", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateSiteAsync_WithSharePointRootUrl_ReturnsInvalidUrl()
    {
        // Act
        var result = await _service.ValidateSiteAsync("https://contoso.sharepoint.com");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(SiteValidationErrorCode.InvalidUrl, result.ErrorCode);
        Assert.Contains("site path", result.ErrorMessage);
    }

    [Theory]
    [InlineData("https://contoso.sharepoint.com/")]
    public async Task ValidateSiteAsync_WithMissingSiteName_ReturnsInvalidUrl(string url)
    {
        // Act
        var result = await _service.ValidateSiteAsync(url);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(SiteValidationErrorCode.InvalidUrl, result.ErrorCode);
    }
}

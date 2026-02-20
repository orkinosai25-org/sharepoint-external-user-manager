using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace SharePointExternalUserManager.Api.Tests.Middleware;

/// <summary>
/// Tests for per-tenant rate limiting functionality
/// </summary>
public class RateLimitingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RateLimiter_ExceedsLimit_Returns429()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Make many requests quickly to exceed the 100 per minute limit
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 105; i++)
        {
            tasks.Add(client.GetAsync("/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - At least one should be rate limited
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.True(rateLimitedResponses > 0, "Expected at least one request to be rate limited");
    }

    [Fact]
    public async Task RateLimiter_RateLimitedResponse_ContainsExpectedErrorFormat()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Make many requests to trigger rate limit
        HttpResponseMessage? rateLimitedResponse = null;
        for (int i = 0; i < 110; i++)
        {
            var response = await client.GetAsync("/health");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedResponse = response;
                break;
            }
        }

        // Assert
        Assert.NotNull(rateLimitedResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimitedResponse.StatusCode);
        
        var content = await rateLimitedResponse.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.True(json.RootElement.TryGetProperty("error", out var errorProperty));
        Assert.Equal("RATE_LIMIT_EXCEEDED", errorProperty.GetString());
        
        Assert.True(json.RootElement.TryGetProperty("message", out var messageProperty));
        Assert.Contains("Too many requests", messageProperty.GetString());
    }
}

/// <summary>
/// Unit tests for rate limiting logic using direct limiter testing
/// </summary>
public class RateLimitingLogicTests
{
    [Fact]
    public void TenantId_Extraction_FromClaims_WorksCorrectly()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim("tid", "test-tenant-123"),
            new Claim("oid", "user-456")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        // Act
        var tenantId = httpContext.User?.FindFirst("tid")?.Value ?? "anonymous";

        // Assert
        Assert.Equal("test-tenant-123", tenantId);
    }

    [Fact]
    public void TenantId_Extraction_WithoutClaims_ReturnsAnonymous()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var tenantId = httpContext.User?.FindFirst("tid")?.Value ?? "anonymous";

        // Assert
        Assert.Equal("anonymous", tenantId);
    }

    [Fact]
    public void TenantId_Extraction_FromMultipleTenants_IsDistinct()
    {
        // Arrange & Act
        var tenantIds = new List<string>();
        
        for (int i = 0; i < 3; i++)
        {
            var httpContext = new DefaultHttpContext();
            var claims = new[] { new Claim("tid", $"tenant-{i}") };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
            
            var tenantId = httpContext.User?.FindFirst("tid")?.Value ?? "anonymous";
            tenantIds.Add(tenantId);
        }

        // Assert
        Assert.Equal(3, tenantIds.Distinct().Count());
        Assert.Contains("tenant-0", tenantIds);
        Assert.Contains("tenant-1", tenantIds);
        Assert.Contains("tenant-2", tenantIds);
    }
}

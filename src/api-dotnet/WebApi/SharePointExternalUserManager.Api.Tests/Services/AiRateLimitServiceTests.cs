using Xunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SharePointExternalUserManager.Api.Services;

namespace SharePointExternalUserManager.Api.Tests.Services;

public class AiRateLimitServiceTests
{
    [Fact]
    public void IsRateLimitExceeded_NoRequests_ReturnsFalse()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<AiRateLimitService>();
        var service = new AiRateLimitService(cache, logger);
        var tenantId = 1;
        var maxRequests = 100;

        // Act
        var result = service.IsRateLimitExceeded(tenantId, maxRequests);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRateLimitExceeded_WithinLimit_ReturnsFalse()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<AiRateLimitService>();
        var service = new AiRateLimitService(cache, logger);
        var tenantId = 1;
        var maxRequests = 100;

        // Act
        for (int i = 0; i < 50; i++)
        {
            service.IncrementRequestCount(tenantId);
        }
        var result = service.IsRateLimitExceeded(tenantId, maxRequests);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRateLimitExceeded_ExceedsLimit_ReturnsTrue()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<AiRateLimitService>();
        var service = new AiRateLimitService(cache, logger);
        var tenantId = 1;
        var maxRequests = 10;

        // Act
        for (int i = 0; i < 15; i++)
        {
            service.IncrementRequestCount(tenantId);
        }
        var result = service.IsRateLimitExceeded(tenantId, maxRequests);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IncrementRequestCount_IncrementsCorrectly()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<AiRateLimitService>();
        var service = new AiRateLimitService(cache, logger);
        var tenantId = 1;
        var maxRequests = 100;

        // Act
        service.IncrementRequestCount(tenantId);
        service.IncrementRequestCount(tenantId);
        service.IncrementRequestCount(tenantId);
        
        var remaining = service.GetRemainingRequests(tenantId, maxRequests);

        // Assert
        Assert.Equal(97, remaining);
    }

    [Fact]
    public void GetRemainingRequests_NoRequests_ReturnsMax()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<AiRateLimitService>();
        var service = new AiRateLimitService(cache, logger);
        var tenantId = 1;
        var maxRequests = 100;

        // Act
        var remaining = service.GetRemainingRequests(tenantId, maxRequests);

        // Assert
        Assert.Equal(100, remaining);
    }

    [Fact]
    public void ResetRateLimit_ClearsRequestCount()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<AiRateLimitService>();
        var service = new AiRateLimitService(cache, logger);
        var tenantId = 1;
        var maxRequests = 100;

        // Act
        for (int i = 0; i < 50; i++)
        {
            service.IncrementRequestCount(tenantId);
        }
        service.ResetRateLimit(tenantId);
        var remaining = service.GetRemainingRequests(tenantId, maxRequests);

        // Assert
        Assert.Equal(100, remaining);
    }

    [Fact]
    public void IsRateLimitExceeded_NullTenant_ReturnsFalse()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<AiRateLimitService>();
        var service = new AiRateLimitService(cache, logger);

        // Act
        var result = service.IsRateLimitExceeded(null, 100);

        // Assert
        Assert.False(result); // Marketing mode with no tenant
    }
}

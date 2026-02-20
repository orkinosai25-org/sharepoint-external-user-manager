using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SharePointExternalUserManager.Api.Services;
using Xunit;

namespace SharePointExternalUserManager.Api.Tests.Services;

public class TenantRateLimitServiceTests
{
    [Fact]
    public void CheckRateLimit_NoRequests_AllowsRequest()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TenantRateLimitService>();
        var service = new TenantRateLimitService(cache, logger);
        var tenantId = "tenant-123";
        var requestsPerMinute = 100;

        // Act
        var result = service.CheckRateLimit(tenantId, requestsPerMinute);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(99, result.RemainingRequests); // 100 - 1
        Assert.Equal(100, result.Limit);
    }

    [Fact]
    public void CheckRateLimit_WithinLimit_AllowsRequest()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TenantRateLimitService>();
        var service = new TenantRateLimitService(cache, logger);
        var tenantId = "tenant-123";
        var requestsPerMinute = 10;

        // Act - Make 5 requests
        for (int i = 0; i < 5; i++)
        {
            service.CheckRateLimit(tenantId, requestsPerMinute);
        }
        var result = service.CheckRateLimit(tenantId, requestsPerMinute);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(4, result.RemainingRequests); // 10 - 6
        Assert.Equal(10, result.Limit);
    }

    [Fact]
    public void CheckRateLimit_ExceedsLimit_BlocksRequest()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TenantRateLimitService>();
        var service = new TenantRateLimitService(cache, logger);
        var tenantId = "tenant-123";
        var requestsPerMinute = 5;

        // Act - Make 5 requests to reach limit
        for (int i = 0; i < 5; i++)
        {
            var intermediateResult = service.CheckRateLimit(tenantId, requestsPerMinute);
            Assert.True(intermediateResult.IsAllowed, $"Request {i + 1} should be allowed");
        }
        
        // Try one more request that should be blocked
        var result = service.CheckRateLimit(tenantId, requestsPerMinute);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal(0, result.RemainingRequests);
        Assert.Equal(5, result.Limit);
    }

    [Fact]
    public void CheckRateLimit_DifferentTenants_IndependentLimits()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TenantRateLimitService>();
        var service = new TenantRateLimitService(cache, logger);
        var tenant1 = "tenant-1";
        var tenant2 = "tenant-2";
        var requestsPerMinute = 3;

        // Act - Exhaust tenant1's limit
        for (int i = 0; i < 3; i++)
        {
            service.CheckRateLimit(tenant1, requestsPerMinute);
        }
        var tenant1Result = service.CheckRateLimit(tenant1, requestsPerMinute);
        var tenant2Result = service.CheckRateLimit(tenant2, requestsPerMinute);

        // Assert
        Assert.False(tenant1Result.IsAllowed); // Tenant 1 should be blocked
        Assert.True(tenant2Result.IsAllowed);  // Tenant 2 should be allowed
    }

    [Fact]
    public void CheckRateLimit_EmptyTenantId_AllowsRequest()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TenantRateLimitService>();
        var service = new TenantRateLimitService(cache, logger);
        var requestsPerMinute = 100;

        // Act
        var result = service.CheckRateLimit("", requestsPerMinute);

        // Assert
        Assert.True(result.IsAllowed); // Should allow when no tenant ID
    }

    [Fact]
    public void GetStatus_NoRequests_ReturnsZeroCount()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TenantRateLimitService>();
        var service = new TenantRateLimitService(cache, logger);
        var tenantId = "tenant-123";
        var requestsPerMinute = 100;

        // Act
        var status = service.GetStatus(tenantId, requestsPerMinute);

        // Assert
        Assert.Equal(0, status.RequestCount);
        Assert.Equal(100, status.Limit);
    }

    [Fact]
    public void GetStatus_AfterRequests_ReturnsCorrectCount()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TenantRateLimitService>();
        var service = new TenantRateLimitService(cache, logger);
        var tenantId = "tenant-123";
        var requestsPerMinute = 100;

        // Act - Make 7 requests
        for (int i = 0; i < 7; i++)
        {
            service.CheckRateLimit(tenantId, requestsPerMinute);
        }
        var status = service.GetStatus(tenantId, requestsPerMinute);

        // Assert
        Assert.Equal(7, status.RequestCount);
        Assert.Equal(100, status.Limit);
    }

    [Fact]
    public void CheckRateLimit_ResetTimeIsInFuture()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TenantRateLimitService>();
        var service = new TenantRateLimitService(cache, logger);
        var tenantId = "tenant-123";
        var requestsPerMinute = 100;
        var now = DateTime.UtcNow;

        // Act
        var result = service.CheckRateLimit(tenantId, requestsPerMinute);

        // Assert
        Assert.True(result.ResetTime > now);
        Assert.True(result.ResetTime <= now.AddSeconds(65)); // Should be within 60-65 seconds
    }

    [Fact]
    public void CheckRateLimit_ConcurrentRequests_ThreadSafe()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new NullLogger<TenantRateLimitService>();
        var service = new TenantRateLimitService(cache, logger);
        var tenantId = "tenant-123";
        var requestsPerMinute = 50;
        var tasks = new List<Task<TenantRateLimitResult>>();

        // Act - Make 100 concurrent requests
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => service.CheckRateLimit(tenantId, requestsPerMinute)));
        }
        Task.WaitAll(tasks.ToArray());
        
        var results = tasks.Select(t => t.Result).ToList();
        var allowedCount = results.Count(r => r.IsAllowed);
        var blockedCount = results.Count(r => !r.IsAllowed);

        // Assert
        // Exactly 50 requests should be allowed, 50 should be blocked
        Assert.Equal(50, allowedCount);
        Assert.Equal(50, blockedCount);
    }
}

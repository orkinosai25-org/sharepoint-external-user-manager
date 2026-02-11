using Microsoft.Extensions.Caching.Memory;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service for rate limiting AI requests
/// </summary>
public class AiRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AiRateLimitService> _logger;
    private const string RATE_LIMIT_KEY_PREFIX = "ai_rate_limit_";
    private const string HOURLY_KEY_SUFFIX = "_hourly";

    public AiRateLimitService(IMemoryCache cache, ILogger<AiRateLimitService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Check if tenant has exceeded rate limit
    /// </summary>
    public bool IsRateLimitExceeded(int? tenantId, int maxRequestsPerHour)
    {
        if (tenantId == null)
        {
            // For marketing mode without tenant, use IP-based or session-based limiting
            // For now, allow unlimited for marketing mode
            return false;
        }

        var key = $"{RATE_LIMIT_KEY_PREFIX}{tenantId}{HOURLY_KEY_SUFFIX}";
        
        if (!_cache.TryGetValue(key, out int requestCount))
        {
            requestCount = 0;
        }

        return requestCount >= maxRequestsPerHour;
    }

    /// <summary>
    /// Increment request count for tenant
    /// </summary>
    public void IncrementRequestCount(int? tenantId)
    {
        if (tenantId == null)
        {
            // Skip for marketing mode
            return;
        }

        var key = $"{RATE_LIMIT_KEY_PREFIX}{tenantId}{HOURLY_KEY_SUFFIX}";
        
        if (!_cache.TryGetValue(key, out int requestCount))
        {
            requestCount = 0;
        }

        requestCount++;

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(1));

        _cache.Set(key, requestCount, cacheOptions);

        _logger.LogInformation("Tenant {TenantId} AI request count: {Count}", tenantId, requestCount);
    }

    /// <summary>
    /// Get remaining requests for tenant this hour
    /// </summary>
    public int GetRemainingRequests(int? tenantId, int maxRequestsPerHour)
    {
        if (tenantId == null)
        {
            return int.MaxValue; // Unlimited for marketing mode
        }

        var key = $"{RATE_LIMIT_KEY_PREFIX}{tenantId}{HOURLY_KEY_SUFFIX}";
        
        if (!_cache.TryGetValue(key, out int requestCount))
        {
            requestCount = 0;
        }

        return Math.Max(0, maxRequestsPerHour - requestCount);
    }

    /// <summary>
    /// Reset rate limit for tenant (admin use)
    /// </summary>
    public void ResetRateLimit(int tenantId)
    {
        var key = $"{RATE_LIMIT_KEY_PREFIX}{tenantId}{HOURLY_KEY_SUFFIX}";
        _cache.Remove(key);
        _logger.LogInformation("Reset rate limit for tenant {TenantId}", tenantId);
    }
}

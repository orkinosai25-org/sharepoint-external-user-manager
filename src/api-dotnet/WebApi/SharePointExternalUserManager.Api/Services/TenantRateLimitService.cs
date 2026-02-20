using Microsoft.Extensions.Caching.Memory;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service for per-tenant rate limiting using token bucket algorithm
/// </summary>
public interface ITenantRateLimitService
{
    /// <summary>
    /// Check if a request should be allowed based on rate limits
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="requestsPerMinute">Maximum requests allowed per minute for this tenant</param>
    /// <returns>Rate limit result with allowed status and remaining requests</returns>
    TenantRateLimitResult CheckRateLimit(string tenantId, int requestsPerMinute);
    
    /// <summary>
    /// Get current rate limit status for a tenant
    /// </summary>
    RateLimitStatus GetStatus(string tenantId, int requestsPerMinute);
}

/// <summary>
/// Result of a rate limit check
/// </summary>
public class TenantRateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RemainingRequests { get; set; }
    public int Limit { get; set; }
    public DateTime ResetTime { get; set; }
}

/// <summary>
/// Current rate limit status
/// </summary>
public class RateLimitStatus
{
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime ResetTime { get; set; }
}

/// <summary>
/// Implementation of tenant-based rate limiting using sliding window
/// </summary>
public class TenantRateLimitService : ITenantRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TenantRateLimitService> _logger;
    private readonly object _lock = new();
    private const string CACHE_KEY_PREFIX = "rate_limit_";
    private const int WINDOW_DURATION_SECONDS = 60; // 1 minute window

    public TenantRateLimitService(IMemoryCache cache, ILogger<TenantRateLimitService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public TenantRateLimitResult CheckRateLimit(string tenantId, int requestsPerMinute)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("Rate limit check called with empty tenant ID");
            return new TenantRateLimitResult
            {
                IsAllowed = true,
                RemainingRequests = requestsPerMinute,
                Limit = requestsPerMinute,
                ResetTime = DateTime.UtcNow.AddSeconds(WINDOW_DURATION_SECONDS)
            };
        }

        var cacheKey = $"{CACHE_KEY_PREFIX}{tenantId}";
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            var window = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(WINDOW_DURATION_SECONDS + 10);
                return new RateLimitWindow
                {
                    WindowStart = now,
                    RequestCount = 0,
                    Limit = requestsPerMinute
                };
            });

            if (window == null)
            {
                // Should never happen, but handle gracefully
                window = new RateLimitWindow
                {
                    WindowStart = now,
                    RequestCount = 0,
                    Limit = requestsPerMinute
                };
                _cache.Set(cacheKey, window, TimeSpan.FromSeconds(WINDOW_DURATION_SECONDS + 10));
            }

            // Check if window has expired and reset if needed
            var windowAge = (now - window.WindowStart).TotalSeconds;
            if (windowAge >= WINDOW_DURATION_SECONDS)
            {
                window.WindowStart = now;
                window.RequestCount = 0;
                window.Limit = requestsPerMinute;
            }

            // Check if request is allowed
            var isAllowed = window.RequestCount < requestsPerMinute;
            
            if (isAllowed)
            {
                window.RequestCount++;
                _cache.Set(cacheKey, window, TimeSpan.FromSeconds(WINDOW_DURATION_SECONDS + 10));
            }
            else
            {
                _logger.LogWarning(
                    "Rate limit exceeded for tenant {TenantId}: {RequestCount}/{Limit} requests",
                    tenantId,
                    window.RequestCount,
                    requestsPerMinute);
            }

            var resetTime = window.WindowStart.AddSeconds(WINDOW_DURATION_SECONDS);
            var remainingRequests = Math.Max(0, requestsPerMinute - window.RequestCount);

            return new TenantRateLimitResult
            {
                IsAllowed = isAllowed,
                RemainingRequests = remainingRequests,
                Limit = requestsPerMinute,
                ResetTime = resetTime
            };
        }
    }

    public RateLimitStatus GetStatus(string tenantId, int requestsPerMinute)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return new RateLimitStatus
            {
                RequestCount = 0,
                Limit = requestsPerMinute,
                WindowStart = DateTime.UtcNow,
                ResetTime = DateTime.UtcNow.AddSeconds(WINDOW_DURATION_SECONDS)
            };
        }

        var cacheKey = $"{CACHE_KEY_PREFIX}{tenantId}";
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out RateLimitWindow? window) && window != null)
            {
                var windowAge = (now - window.WindowStart).TotalSeconds;
                if (windowAge < WINDOW_DURATION_SECONDS)
                {
                    return new RateLimitStatus
                    {
                        RequestCount = window.RequestCount,
                        Limit = window.Limit,
                        WindowStart = window.WindowStart,
                        ResetTime = window.WindowStart.AddSeconds(WINDOW_DURATION_SECONDS)
                    };
                }
            }

            return new RateLimitStatus
            {
                RequestCount = 0,
                Limit = requestsPerMinute,
                WindowStart = now,
                ResetTime = now.AddSeconds(WINDOW_DURATION_SECONDS)
            };
        }
    }

    private class RateLimitWindow
    {
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
        public int Limit { get; set; }
    }
}

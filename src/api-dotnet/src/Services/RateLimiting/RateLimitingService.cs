using Microsoft.Extensions.Logging;
using SharePointExternalUserManager.Functions.Models;
using System.Collections.Concurrent;

namespace SharePointExternalUserManager.Functions.Services.RateLimiting;

/// <summary>
/// In-memory rate limiting service (MVP implementation)
/// Phase 2: Replace with Redis or distributed cache for production
/// </summary>
public class RateLimitingService : IRateLimitingService
{
    private readonly ILogger<RateLimitingService> _logger;
    private readonly ConcurrentDictionary<string, RateLimitWindow> _rateLimitWindows = new();

    // Rate limits per minute by tier
    private readonly Dictionary<SubscriptionTier, int> _searchRateLimits = new()
    {
        { SubscriptionTier.Free, 10 },      // 10 searches per minute
        { SubscriptionTier.Pro, 60 },       // 60 searches per minute
        { SubscriptionTier.Enterprise, 300 } // 300 searches per minute
    };

    public RateLimitingService(ILogger<RateLimitingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if a request is allowed based on rate limits
    /// </summary>
    public async Task<RateLimitResult> CheckRateLimitAsync(Guid tenantId, string endpoint)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var key = $"{tenantId}:{endpoint}";
        var now = DateTime.UtcNow;

        // Clean up old windows periodically
        CleanupOldWindows();

        var window = _rateLimitWindows.AddOrUpdate(
            key,
            _ => new RateLimitWindow
            {
                TenantId = tenantId,
                Endpoint = endpoint,
                WindowStart = now,
                RequestCount = 1,
                Limit = GetRateLimitForEndpoint(endpoint, SubscriptionTier.Pro) // TODO: Get actual tier
            },
            (_, existingWindow) =>
            {
                // If window has expired, start a new one
                if (now >= existingWindow.WindowStart.AddMinutes(1))
                {
                    existingWindow.WindowStart = now;
                    existingWindow.RequestCount = 1;
                }
                else
                {
                    existingWindow.RequestCount++;
                }
                return existingWindow;
            });

        var isAllowed = window.RequestCount <= window.Limit;
        var remaining = Math.Max(0, window.Limit - window.RequestCount);
        var resetTimeSeconds = (int)(window.WindowStart.AddMinutes(1) - now).TotalSeconds;

        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId}, endpoint {Endpoint}: {RequestCount}/{Limit}",
                tenantId, endpoint, window.RequestCount, window.Limit);
        }

        return new RateLimitResult
        {
            IsAllowed = isAllowed,
            RemainingRequests = remaining,
            ResetTimeSeconds = Math.Max(0, resetTimeSeconds),
            Limit = window.Limit
        };
    }

    /// <summary>
    /// Get current rate limit status for a tenant
    /// </summary>
    public async Task<RateLimitStatus> GetRateLimitStatusAsync(Guid tenantId, string endpoint)
    {
        await Task.CompletedTask;

        var key = $"{tenantId}:{endpoint}";
        
        if (_rateLimitWindows.TryGetValue(key, out var window))
        {
            return new RateLimitStatus
            {
                RequestCount = window.RequestCount,
                Limit = window.Limit,
                WindowStart = window.WindowStart,
                WindowEnd = window.WindowStart.AddMinutes(1)
            };
        }

        var limit = GetRateLimitForEndpoint(endpoint, SubscriptionTier.Pro);
        return new RateLimitStatus
        {
            RequestCount = 0,
            Limit = limit,
            WindowStart = DateTime.UtcNow,
            WindowEnd = DateTime.UtcNow.AddMinutes(1)
        };
    }

    private int GetRateLimitForEndpoint(string endpoint, SubscriptionTier tier)
    {
        // Search endpoints have specific rate limits
        if (endpoint.Contains("search", StringComparison.OrdinalIgnoreCase))
        {
            return _searchRateLimits.GetValueOrDefault(tier, 10);
        }

        // Default rate limits for other endpoints
        return tier switch
        {
            SubscriptionTier.Free => 60,
            SubscriptionTier.Pro => 300,
            SubscriptionTier.Enterprise => 1000,
            _ => 60
        };
    }

    private void CleanupOldWindows()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var keysToRemove = _rateLimitWindows
            .Where(kvp => kvp.Value.WindowStart < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _rateLimitWindows.TryRemove(key, out _);
        }
    }

    private class RateLimitWindow
    {
        public Guid TenantId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
        public int Limit { get; set; }
    }
}

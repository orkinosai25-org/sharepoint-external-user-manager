namespace SharePointExternalUserManager.Functions.Services.RateLimiting;

/// <summary>
/// Service interface for rate limiting
/// </summary>
public interface IRateLimitingService
{
    /// <summary>
    /// Check if a request is allowed based on rate limits
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="endpoint">API endpoint identifier</param>
    /// <returns>True if request is allowed, false if rate limit exceeded</returns>
    Task<RateLimitResult> CheckRateLimitAsync(Guid tenantId, string endpoint);

    /// <summary>
    /// Get current rate limit status for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="endpoint">API endpoint identifier</param>
    /// <returns>Rate limit status</returns>
    Task<RateLimitStatus> GetRateLimitStatusAsync(Guid tenantId, string endpoint);
}

/// <summary>
/// Result of a rate limit check
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Remaining requests in the current window
    /// </summary>
    public int RemainingRequests { get; set; }

    /// <summary>
    /// Time until the rate limit resets (in seconds)
    /// </summary>
    public int ResetTimeSeconds { get; set; }

    /// <summary>
    /// Total limit for the window
    /// </summary>
    public int Limit { get; set; }
}

/// <summary>
/// Rate limit status for a tenant
/// </summary>
public class RateLimitStatus
{
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
}

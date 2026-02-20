using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Middleware;

/// <summary>
/// Middleware to enforce per-tenant rate limiting on API requests
/// </summary>
public class TenantRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantRateLimitMiddleware> _logger;

    // Rate limits per plan tier (requests per minute)
    private static readonly Dictionary<string, int> RateLimitsByTier = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Free", 100 },
        { "Starter", 300 },
        { "Pro", 1000 },
        { "Enterprise", 5000 }
    };

    private const int DEFAULT_RATE_LIMIT = 100; // Default for unknown tiers

    // Endpoints that should be excluded from rate limiting
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/swagger",
        "/api-docs"
    };

    public TenantRateLimitMiddleware(RequestDelegate next, ILogger<TenantRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantRateLimitService rateLimitService,
        ApplicationDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip rate limiting for excluded paths
        if (ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Get tenant ID from JWT claims
        var tenantIdClaim = context.User.FindFirst("tid")?.Value;
        
        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            // If no tenant claim, allow the request but log warning
            // This handles unauthenticated endpoints or marketing mode
            _logger.LogDebug("No tenant claim found, skipping rate limiting");
            await _next(context);
            return;
        }

        try
        {
            // Get subscription tier for the tenant
            var tier = await GetTenantTierAsync(dbContext, tenantIdClaim);
            var requestsPerMinute = RateLimitsByTier.GetValueOrDefault(tier, DEFAULT_RATE_LIMIT);

            // Check rate limit
            var result = rateLimitService.CheckRateLimit(tenantIdClaim, requestsPerMinute);

            // Add rate limit headers
            context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = result.RemainingRequests.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(result.ResetTime).ToUnixTimeSeconds().ToString();

            if (!result.IsAllowed)
            {
                // Rate limit exceeded
                var retryAfter = (int)(result.ResetTime - DateTime.UtcNow).TotalSeconds;
                context.Response.Headers["Retry-After"] = Math.Max(1, retryAfter).ToString();

                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "application/json";

                var errorResponse = ApiResponse<object>.ErrorResponse(
                    "RATE_LIMIT_EXCEEDED",
                    $"Rate limit of {result.Limit} requests per minute exceeded. Please try again later.",
                    Guid.NewGuid().ToString()
                );

                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware for tenant {TenantId}", tenantIdClaim);
            // On error, allow the request through to avoid blocking legitimate traffic
            await _next(context);
        }
    }

    private async Task<string> GetTenantTierAsync(ApplicationDbContext dbContext, string entraIdTenantId)
    {
        try
        {
            var tenant = await dbContext.Tenants
                .Include(t => t.Subscriptions)
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == entraIdTenantId);

            if (tenant == null)
            {
                return "Free"; // Default to Free tier if tenant not found
            }

            // Get active subscription
            var subscription = tenant.Subscriptions
                .Where(s => s.Status == "Active" || s.Status == "Trial")
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            return subscription?.Tier ?? "Free";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant tier for {TenantId}", entraIdTenantId);
            return "Free"; // Default to Free tier on error
        }
    }
}

/// <summary>
/// Extension methods for registering rate limiting middleware
/// </summary>
public static class TenantRateLimitMiddlewareExtensions
{
    /// <summary>
    /// Add tenant-based rate limiting middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseTenantRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantRateLimitMiddleware>();
    }
}

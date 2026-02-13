using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Services.RateLimiting;

namespace SharePointExternalUserManager.Functions.Middleware;

/// <summary>
/// Middleware to enforce rate limits on API requests
/// </summary>
public class RateLimitingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IRateLimitingService _rateLimitingService;

    // Endpoints that should have rate limiting applied
    private readonly HashSet<string> _rateLimitedEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "GlobalSearch",
        "ClientSpaceSearch",
        "SearchSuggestions"
    };

    public RateLimitingMiddleware(
        ILogger<RateLimitingMiddleware> logger,
        IRateLimitingService rateLimitingService)
    {
        _logger = logger;
        _rateLimitingService = rateLimitingService;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        
        if (requestData != null)
        {
            var functionName = context.FunctionDefinition.Name;

            // Only apply rate limiting to specific endpoints
            if (_rateLimitedEndpoints.Contains(functionName))
            {
                try
                {
                    // Get tenant ID from context (set by AuthenticationMiddleware)
                    var tenantIdString = context.Items["TenantId"] as string;
                    
                    if (string.IsNullOrEmpty(tenantIdString))
                    {
                        _logger.LogWarning("Tenant ID not found in context for rate limiting");
                        await next(context);
                        return;
                    }

                    var tenantId = Guid.Parse(tenantIdString);

                    // Check rate limit
                    var rateLimitResult = await _rateLimitingService.CheckRateLimitAsync(tenantId, functionName);

                    // Add rate limit headers to response (will be set later)
                    context.Items["RateLimit-Limit"] = rateLimitResult.Limit;
                    context.Items["RateLimit-Remaining"] = rateLimitResult.RemainingRequests;
                    context.Items["RateLimit-Reset"] = rateLimitResult.ResetTimeSeconds;

                    if (!rateLimitResult.IsAllowed)
                    {
                        _logger.LogWarning(
                            "Rate limit exceeded for tenant {TenantId}, function {FunctionName}",
                            tenantId, functionName);
                        
                        await WriteTooManyRequestsResponse(
                            requestData,
                            rateLimitResult.Limit,
                            rateLimitResult.ResetTimeSeconds);
                        return;
                    }

                    _logger.LogDebug(
                        "Rate limit check passed for tenant {TenantId}, function {FunctionName}: {Remaining}/{Limit} remaining",
                        tenantId, functionName, rateLimitResult.RemainingRequests, rateLimitResult.Limit);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rate limiting middleware error for function {FunctionName}", functionName);
                    // Continue on error to avoid blocking legitimate requests
                }
            }
        }

        await next(context);
    }

    private async Task WriteTooManyRequestsResponse(
        HttpRequestData request,
        int limit,
        int resetTimeSeconds)
    {
        var response = request.CreateResponse((System.Net.HttpStatusCode)429); // Too Many Requests
        
        // Add rate limit headers
        response.Headers.Add("X-RateLimit-Limit", limit.ToString());
        response.Headers.Add("X-RateLimit-Remaining", "0");
        response.Headers.Add("X-RateLimit-Reset", resetTimeSeconds.ToString());
        response.Headers.Add("Retry-After", resetTimeSeconds.ToString());

        await response.WriteAsJsonAsync(ApiResponse<object>.ErrorResponse(
            "RATE_LIMIT_EXCEEDED",
            "Too many requests. Please try again later.",
            $"Rate limit of {limit} requests per minute exceeded. Reset in {resetTimeSeconds} seconds."
        ));
        
        var context = request.FunctionContext;
        context.GetInvocationResult().Value = response;
    }
}

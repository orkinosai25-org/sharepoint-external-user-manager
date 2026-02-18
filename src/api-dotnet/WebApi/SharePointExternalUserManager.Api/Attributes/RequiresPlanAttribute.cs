using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Attributes;

/// <summary>
/// Attribute to enforce feature gating based on subscription tier
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresPlanAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _minimumTier;
    private readonly string? _featureName;

    /// <summary>
    /// Initializes a new instance of the RequiresPlanAttribute
    /// </summary>
    /// <param name="minimumTier">Minimum plan tier required (Free, Starter, Pro)</param>
    /// <param name="featureName">Optional feature name for error messages</param>
    public RequiresPlanAttribute(string minimumTier, string? featureName = null)
    {
        _minimumTier = minimumTier;
        _featureName = featureName;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get services from DI
        var dbContext = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
        var logger = context.HttpContext.RequestServices.GetService<ILogger<RequiresPlanAttribute>>();

        if (dbContext == null)
        {
            context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                "INTERNAL_ERROR",
                "Database context not available"))
            {
                StatusCode = 500
            };
            return;
        }

        // Get tenant ID from claims
        var tenantIdClaim = context.HttpContext.User.FindFirst("tid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.ErrorResponse(
                "AUTH_ERROR",
                "Missing tenant claim"));
            return;
        }

        // Get tenant and subscription from database
        var tenant = await dbContext.Tenants
            .Include(t => t.Subscriptions)
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
        {
            context.Result = new NotFoundObjectResult(ApiResponse<object>.ErrorResponse(
                "TENANT_NOT_FOUND",
                "Tenant not found"));
            return;
        }

        var subscription = tenant.Subscriptions.FirstOrDefault();
        if (subscription == null)
        {
            context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                "NO_SUBSCRIPTION",
                "No active subscription found"))
            {
                StatusCode = 403
            };
            return;
        }

        // Check if subscription tier meets minimum requirement
        var currentTier = subscription.Tier;
        if (!IsTierSufficient(currentTier, _minimumTier))
        {
            var featureMessage = !string.IsNullOrEmpty(_featureName) 
                ? $"Feature '{_featureName}' requires {_minimumTier} plan or higher"
                : $"This operation requires {_minimumTier} plan or higher";

            logger?.LogWarning(
                "Feature gate blocked: Tenant {TenantId} with {CurrentTier} tier attempted to access {MinimumTier} feature",
                tenantIdClaim, currentTier, _minimumTier);

            context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                "UPGRADE_REQUIRED",
                $"{featureMessage}. Current plan: {currentTier}"))
            {
                StatusCode = 403
            };
            return;
        }

        // Check if subscription is active
        if (subscription.Status != "Active" && subscription.Status != "Trial")
        {
            context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                "SUBSCRIPTION_INACTIVE",
                $"Subscription is {subscription.Status}. Please renew your subscription."))
            {
                StatusCode = 403
            };
            return;
        }

        // Check trial expiry
        if (subscription.Status == "Trial" && subscription.TrialExpiry.HasValue)
        {
            if (subscription.TrialExpiry.Value < DateTime.UtcNow)
            {
                context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                    "TRIAL_EXPIRED",
                    "Your trial has expired. Please upgrade to a paid plan."))
                {
                    StatusCode = 403
                };
                return;
            }
        }

        // All checks passed, continue with the action
        await next();
    }

    private static bool IsTierSufficient(string currentTier, string requiredTier)
    {
        var tierOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Free", 0 },
            { "Starter", 1 },
            { "Pro", 2 },
            { "Enterprise", 3 }
        };

        var currentLevel = tierOrder.GetValueOrDefault(currentTier, 0);
        var requiredLevel = tierOrder.GetValueOrDefault(requiredTier, 0);

        return currentLevel >= requiredLevel;
    }
}

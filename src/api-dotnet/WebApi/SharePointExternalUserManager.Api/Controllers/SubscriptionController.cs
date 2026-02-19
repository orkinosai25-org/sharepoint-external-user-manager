using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;
using ApiSubscriptionTier = SharePointExternalUserManager.Api.Models.SubscriptionTier;

namespace SharePointExternalUserManager.Api.Controllers;

/// <summary>
/// Controller for subscription management operations
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        ApplicationDbContext context,
        IStripeService stripeService,
        IAuditLogService auditLogService,
        ILogger<SubscriptionController> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get current subscription details for the authenticated user's tenant
    /// </summary>
    /// <returns>Current subscription information including plan, status, limits, and features</returns>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<SubscriptionStatusResponse>>> GetMySubscription()
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            // Get tenant ID from claims
            var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value
                ?? User.FindFirst("tid")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("Tenant ID not found in token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<SubscriptionStatusResponse>.ErrorResponse(
                    "AUTH_ERROR", 
                    "Tenant ID not found in token", 
                    correlationId));
            }

            // Get tenant with subscription
            var tenant = await _context.Tenants
                .Include(t => t.Subscriptions)
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);

            if (tenant == null)
            {
                return NotFound(ApiResponse<SubscriptionStatusResponse>.ErrorResponse(
                    "TENANT_NOT_FOUND", 
                    "Tenant not found", 
                    correlationId));
            }

            // Get active subscription
            var subscription = tenant.Subscriptions
                .Where(s => s.Status == "Active" || s.Status == "Trial")
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            if (subscription == null)
            {
                // Return default starter plan
                var defaultPlan = PlanConfiguration.GetPlanDefinition(ApiSubscriptionTier.Starter);
                return Ok(ApiResponse<SubscriptionStatusResponse>.SuccessResponse(new SubscriptionStatusResponse
                {
                    Tier = "Starter",
                    Status = "None",
                    IsActive = false,
                    Limits = defaultPlan.Limits,
                    Features = defaultPlan.Features
                }));
            }

            // Get plan definition
            var planDef = PlanConfiguration.GetPlanDefinitionByName(subscription.Tier)
                ?? PlanConfiguration.GetPlanDefinition(ApiSubscriptionTier.Starter);

            var response = new SubscriptionStatusResponse
            {
                Tier = subscription.Tier,
                Status = subscription.Status,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                TrialExpiry = subscription.TrialExpiry,
                IsActive = subscription.Status == "Active" || subscription.Status == "Trial",
                StripeSubscriptionId = subscription.StripeSubscriptionId,
                StripeCustomerId = subscription.StripeCustomerId,
                Limits = planDef.Limits,
                Features = planDef.Features
            };

            return Ok(ApiResponse<SubscriptionStatusResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, ApiResponse<SubscriptionStatusResponse>.ErrorResponse(
                "INTERNAL_ERROR",
                "Failed to retrieve subscription status. Please try again later.",
                correlationId));
        }
    }

    /// <summary>
    /// Change subscription plan (upgrade or downgrade)
    /// </summary>
    /// <param name="request">Request containing new plan tier</param>
    /// <returns>Success response or error if plan change fails</returns>
    [HttpPost("change-plan")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePlan([FromBody] ChangePlanRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            // Get tenant ID from claims
            var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value
                ?? User.FindFirst("tid")?.Value;
            var userId = User.FindFirst("oid")?.Value;
            var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("Tenant ID not found in token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "AUTH_ERROR",
                    "Tenant ID not found in token",
                    correlationId));
            }

            // Validate new plan tier
            if (!Enum.TryParse<ApiSubscriptionTier>(request.NewPlanTier, out var newTier))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "INVALID_PLAN",
                    $"Invalid plan tier: {request.NewPlanTier}",
                    correlationId));
            }

            // Enterprise plans require custom sales process
            if (newTier == ApiSubscriptionTier.Enterprise)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "ENTERPRISE_REQUIRES_SALES",
                    "Enterprise plans require custom pricing. Please contact sales.",
                    correlationId));
            }

            // Get tenant with subscription
            var tenant = await _context.Tenants
                .Include(t => t.Subscriptions)
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);

            if (tenant == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "TENANT_NOT_FOUND",
                    "Tenant not found",
                    correlationId));
            }

            // Get current subscription
            var currentSubscription = tenant.Subscriptions
                .Where(s => s.Status == "Active" || s.Status == "Trial")
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            if (currentSubscription == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "NO_ACTIVE_SUBSCRIPTION",
                    "No active subscription found. Please subscribe first.",
                    correlationId));
            }

            // Check if already on the requested plan
            if (currentSubscription.Tier.Equals(request.NewPlanTier, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "ALREADY_ON_PLAN",
                    $"You are already subscribed to the {request.NewPlanTier} plan.",
                    correlationId));
            }

            // If user has a Stripe subscription, update it via Stripe
            if (!string.IsNullOrEmpty(currentSubscription.StripeSubscriptionId))
            {
                // Note: This requires StripeService to have an UpdateSubscriptionAsync method
                // For now, return a message directing them to create a new checkout session
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "USE_CHECKOUT",
                    "Please use the checkout process to change your plan. Cancel your current subscription first.",
                    correlationId));
            }

            // Update subscription tier directly (for trial/free plans)
            currentSubscription.Tier = request.NewPlanTier;
            currentSubscription.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log the action
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId,
                userEmail,
                "PLAN_CHANGED",
                "Subscription",
                currentSubscription.Id.ToString(),
                $"Plan changed to {request.NewPlanTier}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                correlationId,
                "Success");

            _logger.LogInformation(
                "Plan changed for tenant {TenantId} to {NewPlan}. CorrelationId: {CorrelationId}",
                tenantId, request.NewPlanTier, correlationId);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                message = $"Successfully changed plan to {request.NewPlanTier}",
                newTier = request.NewPlanTier
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change plan. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "INTERNAL_ERROR",
                "Failed to change plan. Please try again later.",
                correlationId));
        }
    }

    /// <summary>
    /// Cancel active subscription
    /// </summary>
    /// <returns>Success response or error if cancellation fails</returns>
    [HttpPost("cancel")]
    public async Task<ActionResult<ApiResponse<object>>> CancelSubscription()
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            // Get tenant ID from claims
            var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value
                ?? User.FindFirst("tid")?.Value;
            var userId = User.FindFirst("oid")?.Value;
            var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("Tenant ID not found in token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "AUTH_ERROR",
                    "Tenant ID not found in token",
                    correlationId));
            }

            // Get tenant with subscription
            var tenant = await _context.Tenants
                .Include(t => t.Subscriptions)
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);

            if (tenant == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "TENANT_NOT_FOUND",
                    "Tenant not found",
                    correlationId));
            }

            // Get current subscription
            var currentSubscription = tenant.Subscriptions
                .Where(s => s.Status == "Active" || s.Status == "Trial")
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            if (currentSubscription == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "NO_ACTIVE_SUBSCRIPTION",
                    "No active subscription found.",
                    correlationId));
            }

            // If user has a Stripe subscription, cancel it via Stripe
            if (!string.IsNullOrEmpty(currentSubscription.StripeSubscriptionId))
            {
                try
                {
                    await _stripeService.CancelSubscriptionAsync(currentSubscription.StripeSubscriptionId);
                    
                    _logger.LogInformation(
                        "Stripe subscription {SubscriptionId} cancelled. CorrelationId: {CorrelationId}",
                        currentSubscription.StripeSubscriptionId, correlationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to cancel Stripe subscription {SubscriptionId}. CorrelationId: {CorrelationId}",
                        currentSubscription.StripeSubscriptionId, correlationId);
                    
                    return StatusCode(500, ApiResponse<object>.ErrorResponse(
                        "STRIPE_ERROR",
                        "Failed to cancel subscription with payment provider.",
                        correlationId));
                }
            }

            // Update subscription status
            currentSubscription.Status = "Cancelled";
            currentSubscription.EndDate = DateTime.UtcNow;
            currentSubscription.GracePeriodEnd = DateTime.UtcNow.AddDays(7); // 7-day grace period
            currentSubscription.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log the action
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId,
                userEmail,
                "SUBSCRIPTION_CANCELLED",
                "Subscription",
                currentSubscription.Id.ToString(),
                "Subscription cancelled by user",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                correlationId,
                "Success");

            _logger.LogInformation(
                "Subscription cancelled for tenant {TenantId}. CorrelationId: {CorrelationId}",
                tenantId, correlationId);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                message = "Subscription cancelled successfully",
                gracePeriodEnd = currentSubscription.GracePeriodEnd,
                gracePeriodDays = 7
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "INTERNAL_ERROR",
                "Failed to cancel subscription. Please try again later.",
                correlationId));
        }
    }
}

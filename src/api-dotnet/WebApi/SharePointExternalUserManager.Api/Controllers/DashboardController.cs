using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Controllers;

/// <summary>
/// Controller for dashboard operations and aggregated statistics
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISharePointService _sharePointService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ApplicationDbContext context,
        ISharePointService sharePointService,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _sharePointService = sharePointService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard summary with aggregated statistics for the authenticated tenant
    /// </summary>
    /// <returns>Dashboard summary with client counts, user counts, subscription info, and quick actions</returns>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            _logger.LogWarning("Missing tenant claim. CorrelationId: {CorrelationId}", correlationId);
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));
        }

        try
        {
            var startTime = DateTime.UtcNow;

            // Get the internal tenant ID from the database
            var tenant = await _context.Tenants
                .Include(t => t.Subscriptions)
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

            if (tenant == null)
            {
                _logger.LogWarning(
                    "Tenant not found for Entra ID: {TenantId}. CorrelationId: {CorrelationId}",
                    tenantIdClaim,
                    correlationId);
                return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found in database"));
            }

            // Get active subscription
            var subscription = tenant.Subscriptions
                .Where(s => s.Status == "Active" || s.Status == "Trial")
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            // Get plan definition
            var planDef = subscription != null
                ? PlanConfiguration.GetPlanDefinitionByName(subscription.Tier) ?? PlanConfiguration.GetPlanDefinition(Api.Models.SubscriptionTier.Starter)
                : PlanConfiguration.GetPlanDefinition(Api.Models.SubscriptionTier.Starter);

            // Get all active clients for this tenant
            var clients = await _context.Clients
                .Where(c => c.TenantId == tenant.Id && c.IsActive)
                .ToListAsync();

            var totalClientSpaces = clients.Count;

            // Get external users count across all provisioned client sites
            var totalExternalUsers = 0;
            var activeInvitations = 0;

            foreach (var client in clients.Where(c => !string.IsNullOrEmpty(c.SharePointSiteId)))
            {
                try
                {
                    var externalUsers = await _sharePointService.GetExternalUsersAsync(client.SharePointSiteId!);
                    totalExternalUsers += externalUsers.Count;

                    // Count active invitations (users with "PendingAcceptance" status)
                    activeInvitations += externalUsers.Count(u =>
                        u.Status?.Equals("PendingAcceptance", StringComparison.OrdinalIgnoreCase) == true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to get external users for client {ClientId}. CorrelationId: {CorrelationId}",
                        client.Id,
                        correlationId);
                    // Continue with other clients even if one fails
                }
            }

            // Calculate trial days remaining
            int? trialDaysRemaining = null;
            DateTime? trialExpiryDate = null;

            if (subscription?.Status == "Trial" && subscription.TrialExpiry.HasValue)
            {
                trialExpiryDate = subscription.TrialExpiry.Value;
                var daysRemaining = (trialExpiryDate.Value - DateTime.UtcNow).TotalDays;
                trialDaysRemaining = (int)Math.Max(0, Math.Ceiling(daysRemaining));
            }

            // Calculate usage percentages
            int? clientSpacesUsagePercent = null;
            int? externalUsersUsagePercent = null;

            if (planDef.Limits.MaxClientSpaces.HasValue && planDef.Limits.MaxClientSpaces > 0)
            {
                clientSpacesUsagePercent = (int)((totalClientSpaces / (double)planDef.Limits.MaxClientSpaces.Value) * 100);
            }

            if (planDef.Limits.MaxExternalUsers.HasValue && planDef.Limits.MaxExternalUsers > 0)
            {
                externalUsersUsagePercent = (int)((totalExternalUsers / (double)planDef.Limits.MaxExternalUsers.Value) * 100);
            }

            // Build quick actions based on current state
            var quickActions = BuildQuickActions(
                subscription,
                totalClientSpaces,
                planDef.Limits.MaxClientSpaces,
                trialDaysRemaining);

            var response = new DashboardSummaryResponse
            {
                TotalClientSpaces = totalClientSpaces,
                TotalExternalUsers = totalExternalUsers,
                ActiveInvitations = activeInvitations,
                PlanTier = subscription?.Tier ?? "Free",
                Status = subscription?.Status ?? "None",
                TrialDaysRemaining = trialDaysRemaining,
                TrialExpiryDate = trialExpiryDate,
                IsActive = subscription?.Status == "Active" || subscription?.Status == "Trial",
                Limits = new PlanLimitsDto
                {
                    MaxClientSpaces = planDef.Limits.MaxClientSpaces,
                    MaxExternalUsers = planDef.Limits.MaxExternalUsers,
                    MaxStorageGB = null, // Not tracked in current plan limits
                    ClientSpacesUsagePercent = clientSpacesUsagePercent,
                    ExternalUsersUsagePercent = externalUsersUsagePercent
                },
                QuickActions = quickActions
            };

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "Dashboard summary generated in {Duration}ms for tenant {TenantId}. CorrelationId: {CorrelationId}",
                duration,
                tenant.Id,
                correlationId);

            return Ok(ApiResponse<DashboardSummaryResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate dashboard summary. CorrelationId: {CorrelationId}",
                correlationId);

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "INTERNAL_ERROR",
                "An error occurred while generating the dashboard summary. Please try again later.",
                correlationId));
        }
    }

    /// <summary>
    /// Build quick actions based on subscription and usage state
    /// </summary>
    private List<QuickActionDto> BuildQuickActions(
        Data.Entities.SubscriptionEntity? subscription,
        int totalClientSpaces,
        int? maxClientSpaces,
        int? trialDaysRemaining)
    {
        var actions = new List<QuickActionDto>();

        // Action: Create Client Space (if within limits)
        var canCreateClient = !maxClientSpaces.HasValue || totalClientSpaces < maxClientSpaces.Value;
        
        if (canCreateClient)
        {
            actions.Add(new QuickActionDto
            {
                Id = "create-client",
                Label = "Create Client Space",
                Description = "Add a new client space to manage external users and documents",
                Action = "/dashboard",
                Type = "modal",
                Priority = "primary",
                Icon = "plus-circle"
            });
        }
        else
        {
            actions.Add(new QuickActionDto
            {
                Id = "upgrade-for-clients",
                Label = "Upgrade to Add More Clients",
                Description = $"You've reached your limit of {maxClientSpaces} client spaces",
                Action = "/pricing",
                Type = "navigate",
                Priority = "warning",
                Icon = "arrow-up-circle"
            });
        }

        // Action: View Expiring Trial (if trial expires soon)
        if (trialDaysRemaining.HasValue && trialDaysRemaining.Value <= 7)
        {
            actions.Add(new QuickActionDto
            {
                Id = "trial-expiring",
                Label = "Trial Expiring Soon",
                Description = $"Only {trialDaysRemaining} days left. Upgrade to continue",
                Action = "/pricing",
                Type = "navigate",
                Priority = "warning",
                Icon = "exclamation-triangle"
            });
        }

        // Action: Upgrade Plan (if on Free or trial)
        if (subscription?.Tier == "Free" || subscription?.Status == "Trial")
        {
            actions.Add(new QuickActionDto
            {
                Id = "upgrade-plan",
                Label = "Upgrade Plan",
                Description = "Unlock more features and increase limits",
                Action = "/pricing",
                Type = "navigate",
                Priority = "secondary",
                Icon = "star"
            });
        }

        // Action: Configure SharePoint (if no clients yet)
        if (totalClientSpaces == 0)
        {
            actions.Add(new QuickActionDto
            {
                Id = "getting-started",
                Label = "Getting Started Guide",
                Description = "Learn how to set up your first client space",
                Action = "/docs/getting-started",
                Type = "external",
                Priority = "secondary",
                Icon = "book"
            });
        }

        return actions;
    }
}

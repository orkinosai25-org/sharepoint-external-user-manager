using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service for enforcing plan limits and feature access
/// </summary>
public interface IPlanEnforcementService
{
    /// <summary>
    /// Get the active plan for a tenant
    /// </summary>
    Task<PlanDefinition?> GetTenantPlanAsync(int tenantId);

    /// <summary>
    /// Check if a tenant has access to a feature
    /// </summary>
    Task<bool> HasFeatureAccessAsync(int tenantId, string featureName);

    /// <summary>
    /// Check if a tenant can create more client spaces
    /// </summary>
    Task<(bool Allowed, int Current, int? Limit)> CanCreateClientSpaceAsync(int tenantId);

    /// <summary>
    /// Check if a tenant can add more external users to a client
    /// </summary>
    Task<(bool Allowed, int Current, int? Limit)> CanAddExternalUserAsync(int tenantId, int clientId);

    /// <summary>
    /// Enforce feature access (throws if not allowed)
    /// </summary>
    Task EnforceFeatureAccessAsync(int tenantId, string featureName);

    /// <summary>
    /// Enforce client space limit (throws if exceeded)
    /// </summary>
    Task EnforceClientSpaceLimitAsync(int tenantId);
}

public class PlanEnforcementService : IPlanEnforcementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlanEnforcementService> _logger;

    public PlanEnforcementService(
        ApplicationDbContext context,
        ILogger<PlanEnforcementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PlanDefinition?> GetTenantPlanAsync(int tenantId)
    {
        var subscription = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && 
                   (s.Status == "Active" || s.Status == "Trial"))
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            // Default to Starter plan
            return PlanConfiguration.GetPlanDefinition(SubscriptionTier.Starter);
        }

        var plan = PlanConfiguration.GetPlanDefinitionByName(subscription.Tier);
        return plan ?? PlanConfiguration.GetPlanDefinition(SubscriptionTier.Starter);
    }

    public async Task<bool> HasFeatureAccessAsync(int tenantId, string featureName)
    {
        var plan = await GetTenantPlanAsync(tenantId);
        if (plan == null) return false;

        var property = typeof(PlanFeatures).GetProperty(featureName);
        if (property == null)
        {
            _logger.LogWarning("Unknown feature: {FeatureName}", featureName);
            return false;
        }

        return (bool)(property.GetValue(plan.Features) ?? false);
    }

    public async Task<(bool Allowed, int Current, int? Limit)> CanCreateClientSpaceAsync(int tenantId)
    {
        var plan = await GetTenantPlanAsync(tenantId);
        if (plan == null)
        {
            return (false, 0, 0);
        }

        var currentCount = await _context.Clients.CountAsync(c => c.TenantId == tenantId);

        // Enterprise has unlimited client spaces
        if (plan.Limits.IsUnlimited || plan.Limits.MaxClientSpaces == null)
        {
            return (true, currentCount, null);
        }

        var limit = plan.Limits.MaxClientSpaces.Value;
        return (currentCount < limit, currentCount, limit);
    }

    public async Task<(bool Allowed, int Current, int? Limit)> CanAddExternalUserAsync(int tenantId, int clientId)
    {
        // TODO: This method requires external user tracking to be implemented
        // For now, we cannot accurately enforce external user limits
        throw new NotImplementedException(
            "External user limit enforcement requires external user tracking to be implemented first. " +
            "This will be addressed when the external user management system is integrated.");
    }

    public async Task EnforceFeatureAccessAsync(int tenantId, string featureName)
    {
        var hasAccess = await HasFeatureAccessAsync(tenantId, featureName);
        
        if (!hasAccess)
        {
            var plan = await GetTenantPlanAsync(tenantId);
            _logger.LogWarning(
                "Feature access denied. TenantId: {TenantId}, Feature: {FeatureName}, Plan: {PlanName}",
                tenantId, featureName, plan?.Name ?? "Unknown");
            
            throw new UnauthorizedAccessException(
                $"Your {plan?.Name ?? "current"} plan does not include access to this feature. " +
                $"Please upgrade your subscription to use {featureName}.");
        }
    }

    public async Task EnforceClientSpaceLimitAsync(int tenantId)
    {
        var (allowed, current, limit) = await CanCreateClientSpaceAsync(tenantId);
        
        if (!allowed)
        {
            var plan = await GetTenantPlanAsync(tenantId);
            _logger.LogWarning(
                "Client space limit exceeded. TenantId: {TenantId}, Current: {Current}, Limit: {Limit}, Plan: {PlanName}",
                tenantId, current, limit, plan?.Name ?? "Unknown");
            
            throw new InvalidOperationException(
                $"You have reached the maximum number of client spaces ({limit}) for your {plan?.Name ?? "current"} plan. " +
                $"Please upgrade your subscription to create more client spaces.");
        }
    }
}

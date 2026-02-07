namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Static configuration for all available subscription plans
/// </summary>
public static class PlanConfiguration
{
    /// <summary>
    /// Complete definitions for all plan tiers
    /// </summary>
    public static readonly Dictionary<SubscriptionTier, PlanDefinition> Plans = new()
    {
        [SubscriptionTier.Starter] = new PlanDefinition
        {
            Tier = SubscriptionTier.Starter,
            Name = "Starter",
            Description = "Perfect for small teams getting started with external user management",
            MonthlyPrice = 29.00m,
            AnnualPrice = 290.00m,
            Limits = new PlanLimits
            {
                MaxClientSpaces = 5,
                MaxExternalUsers = 50,
                MaxLibraries = 25,
                MaxAdmins = 2,
                AuditRetentionDays = 30,
                MaxApiCallsPerMonth = 10000,
                SupportLevel = "Community",
                IsUnlimited = false
            },
            Features = new PlanFeatures
            {
                BasicUserManagement = true,
                LibraryAccessControl = true,
                BasicAuditLogs = true,
                EmailNotifications = true,
                AuditExport = false,
                BulkOperations = false,
                CustomPolicies = false,
                ApiAccess = false,
                AdvancedPermissions = false,
                AdvancedReporting = false,
                ScheduledReviews = false,
                SsoIntegration = false,
                PrioritySupport = false,
                EnhancedAudit = false,
                CustomBranding = false,
                DedicatedSupport = false,
                SlaGuarantees = false,
                AdvancedSecurity = false
            }
        },
        [SubscriptionTier.Professional] = new PlanDefinition
        {
            Tier = SubscriptionTier.Professional,
            Name = "Professional",
            Description = "Advanced features for growing businesses",
            MonthlyPrice = 99.00m,
            AnnualPrice = 990.00m,
            Limits = new PlanLimits
            {
                MaxClientSpaces = 20,
                MaxExternalUsers = 250,
                MaxLibraries = 100,
                MaxAdmins = 5,
                AuditRetentionDays = 90,
                MaxApiCallsPerMonth = 50000,
                SupportLevel = "Email",
                IsUnlimited = false
            },
            Features = new PlanFeatures
            {
                BasicUserManagement = true,
                LibraryAccessControl = true,
                BasicAuditLogs = true,
                EmailNotifications = true,
                AuditExport = true,
                BulkOperations = true,
                CustomPolicies = true,
                ApiAccess = true,
                AdvancedPermissions = true,
                AdvancedReporting = false,
                ScheduledReviews = false,
                SsoIntegration = false,
                PrioritySupport = false,
                EnhancedAudit = false,
                CustomBranding = false,
                DedicatedSupport = false,
                SlaGuarantees = false,
                AdvancedSecurity = false
            }
        },
        [SubscriptionTier.Business] = new PlanDefinition
        {
            Tier = SubscriptionTier.Business,
            Name = "Business",
            Description = "Comprehensive solution for established organisations",
            MonthlyPrice = 299.00m,
            AnnualPrice = 2990.00m,
            Limits = new PlanLimits
            {
                MaxClientSpaces = 100,
                MaxExternalUsers = 1000,
                MaxLibraries = 500,
                MaxAdmins = 15,
                AuditRetentionDays = 365,
                MaxApiCallsPerMonth = 250000,
                SupportLevel = "Priority",
                IsUnlimited = false
            },
            Features = new PlanFeatures
            {
                BasicUserManagement = true,
                LibraryAccessControl = true,
                BasicAuditLogs = true,
                EmailNotifications = true,
                AuditExport = true,
                BulkOperations = true,
                CustomPolicies = true,
                ApiAccess = true,
                AdvancedPermissions = true,
                AdvancedReporting = true,
                ScheduledReviews = true,
                SsoIntegration = true,
                PrioritySupport = true,
                EnhancedAudit = true,
                CustomBranding = false,
                DedicatedSupport = false,
                SlaGuarantees = false,
                AdvancedSecurity = false
            }
        },
        [SubscriptionTier.Enterprise] = new PlanDefinition
        {
            Tier = SubscriptionTier.Enterprise,
            Name = "Enterprise",
            Description = "Custom solution for large organisations requiring unlimited resources",
            MonthlyPrice = 999.00m,
            AnnualPrice = 9990.00m,
            Limits = new PlanLimits
            {
                MaxClientSpaces = null, // Unlimited
                MaxExternalUsers = null, // Unlimited
                MaxLibraries = null, // Unlimited
                MaxAdmins = 999,
                AuditRetentionDays = null, // Unlimited
                MaxApiCallsPerMonth = null, // Unlimited
                SupportLevel = "Dedicated",
                IsUnlimited = true
            },
            Features = new PlanFeatures
            {
                BasicUserManagement = true,
                LibraryAccessControl = true,
                BasicAuditLogs = true,
                EmailNotifications = true,
                AuditExport = true,
                BulkOperations = true,
                CustomPolicies = true,
                ApiAccess = true,
                AdvancedPermissions = true,
                AdvancedReporting = true,
                ScheduledReviews = true,
                SsoIntegration = true,
                PrioritySupport = true,
                EnhancedAudit = true,
                CustomBranding = true,
                DedicatedSupport = true,
                SlaGuarantees = true,
                AdvancedSecurity = true
            }
        }
    };

    /// <summary>
    /// Get plan definition by tier
    /// </summary>
    public static PlanDefinition GetPlanDefinition(SubscriptionTier tier)
    {
        return Plans[tier];
    }

    /// <summary>
    /// Get plan definition by tier name (case-insensitive)
    /// </summary>
    public static PlanDefinition? GetPlanDefinitionByName(string tierName)
    {
        if (Enum.TryParse<SubscriptionTier>(tierName, ignoreCase: true, out var tier))
        {
            return Plans[tier];
        }
        return null;
    }

    /// <summary>
    /// Check if a feature is available for a plan tier
    /// </summary>
    public static bool HasFeature(SubscriptionTier tier, string featureName)
    {
        var plan = Plans[tier];
        var property = typeof(PlanFeatures).GetProperty(featureName);
        return property != null && (bool)(property.GetValue(plan.Features) ?? false);
    }

    /// <summary>
    /// Get all available plans (excluding Enterprise for self-service)
    /// </summary>
    public static List<PlanDefinition> GetAvailablePlans(bool includeEnterprise = false)
    {
        return Plans.Values
            .Where(p => includeEnterprise || p.Tier != SubscriptionTier.Enterprise)
            .OrderBy(p => p.MonthlyPrice)
            .ToList();
    }
}

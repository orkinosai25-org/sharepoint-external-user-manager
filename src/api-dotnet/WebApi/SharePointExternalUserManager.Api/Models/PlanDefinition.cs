namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Complete definition of a subscription plan including pricing, limits and features
/// </summary>
public class PlanDefinition
{
    public SubscriptionTier Tier { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public PlanLimits Limits { get; set; } = new();
    public PlanFeatures Features { get; set; } = new();
}

/// <summary>
/// Resource limits for a plan tier
/// </summary>
public class PlanLimits
{
    public int? MaxClientSpaces { get; set; }
    public int? MaxExternalUsers { get; set; }
    public int? MaxLibraries { get; set; }
    public int? MaxAdmins { get; set; }
    public int? AuditRetentionDays { get; set; }
    public int? MaxApiCallsPerMonth { get; set; }
    public int? MaxAiMessagesPerMonth { get; set; }
    public string SupportLevel { get; set; } = "Community";
    
    /// <summary>
    /// Enterprise plan has unlimited resources
    /// </summary>
    public bool IsUnlimited { get; set; }
}

/// <summary>
/// Feature flags for a plan tier
/// </summary>
public class PlanFeatures
{
    public bool BasicUserManagement { get; set; }
    public bool LibraryAccessControl { get; set; }
    public bool BasicAuditLogs { get; set; }
    public bool EmailNotifications { get; set; }
    public bool AuditExport { get; set; }
    public bool BulkOperations { get; set; }
    public bool CustomPolicies { get; set; }
    public bool ApiAccess { get; set; }
    public bool AdvancedPermissions { get; set; }
    public bool AdvancedReporting { get; set; }
    public bool ScheduledReviews { get; set; }
    public bool SsoIntegration { get; set; }
    public bool PrioritySupport { get; set; }
    public bool EnhancedAudit { get; set; }
    public bool CustomBranding { get; set; }
    public bool DedicatedSupport { get; set; }
    public bool SlaGuarantees { get; set; }
    public bool AdvancedSecurity { get; set; }
    public bool GlobalSearch { get; set; }
}

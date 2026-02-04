namespace SharePointExternalUserManager.Functions.Models;

public class Tenant
{
    public Guid TenantId { get; set; }
    public string TenantDomain { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EntraIdTenantId { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public SubscriptionTier SubscriptionTier { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}

public enum SubscriptionTier
{
    Free,
    Pro,
    Enterprise
}

public enum SubscriptionStatus
{
    Active,
    Trial,
    Expired,
    Suspended,
    Cancelled
}

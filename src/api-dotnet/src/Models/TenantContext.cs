namespace SharePointExternalUserManager.Functions.Models;

public class TenantContext
{
    public Guid TenantId { get; set; }
    public string TenantDomain { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public SubscriptionTier SubscriptionTier { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public string UserPrincipalName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

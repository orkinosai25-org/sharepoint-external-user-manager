namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Request to register/onboard a new tenant
/// </summary>
public class TenantRegistrationRequest
{
    /// <summary>
    /// Organization name for the tenant
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Primary admin email address
    /// </summary>
    public string PrimaryAdminEmail { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant settings
    /// </summary>
    public TenantSettings? Settings { get; set; }
}

/// <summary>
/// Tenant settings configuration
/// </summary>
public class TenantSettings
{
    /// <summary>
    /// SharePoint tenant URL (e.g., contoso.sharepoint.com)
    /// </summary>
    public string? SharePointTenantUrl { get; set; }

    /// <summary>
    /// Enable external sharing by default for new sites
    /// </summary>
    public bool EnableExternalSharingDefault { get; set; } = true;

    /// <summary>
    /// Default permission level for external users
    /// </summary>
    public string DefaultExternalPermission { get; set; } = "Read";
}

/// <summary>
/// Response after successful tenant registration
/// </summary>
public class TenantRegistrationResponse
{
    /// <summary>
    /// Internal tenant ID
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Entra ID tenant identifier
    /// </summary>
    public string EntraIdTenantId { get; set; } = string.Empty;

    /// <summary>
    /// Organization name
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Subscription tier
    /// </summary>
    public string SubscriptionTier { get; set; } = "Free";

    /// <summary>
    /// Trial expiry date (30 days from registration)
    /// </summary>
    public DateTime? TrialExpiryDate { get; set; }

    /// <summary>
    /// Registration date
    /// </summary>
    public DateTime RegisteredDate { get; set; }

    /// <summary>
    /// Next steps for the admin to complete setup
    /// </summary>
    public List<string> NextSteps { get; set; } = new()
    {
        "Configure SharePoint tenant URL in settings",
        "Install SPFx web parts from App Catalog",
        "Create your first client space"
    };
}

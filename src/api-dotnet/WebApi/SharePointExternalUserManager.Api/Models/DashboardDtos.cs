namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Dashboard summary response containing aggregated statistics
/// </summary>
public class DashboardSummaryResponse
{
    /// <summary>
    /// Total number of client spaces for the tenant
    /// </summary>
    public int TotalClientSpaces { get; set; }

    /// <summary>
    /// Total number of external users across all client spaces
    /// </summary>
    public int TotalExternalUsers { get; set; }

    /// <summary>
    /// Number of active (pending) invitations
    /// </summary>
    public int ActiveInvitations { get; set; }

    /// <summary>
    /// Current subscription plan tier
    /// </summary>
    public string PlanTier { get; set; } = string.Empty;

    /// <summary>
    /// Subscription status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Number of days remaining in trial (null if not in trial)
    /// </summary>
    public int? TrialDaysRemaining { get; set; }

    /// <summary>
    /// Trial expiry date (null if not in trial)
    /// </summary>
    public DateTime? TrialExpiryDate { get; set; }

    /// <summary>
    /// Whether the subscription is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Plan limits
    /// </summary>
    public PlanLimitsDto Limits { get; set; } = new();

    /// <summary>
    /// Quick action suggestions based on current state
    /// </summary>
    public List<QuickActionDto> QuickActions { get; set; } = new();
}

/// <summary>
/// Plan limits DTO
/// </summary>
public class PlanLimitsDto
{
    /// <summary>
    /// Maximum number of client spaces (null = unlimited)
    /// </summary>
    public int? MaxClientSpaces { get; set; }

    /// <summary>
    /// Maximum number of external users (null = unlimited)
    /// </summary>
    public int? MaxExternalUsers { get; set; }

    /// <summary>
    /// Maximum storage in GB (null = unlimited)
    /// </summary>
    public int? MaxStorageGB { get; set; }

    /// <summary>
    /// Current usage percentage for client spaces (0-100)
    /// </summary>
    public int? ClientSpacesUsagePercent { get; set; }

    /// <summary>
    /// Current usage percentage for external users (0-100)
    /// </summary>
    public int? ExternalUsersUsagePercent { get; set; }
}

/// <summary>
/// Quick action suggestion
/// </summary>
public class QuickActionDto
{
    /// <summary>
    /// Action identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display label
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Action description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Navigation URL or route
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Action type (navigate, modal, external)
    /// </summary>
    public string Type { get; set; } = "navigate";

    /// <summary>
    /// Visual priority (primary, secondary, warning)
    /// </summary>
    public string Priority { get; set; } = "secondary";

    /// <summary>
    /// Icon name (Bootstrap Icons)
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}

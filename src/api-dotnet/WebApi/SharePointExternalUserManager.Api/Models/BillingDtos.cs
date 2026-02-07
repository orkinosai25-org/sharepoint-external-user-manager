namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Request to create a checkout session
/// </summary>
public class CreateCheckoutSessionRequest
{
    public SubscriptionTier PlanTier { get; set; }
    public bool IsAnnual { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response containing checkout session details
/// </summary>
public class CreateCheckoutSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response containing subscription status
/// </summary>
public class SubscriptionStatusResponse
{
    public string Tier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialExpiry { get; set; }
    public PlanLimits? Limits { get; set; }
    public PlanFeatures? Features { get; set; }
    public bool IsActive { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
}

/// <summary>
/// Response containing list of available plans
/// </summary>
public class PlansResponse
{
    public List<PlanDefinition> Plans { get; set; } = new();
}

namespace SharePointExternalUserManager.Portal.Models;

/// <summary>
/// Subscription plan definition
/// </summary>
public class SubscriptionPlan
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public string StripePriceIdMonthly { get; set; } = string.Empty;
    public string StripePriceIdAnnual { get; set; } = string.Empty;
    public string Currency { get; set; } = "GBP";
    public bool IsEnterprise { get; set; }
    public PlanLimits Limits { get; set; } = new();
    public List<string> Features { get; set; } = new();
}

/// <summary>
/// Plan limits and quotas
/// </summary>
public class PlanLimits
{
    public int? MaxClientSpaces { get; set; }
    public int? MaxExternalUsers { get; set; }
    public int? MaxStorageGB { get; set; }
}

/// <summary>
/// Response containing available plans
/// </summary>
public class PlansResponse
{
    public List<SubscriptionPlan> Plans { get; set; } = new();
}

/// <summary>
/// Request to create checkout session
/// </summary>
public class CreateCheckoutSessionRequest
{
    public string PlanTier { get; set; } = string.Empty;
    public bool IsAnnual { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response from checkout session creation
/// </summary>
public class CreateCheckoutSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
}

/// <summary>
/// Subscription status response
/// </summary>
public class SubscriptionStatusResponse
{
    public string Tier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialExpiry { get; set; }
    public bool IsActive { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    public PlanLimits Limits { get; set; } = new();
    public List<string> Features { get; set; } = new();
}

/// <summary>
/// Client space response
/// </summary>
public class ClientResponse
{
    public int Id { get; set; }
    public string ClientReference { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SharePointSiteId { get; set; }
    public string? SharePointSiteUrl { get; set; }
    public string ProvisioningStatus { get; set; } = string.Empty;
    public DateTime? ProvisionedDate { get; set; }
    public string? ProvisioningError { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Request to create a new client
/// </summary>
public class CreateClientRequest
{
    public string ClientReference { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

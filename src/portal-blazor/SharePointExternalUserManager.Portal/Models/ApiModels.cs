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

/// <summary>
/// External user data
/// </summary>
public class ExternalUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string PermissionLevel { get; set; } = string.Empty;
    public DateTime InvitedDate { get; set; }
    public string InvitedBy { get; set; } = string.Empty;
    public DateTime? LastAccessDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request to invite an external user
/// </summary>
public class InviteExternalUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string PermissionLevel { get; set; } = string.Empty;
    public string? Message { get; set; }
}

/// <summary>
/// Document library data
/// </summary>
public class LibraryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastModifiedDateTime { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// List data
/// </summary>
public class ListResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastModifiedDateTime { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Search result type
/// </summary>
public enum SearchResultType
{
    Document,
    User,
    ClientSpace,
    Library
}

/// <summary>
/// Search result data
/// </summary>
public class SearchResultDto
{
    public string Id { get; set; } = string.Empty;
    public SearchResultType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? OwnerEmail { get; set; }
    public string? OwnerDisplayName { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public double Score { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Search response with pagination
/// </summary>
public class SearchResponse
{
    public bool Success { get; set; }
    public List<SearchResultDto> Data { get; set; } = new();
    public PaginationMeta? Pagination { get; set; }
    public SearchInfo? SearchInfo { get; set; }
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public bool HasNext { get; set; }
}

/// <summary>
/// Search information
/// </summary>
public class SearchInfo
{
    public string Query { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public long SearchTimeMs { get; set; }
}

/// <summary>
/// Dashboard summary response containing aggregated statistics
/// </summary>
public class DashboardSummaryResponse
{
    public int TotalClientSpaces { get; set; }
    public int TotalExternalUsers { get; set; }
    public int ActiveInvitations { get; set; }
    public string PlanTier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? TrialDaysRemaining { get; set; }
    public DateTime? TrialExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public DashboardPlanLimits Limits { get; set; } = new();
    public List<QuickAction> QuickActions { get; set; } = new();
}

/// <summary>
/// Plan limits for dashboard
/// </summary>
public class DashboardPlanLimits
{
    public int? MaxClientSpaces { get; set; }
    public int? MaxExternalUsers { get; set; }
    public int? MaxStorageGB { get; set; }
    public int? ClientSpacesUsagePercent { get; set; }
    public int? ExternalUsersUsagePercent { get; set; }
}

/// <summary>
/// Quick action suggestion
/// </summary>
public class QuickAction
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Type { get; set; } = "navigate";
    public string Priority { get; set; } = "secondary";
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
/// Tenant information response from GET /tenants/me
/// </summary>
public class TenantInfoResponse
{
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? UserPrincipalName { get; set; }
    public bool IsActive { get; set; }
    public string SubscriptionTier { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}

/// <summary>
/// Request to register a new tenant
/// </summary>
public class TenantRegistrationRequest
{
    public string OrganizationName { get; set; } = string.Empty;
    public string PrimaryAdminEmail { get; set; } = string.Empty;
    public TenantSettingsDto? Settings { get; set; }
}

/// <summary>
/// Tenant settings
/// </summary>
public class TenantSettingsDto
{
    public string? CompanyWebsite { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }
}

/// <summary>
/// Response from tenant registration
/// </summary>
public class TenantRegistrationResponse
{
    public int InternalTenantId { get; set; }
    public string EntraIdTenantId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public DateTime? TrialExpiryDate { get; set; }
    public DateTime RegisteredDate { get; set; }
}

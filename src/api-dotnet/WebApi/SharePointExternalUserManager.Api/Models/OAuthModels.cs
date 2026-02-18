namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Request to initiate OAuth admin consent flow
/// </summary>
public class ConnectTenantRequest
{
    public string RedirectUri { get; set; } = string.Empty;
}

/// <summary>
/// Response from connect tenant endpoint
/// </summary>
public class ConnectTenantResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// Response from validate permissions endpoint
/// </summary>
public class ValidatePermissionsResponse
{
    public bool HasRequiredPermissions { get; set; }
    public List<string> GrantedPermissions { get; set; } = new();
    public List<string> MissingPermissions { get; set; } = new();
    public bool TokenExpired { get; set; }
    public bool TokenRefreshed { get; set; }
    public DateTime? ConsentGrantedAt { get; set; }
    public string? ConsentGrantedBy { get; set; }
}

/// <summary>
/// Internal state data for OAuth flow (CSRF protection)
/// </summary>
public class OAuthState
{
    public required string TenantId { get; set; }
    public required string RedirectUri { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
}

/// <summary>
/// Microsoft token response from OAuth flow
/// </summary>
public class TokenResponse
{
    public string? access_token { get; set; }
    public string? token_type { get; set; }
    public int expires_in { get; set; }
    public string? refresh_token { get; set; }
    public string? scope { get; set; }
}

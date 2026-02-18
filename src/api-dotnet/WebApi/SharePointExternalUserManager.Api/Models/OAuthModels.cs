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
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

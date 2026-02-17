namespace SharePointExternalUserManager.Portal.Models;

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

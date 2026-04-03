namespace SharePointExternalUserManager.Portal.Models;

/// <summary>
/// Configuration settings for the backend API
/// </summary>
public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;

    /// <summary>
    /// OAuth2 scopes to request when acquiring a Bearer token to call the backend API.
    /// Set to the scope(s) exposed by the API app registration, e.g.
    /// ["api://&lt;api-client-id&gt;/.default"].
    /// Leave empty to skip token acquisition (unauthenticated requests).
    /// Configure via App Service environment variable ApiSettings__Scopes__0.
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();
}

namespace SharePointExternalUserManager.Portal.Models;

/// <summary>
/// Configuration settings for the backend API
/// </summary>
public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;

    /// <summary>
    /// OAuth 2.0 scopes required to call the backend API on behalf of the signed-in user.
    /// Example: ["api://&lt;API-CLIENT-ID&gt;/access_as_user"]
    /// When empty, API calls are made without a Bearer token (only suitable for development
    /// environments where the API has authentication disabled).
    /// </summary>
    public string[] ApiScopes { get; set; } = Array.Empty<string>();
}

namespace SharePointExternalUserManager.Portal.Models;

/// <summary>
/// Configuration settings for Azure AD authentication
/// </summary>
public class AzureAdSettings
{
    public string Instance { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = string.Empty;
    public string SignedOutCallbackPath { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

namespace SharePointExternalUserManager.Portal.Models;

/// <summary>
/// Configuration settings for the backend API
/// </summary>
public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;
}

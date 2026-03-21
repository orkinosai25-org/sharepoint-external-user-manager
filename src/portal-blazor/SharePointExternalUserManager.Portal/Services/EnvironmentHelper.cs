namespace SharePointExternalUserManager.Portal.Services;

/// <summary>
/// Helpers for detecting the hosting environment at runtime.
/// </summary>
internal static class EnvironmentHelper
{
    /// <summary>
    /// Returns <c>true</c> when the application is running inside Azure App Service.
    /// Azure App Service always sets the <c>WEBSITE_INSTANCE_ID</c> environment variable;
    /// its presence is used to distinguish a live hosted deployment from a local developer
    /// machine even when <c>ASPNETCORE_ENVIRONMENT</c> is set to "Development".
    /// </summary>
    public static bool IsAzureAppService =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
}

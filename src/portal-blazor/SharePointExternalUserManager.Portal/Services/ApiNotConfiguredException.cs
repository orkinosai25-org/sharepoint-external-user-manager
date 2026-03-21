namespace SharePointExternalUserManager.Portal.Services;

/// <summary>
/// Thrown when the backend API base URL has not been configured and an API call cannot
/// be made.  Callers should catch this exception to display a user-friendly
/// "please configure the API URL" message rather than a generic HTTP error.
/// </summary>
public class ApiNotConfiguredException : InvalidOperationException
{
    public ApiNotConfiguredException()
        : base(
            "The API server URL is not configured. " +
            "Please set the 'ApiSettings__BaseUrl' application setting in Azure App Service " +
            "to the base URL of the backend API (e.g. 'https://your-api.azurewebsites.net/api') " +
            "and restart the application.")
    {
    }
}

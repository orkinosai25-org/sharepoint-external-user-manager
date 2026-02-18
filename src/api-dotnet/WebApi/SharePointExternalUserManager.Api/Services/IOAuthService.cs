using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service interface for OAuth token management and Microsoft Graph authentication
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// Generate authorization URL for admin consent flow
    /// </summary>
    string GenerateAuthorizationUrl(string redirectUri, string state);

    /// <summary>
    /// Exchange authorization code for access and refresh tokens
    /// </summary>
    Task<TokenResponse?> ExchangeCodeForTokensAsync(string code, string redirectUri);

    /// <summary>
    /// Refresh an expired access token using a refresh token
    /// </summary>
    Task<TokenResponse?> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Validate that all required Microsoft Graph permissions are granted
    /// </summary>
    Task<(bool hasPermissions, List<string> grantedPermissions, List<string> missingPermissions)> 
        ValidatePermissionsAsync(string accessToken);

    /// <summary>
    /// Encode OAuth state object to base64 for CSRF protection
    /// </summary>
    string EncodeState(OAuthState state);

    /// <summary>
    /// Decode OAuth state from base64
    /// </summary>
    OAuthState? DecodeState(string encodedState);

    /// <summary>
    /// Required Microsoft Graph permissions for the application
    /// </summary>
    List<string> RequiredPermissions { get; }
}

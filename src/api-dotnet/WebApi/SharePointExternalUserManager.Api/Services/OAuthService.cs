using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Implementation of OAuth service for Microsoft Graph authentication
/// </summary>
public class OAuthService : IOAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthService> _logger;

    // Required Microsoft Graph permissions
    public List<string> RequiredPermissions => new()
    {
        "User.Read.All",
        "Sites.ReadWrite.All",
        "Sites.FullControl.All",
        "Directory.Read.All"
    };

    public OAuthService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OAuthService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string GenerateAuthorizationUrl(string redirectUri, string state)
    {
        var clientId = _configuration["AzureAd:ClientId"];
        var tenantId = "common"; // Multi-tenant support

        if (string.IsNullOrEmpty(clientId))
        {
            throw new InvalidOperationException("Azure AD ClientId is not configured");
        }

        // Construct admin consent URL
        var authUrl = $"https://login.microsoftonline.com/{tenantId}/v2.0/adminconsent" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&scope=https://graph.microsoft.com/.default";

        return authUrl;
    }

    public async Task<TokenResponse?> ExchangeCodeForTokensAsync(string code, string redirectUri)
    {
        var clientId = _configuration["AzureAd:ClientId"];
        var clientSecret = _configuration["AzureAd:ClientSecret"];
        var tenantId = "common"; // Multi-tenant support

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Azure AD credentials are not configured");
        }

        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        var requestData = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["scope"] = "https://graph.microsoft.com/.default offline_access"
        };

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(requestData);
            var response = await httpClient.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token exchange failed: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging code for tokens");
            return null;
        }
    }

    public async Task<TokenResponse?> RefreshAccessTokenAsync(string refreshToken)
    {
        var clientId = _configuration["AzureAd:ClientId"];
        var clientSecret = _configuration["AzureAd:ClientSecret"];
        var tenantId = "common"; // Multi-tenant support

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Azure AD credentials are not configured");
        }

        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        var requestData = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
            ["scope"] = "https://graph.microsoft.com/.default offline_access"
        };

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(requestData);
            var response = await httpClient.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token refresh failed: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing access token");
            return null;
        }
    }

    public async Task<(bool hasPermissions, List<string> grantedPermissions, List<string> missingPermissions)> 
        ValidatePermissionsAsync(string accessToken)
    {
        try
        {
            // Call Microsoft Graph to get granted permissions
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Get the service principal for Microsoft Graph
            var response = await httpClient.GetAsync(
                "https://graph.microsoft.com/v1.0/me/oauth2PermissionGrants");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to validate permissions: {StatusCode}", response.StatusCode);
                return (false, new List<string>(), RequiredPermissions);
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            var grantedPermissions = new List<string>();

            if (result.TryGetProperty("value", out var grants))
            {
                foreach (var grant in grants.EnumerateArray())
                {
                    if (grant.TryGetProperty("scope", out var scope))
                    {
                        var scopes = scope.GetString()?.Split(' ') ?? Array.Empty<string>();
                        grantedPermissions.AddRange(scopes);
                    }
                }
            }

            // Check if all required permissions are granted
            var missingPermissions = RequiredPermissions
                .Where(p => !grantedPermissions.Contains(p, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var hasAllPermissions = !missingPermissions.Any();

            return (hasAllPermissions, grantedPermissions, missingPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating permissions");
            return (false, new List<string>(), RequiredPermissions);
        }
    }

    public string EncodeState(OAuthState state)
    {
        var json = JsonSerializer.Serialize(state);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    public OAuthState? DecodeState(string encodedState)
    {
        try
        {
            var bytes = Convert.FromBase64String(encodedState);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<OAuthState>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decoding OAuth state");
            return null;
        }
    }
}

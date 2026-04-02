using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using SharePointExternalUserManager.Portal.Models;

namespace SharePointExternalUserManager.Portal.Services;

/// <summary>
/// Service for calling the backend API
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly string[] _apiScopes;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(
        HttpClient httpClient,
        AuthenticationStateProvider authStateProvider,
        ITokenAcquisition tokenAcquisition,
        IOptions<ApiSettings> apiSettings,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _tokenAcquisition = tokenAcquisition;
        _apiScopes = apiSettings.Value.Scopes;
        _logger = logger;
    }

    /// <summary>
    /// Verifies that the HttpClient has a BaseAddress configured and throws
    /// <see cref="ApiNotConfiguredException"/> when it does not.  Call this at the start of
    /// every public method that makes an HTTP request so that callers receive a descriptive
    /// error instead of the cryptic "An invalid request URI was provided" message.
    /// </summary>
    private void EnsureBaseAddressConfigured()
    {
        if (_httpClient.BaseAddress == null)
        {
            throw new ApiNotConfiguredException();
        }
    }

    /// <summary>
    /// Returns the configured API base URL, or null when no base address is configured.
    /// Useful for diagnostic error messages.
    /// </summary>
    public string? BaseUrl => _httpClient.BaseAddress?.ToString();

    /// <summary>
    /// Throws a descriptive <see cref="HttpRequestException"/> when the server returns 404,
    /// including the target URL so operators can quickly diagnose misconfigured
    /// <c>ApiSettings__BaseUrl</c> values.
    /// </summary>
    private void ThrowIfNotFound(System.Net.HttpStatusCode statusCode, string endpointPath)
    {
        if (statusCode == System.Net.HttpStatusCode.NotFound)
        {
            var baseUrl = _httpClient.BaseAddress?.ToString() ?? "unknown";
            string actualUrl;
            try
            {
                actualUrl = _httpClient.BaseAddress != null
                    ? new Uri(_httpClient.BaseAddress, new Uri(endpointPath, UriKind.RelativeOrAbsolute)).ToString()
                    : baseUrl + endpointPath;
            }
            catch
            {
                actualUrl = baseUrl + endpointPath;
            }

            throw new HttpRequestException(
                $"Response status code does not indicate success: 404 (Not Found). " +
                $"The API endpoint '{endpointPath}' was not found. " +
                $"Actual URL called: '{actualUrl}'. " +
                $"Configured base URL ('ApiSettings__BaseUrl'): '{baseUrl}'. " +
                $"Verify that 'ApiSettings__BaseUrl' points to the correct API App Service URL " +
                $"(e.g. https://your-api.azurewebsites.net) and that the API is deployed and running.",
                null,
                System.Net.HttpStatusCode.NotFound);
        }
    }

    /// <summary>
    /// Acquires a Bearer token for the configured API scopes, or returns <c>null</c> when no
    /// scopes are configured or token acquisition fails.  Uses Microsoft.Identity.Web's
    /// in-memory token cache so real network round-trips only happen when the token has expired.
    /// </summary>
    private async Task<string?> AcquireBearerTokenAsync()
    {
        if (_apiScopes.Length == 0)
        {
            return null;
        }

        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                return await _tokenAcquisition.GetAccessTokenForUserAsync(_apiScopes, user: user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to acquire access token for API call; request will proceed without Bearer token");
        }

        return null;
    }

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/> with the Authorization header set when a
    /// Bearer token is available.  Using per-request headers avoids mutating the shared
    /// <see cref="HttpClient.DefaultRequestHeaders"/> which would be unsafe under concurrent use.
    /// </summary>
    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (content != null)
        {
            request.Content = content;
        }

        var token = await AcquireBearerTokenAsync();
        if (token != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    /// <summary>
    /// Get dashboard summary with aggregated statistics
    /// </summary>
    public async Task<DashboardSummaryResponse?> GetDashboardSummaryAsync()
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var request = await CreateRequestAsync(HttpMethod.Get, "/api/dashboard/summary");
            var response = await _httpClient.SendAsync(request);
            ThrowIfNotFound(response.StatusCode, "/api/dashboard/summary");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<DashboardSummaryResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard summary");
            throw;
        }
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    public async Task<PlansResponse?> GetPlansAsync(bool includeEnterprise = false)
    {
        try
        {
            EnsureBaseAddressConfigured();
            var url = $"/api/billing/plans?includeEnterprise={includeEnterprise}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PlansResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get plans");
            throw;
        }
    }

    /// <summary>
    /// Create a Stripe checkout session
    /// </summary>
    public async Task<CreateCheckoutSessionResponse?> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest request)
    {
        try
        {
            EnsureBaseAddressConfigured();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var req = await CreateRequestAsync(HttpMethod.Post, "/api/billing/checkout-session", content);
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CreateCheckoutSessionResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkout session");
            throw;
        }
    }

    /// <summary>
    /// Get current subscription status
    /// </summary>
    public async Task<SubscriptionStatusResponse?> GetSubscriptionStatusAsync()
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var req = await CreateRequestAsync(HttpMethod.Get, "/api/billing/subscription/status");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SubscriptionStatusResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subscription status");
            throw;
        }
    }

    /// <summary>
    /// Get all clients for the current tenant
    /// </summary>
    public async Task<List<ClientResponse>> GetClientsAsync()
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var req = await CreateRequestAsync(HttpMethod.Get, "/api/clients");
            var response = await _httpClient.SendAsync(req);
            ThrowIfNotFound(response.StatusCode, "/api/clients");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ClientResponse>>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data ?? new List<ClientResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get clients");
            throw;
        }
    }

    /// <summary>
    /// Create a new client space
    /// </summary>
    public async Task<ClientResponse?> CreateClientAsync(CreateClientRequest request)
    {
        try
        {
            EnsureBaseAddressConfigured();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var req = await CreateRequestAsync(HttpMethod.Post, "/api/clients", content);
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ClientResponse>>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create client");
            throw;
        }
    }

    /// <summary>
    /// Get a specific client by ID
    /// </summary>
    public async Task<ClientResponse?> GetClientAsync(int id)
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var req = await CreateRequestAsync(HttpMethod.Get, $"/api/clients/{id}");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ClientResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to get client {id}");
            throw;
        }
    }

    /// <summary>
    /// Initiate OAuth admin consent flow for tenant
    /// </summary>
    public async Task<ConnectTenantResponse?> ConnectTenantAsync(string redirectUri)
    {
        try
        {
            EnsureBaseAddressConfigured();
            var request = new { redirectUri };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var req = await CreateRequestAsync(HttpMethod.Post, "/api/auth/connect", content);
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ConnectTenantResponse>>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect tenant");
            throw;
        }
    }

    /// <summary>
    /// Validate Microsoft Graph permissions for tenant
    /// </summary>
    public async Task<ValidatePermissionsResponse?> ValidatePermissionsAsync()
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var req = await CreateRequestAsync(HttpMethod.Get, "/api/auth/permissions");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ValidatePermissionsResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate permissions");
            throw;
        }
    }

    /// <summary>
    /// Get all external users for a client
    /// </summary>
    public async Task<List<ExternalUserDto>> GetExternalUsersAsync(int clientId)
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var req = await CreateRequestAsync(HttpMethod.Get, $"/api/clients/{clientId}/external-users");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ExternalUserDto>>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data ?? new List<ExternalUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to get external users for client {clientId}");
            throw;
        }
    }

    /// <summary>
    /// Invite an external user to a client
    /// </summary>
    public async Task<ExternalUserDto?> InviteExternalUserAsync(int clientId, InviteExternalUserRequest request)
    {
        try
        {
            EnsureBaseAddressConfigured();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var req = await CreateRequestAsync(HttpMethod.Post, $"/api/clients/{clientId}/external-users", content);
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ExternalUserDto>>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to invite external user to client {clientId}");
            throw;
        }
    }

    /// <summary>
    /// Remove an external user from a client
    /// </summary>
    public async Task<bool> RemoveExternalUserAsync(int clientId, string email)
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var req = await CreateRequestAsync(HttpMethod.Delete, $"/api/clients/{clientId}/external-users/{Uri.EscapeDataString(email)}");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to remove external user {email} from client {clientId}");
            throw;
        }
    }

    /// <summary>
    /// Get all libraries for a client
    /// </summary>
    public async Task<List<LibraryResponse>> GetLibrariesAsync(int clientId)
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var req = await CreateRequestAsync(HttpMethod.Get, $"/api/clients/{clientId}/libraries");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<LibraryResponse>>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data ?? new List<LibraryResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to get libraries for client {clientId}");
            throw;
        }
    }

    /// <summary>
    /// Get all lists for a client
    /// </summary>
    public async Task<List<ListResponse>> GetListsAsync(int clientId)
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var req = await CreateRequestAsync(HttpMethod.Get, $"/api/clients/{clientId}/lists");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ListResponse>>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data ?? new List<ListResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to get lists for client {clientId}");
            throw;
        }
    }

    /// <summary>
    /// Search within a client space
    /// </summary>
    /// <param name="clientId">The client space ID to search within</param>
    /// <param name="query">The search query string</param>
    /// <param name="page">Page number (minimum 1, defaults to 1)</param>
    /// <param name="pageSize">Results per page (minimum 1, maximum 100, defaults to 20)</param>
    /// <returns>Search response with results and pagination metadata</returns>
    public async Task<SearchResponse?> SearchClientSpaceAsync(int clientId, string query, int page = 1, int pageSize = 20)
    {
        try
        {
            EnsureBaseAddressConfigured();
            // Validate and correct parameters
            if (page < 1)
            {
                _logger.LogWarning("Invalid page number {Page} corrected to 1", page);
                page = 1;
            }
            
            if (pageSize < 1 || pageSize > 100)
            {
                _logger.LogWarning("Invalid page size {PageSize} corrected to 20 (valid range: 1-100)", pageSize);
                pageSize = 20;
            }

            var url = $"/v1/client-spaces/{clientId}/search?q={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}";
            using var req = await CreateRequestAsync(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to search client space {clientId} with query '{query}'");
            throw;
        }
    }

    /// <summary>
    /// Execute global search across all client spaces (Pro/Enterprise feature)
    /// </summary>
    public async Task<SearchResponse?> GlobalSearchAsync(
        string query, 
        string scope = "all", 
        string? type = null,
        int page = 1, 
        int pageSize = 20)
    {
        try
        {
            EnsureBaseAddressConfigured();
            // Validate and correct parameters
            if (page < 1)
            {
                _logger.LogWarning("Invalid page number {Page} corrected to 1", page);
                page = 1;
            }
            
            if (pageSize < 1 || pageSize > 100)
            {
                _logger.LogWarning("Invalid page size {PageSize} corrected to 20 (valid range: 1-100)", pageSize);
                pageSize = 20;
            }

            var url = $"/v1/search?q={Uri.EscapeDataString(query)}&scope={scope}&page={page}&pageSize={pageSize}";
            
            if (!string.IsNullOrWhiteSpace(type))
            {
                url += $"&type={type}";
            }

            using var req = await CreateRequestAsync(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to execute global search with query '{query}'");
            throw;
        }
    }

    /// <summary>
    /// Read an unsuccessful HTTP response and throw an HttpRequestException that includes the
    /// response status code, reason phrase, and body so callers get actionable error details.
    /// </summary>
    private static async Task ThrowWithResponseBodyAsync(HttpResponseMessage response)
    {
        var body = string.Empty;
        try { body = await response.Content.ReadAsStringAsync(); }
        catch (Exception readEx)
        {
            // Reading the body is best-effort; if it fails the original status code is still
            // included in the thrown exception so the caller still gets actionable information.
            _ = readEx; // suppress unused-variable warning
        }

        var message = $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase})";
        if (!string.IsNullOrWhiteSpace(body))
        {
            message += $"\nResponse body: {body}";
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    /// <summary>
    /// Get current subscription for authenticated user (new endpoint)
    /// </summary>
    public async Task<SubscriptionStatusResponse?> GetMySubscriptionAsync()
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var req = await CreateRequestAsync(HttpMethod.Get, "/api/subscription/me");
            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                await ThrowWithResponseBodyAsync(response);
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<SubscriptionStatusResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get my subscription");
            throw;
        }
    }

    /// <summary>
    /// Cancel current subscription
    /// </summary>
    public async Task CancelSubscriptionAsync()
    {
        try
        {
            EnsureBaseAddressConfigured();
            using var req = await CreateRequestAsync(HttpMethod.Post, "/api/subscription/cancel");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription");
            throw;
        }
    }
}

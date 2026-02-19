using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using SharePointExternalUserManager.Portal.Models;

namespace SharePointExternalUserManager.Portal.Services;

/// <summary>
/// Service for calling the backend API
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(
        HttpClient httpClient,
        AuthenticationStateProvider authStateProvider,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard summary with aggregated statistics
    /// </summary>
    public async Task<DashboardSummaryResponse?> GetDashboardSummaryAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/dashboard/summary");
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
            var url = $"/billing/plans?includeEnterprise={includeEnterprise}";
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
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/billing/checkout-session", content);
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
            var response = await _httpClient.GetAsync("/billing/subscription/status");
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
            var response = await _httpClient.GetAsync("/clients");
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
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/clients", content);
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
            var response = await _httpClient.GetAsync($"/clients/{id}");
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
            var request = new { redirectUri };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/auth/connect", content);
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
            var response = await _httpClient.GetAsync("/auth/permissions");
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
            var response = await _httpClient.GetAsync($"/clients/{clientId}/external-users");
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
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"/clients/{clientId}/external-users", content);
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
            var response = await _httpClient.DeleteAsync($"/clients/{clientId}/external-users/{Uri.EscapeDataString(email)}");
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
            var response = await _httpClient.GetAsync($"/clients/{clientId}/libraries");
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
            var response = await _httpClient.GetAsync($"/clients/{clientId}/lists");
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
            var response = await _httpClient.GetAsync(url);
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

            var response = await _httpClient.GetAsync(url);
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
    /// Get current subscription for authenticated user (new endpoint)
    /// </summary>
    public async Task<SubscriptionStatusResponse?> GetMySubscriptionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/subscription/me");
            response.EnsureSuccessStatusCode();

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
            var response = await _httpClient.PostAsync("/api/subscription/cancel", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription");
            throw;
        }
    }

    /// <summary>
    /// Get current tenant information from /tenants/me
    /// Returns null if tenant is not yet registered
    /// </summary>
    public async Task<TenantInfoResponse?> GetTenantInfoAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/tenants/me");
            
            // If 401/403, tenant doesn't exist yet or auth failed
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Tenant info request failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TenantInfoResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenant info");
            // Return null instead of throwing - tenant might not exist yet
            return null;
        }
    }

    /// <summary>
    /// Register a new tenant
    /// </summary>
    public async Task<TenantRegistrationResponse?> RegisterTenantAsync(TenantRegistrationRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/tenants/register", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TenantRegistrationResponse>>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register tenant");
            throw;
        }
    }
}

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
}

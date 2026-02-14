using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Models.Search;
using SharePointExternalUserManager.Functions.Services.Search;

namespace SharePointExternalUserManager.Functions.Functions.Search;

/// <summary>
/// Search suggestions function - provides autocomplete suggestions
/// </summary>
public class SearchSuggestionsFunction
{
    private readonly ILogger<SearchSuggestionsFunction> _logger;
    private readonly ISearchService _searchService;

    public SearchSuggestionsFunction(
        ILogger<SearchSuggestionsFunction> logger,
        ISearchService searchService)
    {
        _logger = logger;
        _searchService = searchService;
    }

    [Function("SearchSuggestions")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/search/suggestions")] HttpRequestData req)
    {
        _logger.LogInformation("Search suggestions request received");

        try
        {
            // Get tenant context from middleware
            var tenantIdString = req.FunctionContext.Items["TenantId"] as string;
            
            if (string.IsNullOrEmpty(tenantIdString))
            {
                return await CreateErrorResponse(req, System.Net.HttpStatusCode.Unauthorized,
                    "UNAUTHORIZED", "Tenant ID not found");
            }

            var tenantId = Guid.Parse(tenantIdString);

            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var searchQuery = query["q"] ?? query["query"] ?? string.Empty;
            var scope = ParseScope(query["scope"]);

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return await CreateErrorResponse(req, System.Net.HttpStatusCode.BadRequest,
                    "INVALID_QUERY", "Search query parameter 'q' is required");
            }

            // Validate scope permissions for global suggestions
            if (scope == SearchScope.AllClients)
            {
                var subscriptionTier = req.FunctionContext.Items["SubscriptionTier"] as SubscriptionTier?;
                if (subscriptionTier != SubscriptionTier.Pro && subscriptionTier != SubscriptionTier.Enterprise)
                {
                    scope = SearchScope.CurrentClient;
                }
            }

            _logger.LogInformation("Getting search suggestions for tenant {TenantId}: query='{Query}', scope={Scope}",
                tenantId, searchQuery, scope);

            // Get suggestions
            var suggestions = await _searchService.GetSuggestionsAsync(tenantId, searchQuery, scope);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<List<string>>.SuccessResponse(suggestions));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions");
            return await CreateErrorResponse(req, System.Net.HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR", "An error occurred while getting search suggestions");
        }
    }

    private SearchScope ParseScope(string? scopeString)
    {
        if (string.IsNullOrEmpty(scopeString))
            return SearchScope.CurrentClient;

        return scopeString.ToLowerInvariant() switch
        {
            "all" or "allclients" or "global" => SearchScope.AllClients,
            "current" or "currentclient" or "client" => SearchScope.CurrentClient,
            _ => SearchScope.CurrentClient
        };
    }

    private async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req,
        System.Net.HttpStatusCode statusCode,
        string errorCode,
        string message,
        string? details = null)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(ApiResponse<object>.ErrorResponse(errorCode, message, details));
        return response;
    }
}

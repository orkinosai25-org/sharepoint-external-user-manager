using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Models.Search;
using SharePointExternalUserManager.Functions.Services.Search;
using System.Diagnostics;

namespace SharePointExternalUserManager.Functions.Functions.Search;

/// <summary>
/// Global search function - searches across all client spaces (Pro tier feature)
/// </summary>
public class GlobalSearchFunction
{
    private readonly ILogger<GlobalSearchFunction> _logger;
    private readonly ISearchService _searchService;

    public GlobalSearchFunction(
        ILogger<GlobalSearchFunction> logger,
        ISearchService searchService)
    {
        _logger = logger;
        _searchService = searchService;
    }

    [Function("GlobalSearch")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/search")] HttpRequestData req)
    {
        _logger.LogInformation("Global search request received");

        var stopwatch = Stopwatch.StartNew();

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
            var searchRequest = new SearchRequest
            {
                Query = query["q"] ?? query["query"] ?? string.Empty,
                Scope = ParseScope(query["scope"]),
                ClientId = int.TryParse(query["clientId"], out var cid) ? cid : null,
                UserEmail = query["userEmail"],
                ResultType = ParseResultType(query["type"]),
                DateFrom = ParseDate(query["dateFrom"]),
                DateTo = ParseDate(query["dateTo"]),
                Page = int.TryParse(query["page"], out var p) ? Math.Max(1, p) : 1,
                PageSize = int.TryParse(query["pageSize"], out var ps) ? Math.Min(Math.Max(1, ps), 100) : 20
            };

            // Validate scope permissions
            if (searchRequest.Scope == SearchScope.AllClients)
            {
                var subscriptionTier = req.FunctionContext.Items["SubscriptionTier"] as SubscriptionTier?;
                if (subscriptionTier != SubscriptionTier.Pro && subscriptionTier != SubscriptionTier.Enterprise)
                {
                    return await CreateErrorResponse(req, System.Net.HttpStatusCode.Forbidden,
                        "FORBIDDEN", "Global search across all clients requires Pro or Enterprise subscription",
                        "Upgrade your subscription to access cross-client search");
                }
            }

            _logger.LogInformation(
                "Executing search for tenant {TenantId}: query='{Query}', scope={Scope}, page={Page}, pageSize={PageSize}",
                tenantId, searchRequest.Query, searchRequest.Scope, searchRequest.Page, searchRequest.PageSize);

            // Execute search
            var results = await _searchService.SearchAsync(tenantId, searchRequest);

            // Apply pagination
            var total = results.Count;
            var pagedResults = results
                .Skip((searchRequest.Page - 1) * searchRequest.PageSize)
                .Take(searchRequest.PageSize)
                .ToList();

            stopwatch.Stop();

            var pagination = new PaginationMeta
            {
                Page = searchRequest.Page,
                PageSize = searchRequest.PageSize,
                Total = total,
                HasNext = searchRequest.Page * searchRequest.PageSize < total
            };

            var searchResponse = new SearchResponse
            {
                Query = searchRequest.Query,
                Scope = searchRequest.Scope,
                SearchTimeMs = stopwatch.ElapsedMilliseconds,
                Filters = new SearchFiltersDto
                {
                    ClientId = searchRequest.ClientId,
                    UserEmail = searchRequest.UserEmail,
                    ResultType = searchRequest.ResultType,
                    DateFrom = searchRequest.DateFrom,
                    DateTo = searchRequest.DateTo
                }
            };

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                Success = true,
                Data = pagedResults,
                Pagination = pagination,
                SearchInfo = searchResponse
            });

            _logger.LogInformation("Search completed: {ResultCount} total results, {PagedCount} returned in {ElapsedMs}ms",
                total, pagedResults.Count, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing global search");
            return await CreateErrorResponse(req, System.Net.HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR", "An error occurred while executing the search");
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

    private SearchResultType? ParseResultType(string? typeString)
    {
        if (string.IsNullOrEmpty(typeString))
            return null;

        return typeString.ToLowerInvariant() switch
        {
            "document" or "documents" or "doc" or "file" => SearchResultType.Document,
            "user" or "users" => SearchResultType.User,
            "client" or "clientspace" or "space" => SearchResultType.ClientSpace,
            "library" or "libraries" or "lib" => SearchResultType.Library,
            _ => null
        };
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var date))
            return date;

        return null;
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

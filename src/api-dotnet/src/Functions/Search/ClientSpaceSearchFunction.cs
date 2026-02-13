using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Models.Search;
using SharePointExternalUserManager.Functions.Services.Search;
using System.Diagnostics;

namespace SharePointExternalUserManager.Functions.Functions.Search;

/// <summary>
/// Client space search function - searches within a specific client space (Free tier feature)
/// </summary>
public class ClientSpaceSearchFunction
{
    private readonly ILogger<ClientSpaceSearchFunction> _logger;
    private readonly ISearchService _searchService;

    public ClientSpaceSearchFunction(
        ILogger<ClientSpaceSearchFunction> logger,
        ISearchService searchService)
    {
        _logger = logger;
        _searchService = searchService;
    }

    [Function("ClientSpaceSearch")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/client-spaces/{clientId}/search")] 
        HttpRequestData req,
        int clientId)
    {
        _logger.LogInformation("Client space search request received for client {ClientId}", clientId);

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

            // Validate client ID
            if (clientId <= 0)
            {
                return await CreateErrorResponse(req, System.Net.HttpStatusCode.BadRequest,
                    "INVALID_CLIENT_ID", "Invalid client space ID");
            }

            // TODO: Verify user has access to this client space
            // For MVP, we assume the authentication middleware has verified tenant access

            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var searchRequest = new SearchRequest
            {
                Query = query["q"] ?? query["query"] ?? string.Empty,
                Scope = SearchScope.CurrentClient,
                ClientId = clientId,
                UserEmail = query["userEmail"],
                ResultType = ParseResultType(query["type"]),
                DateFrom = ParseDate(query["dateFrom"]),
                DateTo = ParseDate(query["dateTo"]),
                Page = int.TryParse(query["page"], out var p) ? Math.Max(1, p) : 1,
                PageSize = int.TryParse(query["pageSize"], out var ps) ? Math.Min(Math.Max(1, ps), 100) : 20
            };

            _logger.LogInformation(
                "Executing client space search for tenant {TenantId}, client {ClientId}: query='{Query}', page={Page}, pageSize={PageSize}",
                tenantId, clientId, searchRequest.Query, searchRequest.Page, searchRequest.PageSize);

            // Execute search
            var results = await _searchService.SearchClientSpaceAsync(tenantId, clientId, searchRequest);

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

            _logger.LogInformation("Client space search completed: {ResultCount} total results, {PagedCount} returned in {ElapsedMs}ms",
                total, pagedResults.Count, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing client space search for client {ClientId}", clientId);
            return await CreateErrorResponse(req, System.Net.HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR", "An error occurred while executing the search");
        }
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

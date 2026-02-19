using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Models.Search;
using SharePointExternalUserManager.Functions.Services.Search;

namespace SharePointExternalUserManager.Api.Controllers;

/// <summary>
/// Controller for search operations
/// </summary>
[ApiController]
[Route("v1")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISearchService _searchService;
    private readonly IPlanEnforcementService _planEnforcementService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ApplicationDbContext context,
        ISearchService searchService,
        IPlanEnforcementService planEnforcementService,
        ILogger<SearchController> logger)
    {
        _context = context;
        _searchService = searchService;
        _planEnforcementService = planEnforcementService;
        _logger = logger;
    }

    /// <summary>
    /// Search within a specific client space (Available to all tiers)
    /// </summary>
    /// <param name="clientId">Client space ID</param>
    /// <param name="q">Search query</param>
    /// <param name="type">Optional filter by result type</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Results per page (default: 20, max: 100)</param>
    /// <returns>Search results</returns>
    [HttpGet("client-spaces/{clientId}/search")]
    public async Task<IActionResult> SearchClientSpace(
        int clientId,
        [FromQuery] string q,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Get tenant from claims
            var tenantIdClaim = User.FindFirst("tid")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));

            // Get tenant from database
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

            if (tenant == null)
                return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

            // Verify client belongs to tenant
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenant.Id && c.IsActive);

            if (client == null)
                return NotFound(ApiResponse<object>.ErrorResponse("CLIENT_NOT_FOUND", "Client space not found or access denied"));

            // Validate query
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(ApiResponse<object>.ErrorResponse("INVALID_QUERY", "Search query is required"));

            // Validate pagination
            if (page < 1)
                page = 1;
            if (pageSize < 1 || pageSize > 100)
                pageSize = 20;

            // Parse result type filter
            SearchResultType? resultType = null;
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (Enum.TryParse<SearchResultType>(type, ignoreCase: true, out var parsedType))
                    resultType = parsedType;
            }

            // Build search request
            var searchRequest = new SearchRequest
            {
                Query = q,
                Scope = SearchScope.CurrentClient,
                ClientId = clientId,
                ResultType = resultType,
                Page = page,
                PageSize = pageSize
            };

            // Execute search
            var results = await _searchService.SearchClientSpaceAsync(
                Guid.Parse(tenant.EntraIdTenantId),
                clientId,
                searchRequest);

            // Calculate pagination
            var total = results.Count;
            var skip = (page - 1) * pageSize;
            var paginatedResults = results.Skip(skip).Take(pageSize).ToList();

            // Build response
            var searchTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var response = new
            {
                success = true,
                data = paginatedResults,
                pagination = new
                {
                    page,
                    pageSize,
                    total,
                    hasNext = (skip + pageSize) < total
                },
                searchInfo = new
                {
                    query = q,
                    scope = "CurrentClient",
                    searchTimeMs
                }
            };

            _logger.LogInformation(
                "Search completed for client {ClientId} with query '{Query}'. Found {Count} results in {Time}ms",
                clientId, q, total, searchTimeMs);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing client space search for client {ClientId}", clientId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("SEARCH_ERROR", "An error occurred while searching"));
        }
    }

    /// <summary>
    /// Global search across all client spaces (Pro/Enterprise only)
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="scope">Search scope (default: current)</param>
    /// <param name="clientId">Optional filter by client ID</param>
    /// <param name="type">Optional filter by result type</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Results per page (default: 20, max: 100)</param>
    /// <returns>Search results</returns>
    [HttpGet("search")]
    public async Task<IActionResult> GlobalSearch(
        [FromQuery] string q,
        [FromQuery] string? scope = "current",
        [FromQuery] int? clientId = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Get tenant from claims
            var tenantIdClaim = User.FindFirst("tid")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));

            // Get tenant from database
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

            if (tenant == null)
                return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

            // Check subscription tier - global search requires Pro, Business, or Enterprise
            var hasGlobalSearch = await _planEnforcementService.HasFeatureAccessAsync(tenant.Id, nameof(PlanFeatures.GlobalSearch));
            if (!hasGlobalSearch)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to use global search without proper subscription", tenant.Id);
                return StatusCode(403, ApiResponse<object>.ErrorResponse(
                    "FEATURE_NOT_AVAILABLE",
                    "Global search is only available for Professional, Business, and Enterprise plans. Please upgrade your subscription to access this feature."));
            }

            // Validate query
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(ApiResponse<object>.ErrorResponse("INVALID_QUERY", "Search query is required"));

            // Validate pagination
            if (page < 1)
                page = 1;
            if (pageSize < 1 || pageSize > 100)
                pageSize = 20;

            // Parse search scope
            var searchScope = scope?.ToLower() switch
            {
                "all" => SearchScope.AllClients,
                "global" => SearchScope.AllClients,
                _ => SearchScope.CurrentClient
            };

            // Parse result type filter
            SearchResultType? resultType = null;
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (Enum.TryParse<SearchResultType>(type, ignoreCase: true, out var parsedType))
                    resultType = parsedType;
            }

            // Build search request
            var searchRequest = new SearchRequest
            {
                Query = q,
                Scope = searchScope,
                ClientId = clientId,
                ResultType = resultType,
                Page = page,
                PageSize = pageSize
            };

            // Execute search
            var results = await _searchService.SearchAsync(
                Guid.Parse(tenant.EntraIdTenantId),
                searchRequest);

            // Calculate pagination
            var total = results.Count;
            var skip = (page - 1) * pageSize;
            var paginatedResults = results.Skip(skip).Take(pageSize).ToList();

            // Build response
            var searchTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var response = new
            {
                success = true,
                data = paginatedResults,
                pagination = new
                {
                    page,
                    pageSize,
                    total,
                    hasNext = (skip + pageSize) < total
                },
                searchInfo = new
                {
                    query = q,
                    scope = searchScope.ToString(),
                    searchTimeMs
                }
            };

            _logger.LogInformation(
                "Global search completed with query '{Query}'. Found {Count} results in {Time}ms",
                q, total, searchTimeMs);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing global search");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("SEARCH_ERROR", "An error occurred while searching"));
        }
    }

    /// <summary>
    /// Get search suggestions (autocomplete)
    /// </summary>
    /// <param name="q">Partial query string</param>
    /// <param name="scope">Search scope (default: current)</param>
    /// <returns>List of suggestions</returns>
    [HttpGet("search/suggestions")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] string q,
        [FromQuery] string? scope = "current")
    {
        try
        {
            // Get tenant from claims
            var tenantIdClaim = User.FindFirst("tid")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
                return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));

            // Get tenant from database
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

            if (tenant == null)
                return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

            // Validate query
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(ApiResponse<List<string>>.SuccessResponse(new List<string>()));

            // Parse search scope
            var searchScope = scope?.ToLower() switch
            {
                "all" => SearchScope.AllClients,
                "global" => SearchScope.AllClients,
                _ => SearchScope.CurrentClient
            };

            // Get suggestions
            var suggestions = await _searchService.GetSuggestionsAsync(
                Guid.Parse(tenant.EntraIdTenantId),
                q,
                searchScope);

            return Ok(ApiResponse<List<string>>.SuccessResponse(suggestions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("SEARCH_ERROR", "An error occurred while getting suggestions"));
        }
    }
}

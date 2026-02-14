using SharePointExternalUserManager.Functions.Models.Search;

namespace SharePointExternalUserManager.Functions.Services.Search;

/// <summary>
/// Service interface for search operations
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Execute a search query
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="request">Search request parameters</param>
    /// <returns>List of search results</returns>
    Task<List<SearchResultDto>> SearchAsync(Guid tenantId, SearchRequest request);

    /// <summary>
    /// Search within a specific client space
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="clientId">Client space ID</param>
    /// <param name="request">Search request parameters</param>
    /// <returns>List of search results</returns>
    Task<List<SearchResultDto>> SearchClientSpaceAsync(Guid tenantId, int clientId, SearchRequest request);

    /// <summary>
    /// Get search suggestions/autocomplete
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="query">Partial query string</param>
    /// <param name="scope">Search scope</param>
    /// <returns>List of suggestions</returns>
    Task<List<string>> GetSuggestionsAsync(Guid tenantId, string query, SearchScope scope);
}

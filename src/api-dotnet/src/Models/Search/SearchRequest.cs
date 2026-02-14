namespace SharePointExternalUserManager.Functions.Models.Search;

/// <summary>
/// Request model for search operations
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// Search query string
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Search scope - current client or all clients (Pro feature)
    /// </summary>
    public SearchScope Scope { get; set; } = SearchScope.CurrentClient;

    /// <summary>
    /// Optional client ID filter (for cross-client search)
    /// </summary>
    public int? ClientId { get; set; }

    /// <summary>
    /// Optional user email filter
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Optional result type filter
    /// </summary>
    public SearchResultType? ResultType { get; set; }

    /// <summary>
    /// Optional date range filter - from
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// Optional date range filter - to
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Results per page (max 100)
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Defines the scope of the search operation
/// </summary>
public enum SearchScope
{
    /// <summary>
    /// Search within current client space only (Free tier)
    /// </summary>
    CurrentClient,

    /// <summary>
    /// Search across all client spaces (Pro tier)
    /// </summary>
    AllClients
}

/// <summary>
/// Types of searchable content
/// </summary>
public enum SearchResultType
{
    /// <summary>
    /// Document/file result
    /// </summary>
    Document,

    /// <summary>
    /// External user result
    /// </summary>
    User,

    /// <summary>
    /// Client space result
    /// </summary>
    ClientSpace,

    /// <summary>
    /// Library/list result
    /// </summary>
    Library
}

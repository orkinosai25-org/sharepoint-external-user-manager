namespace SharePointExternalUserManager.Functions.Models.Search;

/// <summary>
/// Unified search result model
/// </summary>
public class SearchResultDto
{
    /// <summary>
    /// Unique identifier for the result
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of search result
    /// </summary>
    public SearchResultType Type { get; set; }

    /// <summary>
    /// Title or name of the result
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description or snippet
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL to access the result
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Client space ID this result belongs to
    /// </summary>
    public int? ClientId { get; set; }

    /// <summary>
    /// Client space name
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Owner or author email
    /// </summary>
    public string? OwnerEmail { get; set; }

    /// <summary>
    /// Owner or author display name
    /// </summary>
    public string? OwnerDisplayName { get; set; }

    /// <summary>
    /// Date created
    /// </summary>
    public DateTime? CreatedDate { get; set; }

    /// <summary>
    /// Date last modified
    /// </summary>
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// Relevance score for ranking
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Additional metadata specific to the result type
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Response model for search operations
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// Search query that was executed
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Search scope that was used
    /// </summary>
    public SearchScope Scope { get; set; }

    /// <summary>
    /// Total time taken for the search (in milliseconds)
    /// </summary>
    public long SearchTimeMs { get; set; }

    /// <summary>
    /// Applied filters
    /// </summary>
    public SearchFiltersDto? Filters { get; set; }
}

/// <summary>
/// Applied search filters
/// </summary>
public class SearchFiltersDto
{
    public int? ClientId { get; set; }
    public string? UserEmail { get; set; }
    public SearchResultType? ResultType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

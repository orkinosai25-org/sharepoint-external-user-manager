using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Models.Search;
using SharePointExternalUserManager.Functions.Models.Clients;
using SharePointExternalUserManager.Functions.Models.ExternalUsers;
using Microsoft.Extensions.Logging;

namespace SharePointExternalUserManager.Functions.Services.Search;

/// <summary>
/// Search service implementation with mock data for MVP
/// Phase 2 will integrate with Azure AI Search or SharePoint Search API
/// </summary>
public class SearchService : ISearchService
{
    private readonly ILogger<SearchService> _logger;

    public SearchService(ILogger<SearchService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Execute a search query across all client spaces or current client
    /// </summary>
    public async Task<List<SearchResultDto>> SearchAsync(Guid tenantId, SearchRequest request)
    {
        _logger.LogInformation("Executing search for tenant {TenantId}, query: {Query}, scope: {Scope}",
            tenantId, request.Query, request.Scope);

        await Task.CompletedTask; // Placeholder for async operations

        var results = new List<SearchResultDto>();

        // Get mock data
        var documents = GetMockDocuments(tenantId);
        var users = GetMockUsers(tenantId);
        var clients = GetMockClients(tenantId);
        var libraries = GetMockLibraries(tenantId);

        // Filter by scope
        if (request.Scope == SearchScope.CurrentClient && request.ClientId.HasValue)
        {
            documents = documents.Where(d => d.ClientId == request.ClientId.Value).ToList();
            users = users.Where(u => u.ClientId == request.ClientId.Value).ToList();
            libraries = libraries.Where(l => l.ClientId == request.ClientId.Value).ToList();
        }

        // Search documents
        if (!request.ResultType.HasValue || request.ResultType == SearchResultType.Document)
        {
            var docResults = documents
                .Where(d => string.IsNullOrEmpty(request.Query) ||
                           d.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                           (d.Description?.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(d => new SearchResultDto
                {
                    Id = d.Id,
                    Type = SearchResultType.Document,
                    Title = d.Title,
                    Description = d.Description,
                    Url = d.Url,
                    ClientId = d.ClientId,
                    ClientName = d.ClientName,
                    OwnerEmail = d.OwnerEmail,
                    OwnerDisplayName = d.OwnerDisplayName,
                    CreatedDate = d.CreatedDate,
                    ModifiedDate = d.ModifiedDate,
                    Score = CalculateRelevanceScore(d.Title, request.Query),
                    Metadata = new Dictionary<string, string>
                    {
                        { "FileType", d.FileType ?? "unknown" },
                        { "Size", d.Size.ToString() }
                    }
                });

            results.AddRange(docResults);
        }

        // Search users
        if (!request.ResultType.HasValue || request.ResultType == SearchResultType.User)
        {
            var userResults = users
                .Where(u => string.IsNullOrEmpty(request.Query) ||
                           u.Email.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                           u.DisplayName.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
                .Select(u => new SearchResultDto
                {
                    Id = u.Email,
                    Type = SearchResultType.User,
                    Title = u.DisplayName,
                    Description = u.Email,
                    ClientId = u.ClientId,
                    ClientName = u.ClientName,
                    OwnerEmail = u.Email,
                    OwnerDisplayName = u.DisplayName,
                    CreatedDate = u.InvitedDate,
                    Score = CalculateRelevanceScore(u.DisplayName, request.Query),
                    Metadata = new Dictionary<string, string>
                    {
                        { "PermissionLevel", u.PermissionLevel },
                        { "Status", u.Status }
                    }
                });

            results.AddRange(userResults);
        }

        // Search client spaces
        if (!request.ResultType.HasValue || request.ResultType == SearchResultType.ClientSpace)
        {
            var clientResults = clients
                .Where(c => string.IsNullOrEmpty(request.Query) ||
                           c.ClientName.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                           c.ClientReference.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                           (c.Description?.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(c => new SearchResultDto
                {
                    Id = c.Id.ToString(),
                    Type = SearchResultType.ClientSpace,
                    Title = c.ClientName,
                    Description = c.Description,
                    Url = c.SharePointSiteUrl,
                    ClientId = c.Id,
                    ClientName = c.ClientName,
                    CreatedDate = c.CreatedDate,
                    Score = CalculateRelevanceScore(c.ClientName, request.Query),
                    Metadata = new Dictionary<string, string>
                    {
                        { "ClientReference", c.ClientReference },
                        { "ProvisioningStatus", c.ProvisioningStatus },
                        { "IsActive", c.IsActive.ToString() }
                    }
                });

            results.AddRange(clientResults);
        }

        // Search libraries
        if (!request.ResultType.HasValue || request.ResultType == SearchResultType.Library)
        {
            var libraryResults = libraries
                .Where(l => string.IsNullOrEmpty(request.Query) ||
                           l.Name.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                           (l.Description?.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(l => new SearchResultDto
                {
                    Id = l.Id.ToString(),
                    Type = SearchResultType.Library,
                    Title = l.Name,
                    Description = l.Description,
                    Url = l.SiteUrl,
                    ClientId = l.ClientId,
                    ClientName = l.ClientName,
                    OwnerEmail = l.OwnerEmail,
                    OwnerDisplayName = l.OwnerDisplayName,
                    CreatedDate = l.CreatedDate,
                    Score = CalculateRelevanceScore(l.Name, request.Query),
                    Metadata = new Dictionary<string, string>
                    {
                        { "ExternalUserCount", l.ExternalUserCount.ToString() },
                        { "ExternalSharingEnabled", l.ExternalSharingEnabled.ToString() }
                    }
                });

            results.AddRange(libraryResults);
        }

        // Apply additional filters
        results = ApplyFilters(results, request);

        // Sort by relevance score
        results = results.OrderByDescending(r => r.Score).ToList();

        _logger.LogInformation("Search returned {Count} results", results.Count);

        return results;
    }

    /// <summary>
    /// Search within a specific client space
    /// </summary>
    public async Task<List<SearchResultDto>> SearchClientSpaceAsync(Guid tenantId, int clientId, SearchRequest request)
    {
        _logger.LogInformation("Searching client space {ClientId} for tenant {TenantId}", clientId, tenantId);

        // Set the scope and client ID for the search
        request.Scope = SearchScope.CurrentClient;
        request.ClientId = clientId;

        return await SearchAsync(tenantId, request);
    }

    /// <summary>
    /// Get search suggestions (autocomplete)
    /// </summary>
    public async Task<List<string>> GetSuggestionsAsync(Guid tenantId, string query, SearchScope scope)
    {
        _logger.LogInformation("Getting suggestions for query: {Query}, scope: {Scope}", query, scope);

        await Task.CompletedTask;

        var suggestions = new List<string>();

        if (string.IsNullOrWhiteSpace(query))
            return suggestions;

        // Mock suggestions based on common searches
        var allSuggestions = new List<string>
        {
            "Annual Report 2024",
            "Annual Budget Documents",
            "Client Agreement Template",
            "Client Contracts",
            "Project Documents",
            "Project Plan 2024",
            "Marketing Materials",
            "Financial Reports",
            "Legal Documents",
            "Compliance Documents"
        };

        suggestions = allSuggestions
            .Where(s => s.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        return suggestions;
    }

    #region Private Helper Methods

    private List<SearchResultDto> ApplyFilters(List<SearchResultDto> results, SearchRequest request)
    {
        // Filter by client ID
        if (request.ClientId.HasValue)
        {
            results = results.Where(r => r.ClientId == request.ClientId.Value).ToList();
        }

        // Filter by user email
        if (!string.IsNullOrWhiteSpace(request.UserEmail))
        {
            results = results.Where(r =>
                r.OwnerEmail?.Equals(request.UserEmail, StringComparison.OrdinalIgnoreCase) ?? false
            ).ToList();
        }

        // Filter by date range
        if (request.DateFrom.HasValue)
        {
            results = results.Where(r =>
                r.ModifiedDate.HasValue && r.ModifiedDate.Value >= request.DateFrom.Value
            ).ToList();
        }

        if (request.DateTo.HasValue)
        {
            results = results.Where(r =>
                r.ModifiedDate.HasValue && r.ModifiedDate.Value <= request.DateTo.Value
            ).ToList();
        }

        return results;
    }

    private double CalculateRelevanceScore(string text, string query)
    {
        if (string.IsNullOrEmpty(query))
            return 0.5;

        text = text.ToLowerInvariant();
        query = query.ToLowerInvariant();

        // Exact match gets highest score
        if (text == query)
            return 1.0;

        // Starts with query gets high score
        if (text.StartsWith(query))
            return 0.9;

        // Contains query gets medium score
        if (text.Contains(query))
            return 0.7;

        // Fuzzy match based on word overlap
        var textWords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matchCount = queryWords.Count(qw => textWords.Any(tw => tw.Contains(qw)));

        return 0.3 + (0.4 * matchCount / queryWords.Length);
    }

    #endregion

    #region Mock Data Methods

    private List<MockDocument> GetMockDocuments(Guid tenantId)
    {
        return new List<MockDocument>
        {
            new MockDocument
            {
                Id = "doc-1",
                Title = "Annual Report 2024",
                Description = "Comprehensive annual report for 2024",
                Url = "https://contoso.sharepoint.com/sites/client1/Documents/Annual-Report-2024.pdf",
                ClientId = 1,
                ClientName = "Acme Corporation",
                OwnerEmail = "john.doe@contoso.com",
                OwnerDisplayName = "John Doe",
                CreatedDate = DateTime.UtcNow.AddMonths(-2),
                ModifiedDate = DateTime.UtcNow.AddDays(-5),
                FileType = "pdf",
                Size = 2048576
            },
            new MockDocument
            {
                Id = "doc-2",
                Title = "Project Plan Q1 2024",
                Description = "Quarterly project planning document",
                Url = "https://contoso.sharepoint.com/sites/client1/Documents/Project-Plan-Q1.docx",
                ClientId = 1,
                ClientName = "Acme Corporation",
                OwnerEmail = "jane.smith@contoso.com",
                OwnerDisplayName = "Jane Smith",
                CreatedDate = DateTime.UtcNow.AddMonths(-1),
                ModifiedDate = DateTime.UtcNow.AddDays(-2),
                FileType = "docx",
                Size = 512000
            },
            new MockDocument
            {
                Id = "doc-3",
                Title = "Client Agreement",
                Description = "Legal agreement with client",
                Url = "https://contoso.sharepoint.com/sites/client2/Documents/Agreement.pdf",
                ClientId = 2,
                ClientName = "Beta Industries",
                OwnerEmail = "legal@contoso.com",
                OwnerDisplayName = "Legal Team",
                CreatedDate = DateTime.UtcNow.AddMonths(-3),
                ModifiedDate = DateTime.UtcNow.AddDays(-10),
                FileType = "pdf",
                Size = 1024000
            },
            new MockDocument
            {
                Id = "doc-4",
                Title = "Marketing Materials 2024",
                Description = "Marketing collateral and materials",
                Url = "https://contoso.sharepoint.com/sites/client2/Documents/Marketing.pptx",
                ClientId = 2,
                ClientName = "Beta Industries",
                OwnerEmail = "marketing@contoso.com",
                OwnerDisplayName = "Marketing Team",
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                ModifiedDate = DateTime.UtcNow.AddDays(-1),
                FileType = "pptx",
                Size = 3145728
            }
        };
    }

    private List<MockUser> GetMockUsers(Guid tenantId)
    {
        return new List<MockUser>
        {
            new MockUser
            {
                Email = "external.user1@partner.com",
                DisplayName = "Alice Johnson",
                ClientId = 1,
                ClientName = "Acme Corporation",
                PermissionLevel = "Read",
                Status = "Active",
                InvitedDate = DateTime.UtcNow.AddMonths(-2)
            },
            new MockUser
            {
                Email = "external.user2@vendor.com",
                DisplayName = "Bob Wilson",
                ClientId = 1,
                ClientName = "Acme Corporation",
                PermissionLevel = "Edit",
                Status = "Active",
                InvitedDate = DateTime.UtcNow.AddMonths(-1)
            },
            new MockUser
            {
                Email = "consultant@external.com",
                DisplayName = "Carol Martinez",
                ClientId = 2,
                ClientName = "Beta Industries",
                PermissionLevel = "Read",
                Status = "Active",
                InvitedDate = DateTime.UtcNow.AddDays(-20)
            }
        };
    }

    private List<MockClient> GetMockClients(Guid tenantId)
    {
        return new List<MockClient>
        {
            new MockClient
            {
                Id = 1,
                ClientReference = "CLI-001",
                ClientName = "Acme Corporation",
                Description = "Technology consulting project",
                SharePointSiteUrl = "https://contoso.sharepoint.com/sites/acme",
                ProvisioningStatus = "Completed",
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            },
            new MockClient
            {
                Id = 2,
                ClientReference = "CLI-002",
                ClientName = "Beta Industries",
                Description = "Marketing and design services",
                SharePointSiteUrl = "https://contoso.sharepoint.com/sites/beta",
                ProvisioningStatus = "Completed",
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-4)
            },
            new MockClient
            {
                Id = 3,
                ClientReference = "CLI-003",
                ClientName = "Gamma Solutions",
                Description = "Software development engagement",
                SharePointSiteUrl = "https://contoso.sharepoint.com/sites/gamma",
                ProvisioningStatus = "Completed",
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-2)
            }
        };
    }

    private List<MockLibrary> GetMockLibraries(Guid tenantId)
    {
        return new List<MockLibrary>
        {
            new MockLibrary
            {
                Id = Guid.NewGuid(),
                Name = "Project Documents",
                Description = "Main project documentation library",
                SiteUrl = "https://contoso.sharepoint.com/sites/acme/ProjectDocs",
                ClientId = 1,
                ClientName = "Acme Corporation",
                OwnerEmail = "john.doe@contoso.com",
                OwnerDisplayName = "John Doe",
                ExternalUserCount = 3,
                ExternalSharingEnabled = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-5)
            },
            new MockLibrary
            {
                Id = Guid.NewGuid(),
                Name = "Contracts",
                Description = "Legal contracts and agreements",
                SiteUrl = "https://contoso.sharepoint.com/sites/beta/Contracts",
                ClientId = 2,
                ClientName = "Beta Industries",
                OwnerEmail = "legal@contoso.com",
                OwnerDisplayName = "Legal Team",
                ExternalUserCount = 1,
                ExternalSharingEnabled = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-3)
            }
        };
    }

    #endregion

    #region Mock Data Classes

    private class MockDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Url { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string OwnerDisplayName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string? FileType { get; set; }
        public long Size { get; set; }
    }

    private class MockUser
    {
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string PermissionLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime InvitedDate { get; set; }
    }

    private class MockClient
    {
        public int Id { get; set; }
        public string ClientReference { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SharePointSiteUrl { get; set; }
        public string ProvisioningStatus { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    private class MockLibrary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string SiteUrl { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string OwnerDisplayName { get; set; } = string.Empty;
        public int ExternalUserCount { get; set; }
        public bool ExternalSharingEnabled { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    #endregion
}

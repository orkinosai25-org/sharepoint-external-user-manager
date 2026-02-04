using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Functions.Functions.UserManagement;

public class GetLibrariesFunction
{
    private readonly ILogger<GetLibrariesFunction> _logger;

    public GetLibrariesFunction(ILogger<GetLibrariesFunction> logger)
    {
        _logger = logger;
    }

    [Function("GetLibraries")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/libraries")] HttpRequestData req)
    {
        _logger.LogInformation("Get libraries request received");

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
            var page = int.TryParse(query["page"], out var p) ? p : 1;
            var pageSize = int.TryParse(query["pageSize"], out var ps) ? Math.Min(ps, 100) : 50;
            var search = query["search"];
            var owner = query["owner"];

            _logger.LogInformation("Fetching libraries for tenant: {TenantId}, page: {Page}, pageSize: {PageSize}", 
                tenantId, page, pageSize);

            // TODO: Implement actual database query
            // For MVP, return mock data
            var libraries = GetMockLibraries(tenantId);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                libraries = libraries.Where(l => 
                    l.LibraryName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (l.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(owner))
            {
                libraries = libraries.Where(l => 
                    l.OwnerEmail.Equals(owner, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Apply pagination
            var total = libraries.Count;
            var pagedLibraries = libraries
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pagination = new PaginationMeta
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                HasNext = page * pageSize < total
            };

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<List<Library>>.SuccessResponse(pagedLibraries, pagination));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching libraries");
            return await CreateErrorResponse(req, System.Net.HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR", "An error occurred while fetching libraries");
        }
    }

    private List<Library> GetMockLibraries(Guid tenantId)
    {
        // Mock data for testing
        return new List<Library>
        {
            new Library
            {
                LibraryId = Guid.NewGuid(),
                SharePointSiteId = "contoso.sharepoint.com,site-1,lib-1",
                SharePointLibraryId = "lib-guid-1",
                LibraryName = "External Projects",
                Description = "Documents shared with external partners",
                SiteUrl = "https://contoso.sharepoint.com/sites/external-projects",
                OwnerEmail = "john.doe@contoso.com",
                OwnerDisplayName = "John Doe",
                ExternalSharingEnabled = true,
                ExternalUserCount = 5,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-2),
                CreatedBy = "john.doe@contoso.com",
                ModifiedDate = DateTime.UtcNow.AddDays(-5),
                LastSyncDate = DateTime.UtcNow.AddHours(-1)
            },
            new Library
            {
                LibraryId = Guid.NewGuid(),
                SharePointSiteId = "contoso.sharepoint.com,site-2,lib-2",
                SharePointLibraryId = "lib-guid-2",
                LibraryName = "Partner Documents",
                Description = "Collaboration space for vendor partners",
                SiteUrl = "https://contoso.sharepoint.com/sites/partners",
                OwnerEmail = "jane.smith@contoso.com",
                OwnerDisplayName = "Jane Smith",
                ExternalSharingEnabled = true,
                ExternalUserCount = 3,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-1),
                CreatedBy = "jane.smith@contoso.com",
                ModifiedDate = DateTime.UtcNow.AddDays(-2),
                LastSyncDate = DateTime.UtcNow.AddHours(-2)
            }
        };
    }

    private async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req,
        System.Net.HttpStatusCode statusCode,
        string errorCode,
        string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(ApiResponse<object>.ErrorResponse(errorCode, message));
        return response;
    }
}

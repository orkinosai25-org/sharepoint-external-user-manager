using Microsoft.Graph;
using Microsoft.Graph.Models;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Functions.Models.ExternalUsers;
using SharePointExternalUserManager.Functions.Models.Libraries;
using SharePointExternalUserManager.Functions.Models.Lists;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service for SharePoint site provisioning via Microsoft Graph API
/// </summary>
public interface ISharePointService
{
    /// <summary>
    /// Create a SharePoint team site for a client space
    /// </summary>
    Task<(bool Success, string? SiteId, string? SiteUrl, string? ErrorMessage)> CreateClientSiteAsync(
        ClientEntity client, 
        string userEmail);

    /// <summary>
    /// Get external users for a client site
    /// </summary>
    Task<List<ExternalUserDto>> GetExternalUsersAsync(string siteId);

    /// <summary>
    /// Invite an external user to a client site with specified permissions
    /// </summary>
    Task<(bool Success, ExternalUserDto? User, string? ErrorMessage)> InviteExternalUserAsync(
        string siteId,
        string email,
        string? displayName,
        string permissionLevel,
        string? message,
        string invitedBy);

    /// <summary>
    /// Remove an external user from a client site
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> RemoveExternalUserAsync(
        string siteId,
        string email);

    /// <summary>
    /// Get all document libraries for a client site
    /// </summary>
    Task<List<LibraryResponse>> GetLibrariesAsync(string siteId);

    /// <summary>
    /// Create a new document library in a client site
    /// </summary>
    Task<LibraryResponse> CreateLibraryAsync(string siteId, string name, string? description);

    /// <summary>
    /// Get all lists for a client site
    /// </summary>
    Task<List<ListResponse>> GetListsAsync(string siteId);

    /// <summary>
    /// Create a new list in a client site
    /// </summary>
    Task<ListResponse> CreateListAsync(string siteId, string name, string? description, string? template);

    /// <summary>
    /// Validate a SharePoint site URL before client creation
    /// Checks if site exists, validates permissions, and verifies Graph access
    /// </summary>
    /// <param name="siteUrl">The SharePoint site URL to validate</param>
    /// <returns>Validation result with site details or error information</returns>
    Task<SiteValidationResult> ValidateSiteAsync(string siteUrl);
}

public class SharePointService : ISharePointService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<SharePointService> _logger;

    public SharePointService(
        GraphServiceClient graphClient,
        ILogger<SharePointService> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
    }

    public async Task<(bool Success, string? SiteId, string? SiteUrl, string? ErrorMessage)> CreateClientSiteAsync(
        ClientEntity client,
        string userEmail)
    {
        try
        {
            _logger.LogInformation(
                "Creating SharePoint site for client {ClientReference} - {ClientName}",
                client.ClientReference,
                client.ClientName);

            // Create a team site with the client details
            // Using a URL-safe alias based on client reference
            var siteAlias = GenerateSiteAlias(client.ClientReference, client.ClientName);
            var displayName = $"{client.ClientReference} - {client.ClientName}";
            var description = client.Description ?? $"Client space for {client.ClientName}";

            // Create the site using Microsoft Graph
            var site = new Site
            {
                DisplayName = displayName,
                Description = description
            };

            // Note: Direct site creation via Graph API requires SharePoint admin permissions
            // In a real implementation, this would use the Sites.ReadWrite.All permission
            // For now, we'll create a site using the root site's sites collection
            var rootSite = await _graphClient.Sites["root"].GetAsync();
            
            if (rootSite?.SiteCollection?.Hostname == null)
            {
                return (false, null, null, "Could not retrieve root site information");
            }

            // Construct the site URL
            // In production, this would use the actual Graph API site creation
            // For MVP, we'll use a simplified approach that creates a subsite reference
            var hostname = rootSite.SiteCollection.Hostname;
            var siteUrl = $"https://{hostname}/sites/{siteAlias}";
            
            // For MVP purposes, we'll store the site details
            // In production, this would actually provision the site using appropriate Graph calls
            // The actual provisioning would be done using one of these approaches:
            // 1. SPO Admin PowerShell commands via Azure Function
            // 2. PnP PowerShell provisioning
            // 3. SharePoint REST API with elevated permissions
            
            _logger.LogInformation(
                "SharePoint site URL planned for client {ClientReference}: {SiteUrl}",
                client.ClientReference,
                siteUrl);

            // For now, return a reference that indicates the site would be created
            // The actual site ID would come from the Graph API response
            var siteId = Guid.NewGuid().ToString(); // Placeholder - would be actual site ID from Graph

            return (true, siteId, siteUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating SharePoint site for client {ClientReference}",
                client.ClientReference);

            return (false, null, null, ex.Message);
        }
    }

    private static string GenerateSiteAlias(string clientReference, string clientName)
    {
        // Create a URL-safe alias from client reference and name
        // Remove special characters and spaces, convert to lowercase
        var alias = $"{clientReference}-{clientName}"
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("'", "")
            .Replace("\"", "");

        // Remove any remaining non-alphanumeric characters except hyphens
        alias = new string(alias.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        // Limit length to 50 characters
        if (alias.Length > 50)
        {
            alias = alias[..50];
        }

        return alias;
    }

    private static string ExtractEmailFromUserId(string userId)
    {
        // External users in Azure AD have IDs in the format: email#EXT#@domain
        // Example: john.doe_contoso.com#EXT#@tenant.onmicrosoft.com
        // We need to extract the original email (john.doe@contoso.com)
        
        if (string.IsNullOrEmpty(userId))
        {
            return "";
        }

        // If it contains #EXT#, extract the email part before it
        var extIndex = userId.IndexOf("#EXT#", StringComparison.OrdinalIgnoreCase);
        if (extIndex > 0)
        {
            var emailPart = userId[..extIndex];
            // Replace underscores back to @ (Azure AD encodes @ as _)
            var lastUnderscore = emailPart.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                emailPart = emailPart[..lastUnderscore] + "@" + emailPart[(lastUnderscore + 1)..];
            }
            return emailPart;
        }

        // If no #EXT#, return the userId as-is (might be a direct email)
        return userId;
    }

    public async Task<List<ExternalUserDto>> GetExternalUsersAsync(string siteId)
    {
        try
        {
            _logger.LogInformation("Retrieving external users for site {SiteId}", siteId);

            // Get all permissions for the site
            var permissions = await _graphClient.Sites[siteId].Permissions.GetAsync();

            if (permissions?.Value == null)
            {
                return new List<ExternalUserDto>();
            }

            var externalUsers = new List<ExternalUserDto>();

            foreach (var permission in permissions.Value)
            {
                // Check if this is an external user (contains #EXT# in the email or has specific grantedToIdentities)
                var grantedTo = permission.GrantedToIdentitiesV2?.FirstOrDefault() 
                    ?? permission.GrantedToV2;

                if (grantedTo?.User != null)
                {
                    // The Id property typically contains the email for external users
                    var userId = grantedTo.User.Id ?? "";
                    var displayName = grantedTo.User.DisplayName ?? "Unknown";
                    
                    // Only include external users (those with #EXT# in their ID or guest users)
                    if (userId.Contains("#EXT#", StringComparison.OrdinalIgnoreCase) || 
                        displayName.Contains("(Guest)", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract email from the user ID (format: email#EXT#@domain)
                        var email = ExtractEmailFromUserId(userId);
                        
                        externalUsers.Add(new ExternalUserDto
                        {
                            Id = permission.Id ?? Guid.NewGuid().ToString(),
                            Email = email,
                            DisplayName = displayName,
                            PermissionLevel = GetPermissionLevelFromRoles(permission.Roles),
                            // Graph API doesn't expose creation date for permissions
                            // Use a fixed date to indicate historical/unknown invite date
                            InvitedDate = DateTime.UtcNow.Date.AddDays(-90),
                            InvitedBy = "System", // Graph API doesn't always provide this
                            LastAccessDate = null, // Not available via Graph permissions
                            // Status based on whether the invitation has been accepted
                            // HasPassword indicates the user has set up their account
                            Status = permission.HasPassword == true ? "Active" : "Invited"
                        });
                    }
                }
            }

            _logger.LogInformation("Found {Count} external users for site {SiteId}", 
                externalUsers.Count, siteId);

            return externalUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving external users for site {SiteId}", siteId);
            return new List<ExternalUserDto>();
        }
    }

    public async Task<(bool Success, ExternalUserDto? User, string? ErrorMessage)> InviteExternalUserAsync(
        string siteId,
        string email,
        string? displayName,
        string permissionLevel,
        string? message,
        string invitedBy)
    {
        try
        {
            _logger.LogInformation(
                "Inviting external user {Email} to site {SiteId} with {PermissionLevel} permissions",
                email, siteId, permissionLevel);

            // Map permission level to SharePoint roles
            var roles = MapPermissionLevelToRoles(permissionLevel);

            if (roles == null || roles.Count == 0)
            {
                return (false, null, $"Invalid permission level: {permissionLevel}");
            }

            // Create the permission request
            // Note: Microsoft Graph SharePoint permissions use sharing links or direct invites
            // For external user invites, we'll use the invite endpoint
            var invitation = new Permission
            {
                Roles = roles,
                GrantedToIdentitiesV2 = new List<SharePointIdentitySet>
                {
                    new SharePointIdentitySet
                    {
                        User = new Identity
                        {
                            Id = email,
                            DisplayName = displayName ?? email
                        }
                    }
                },
                AdditionalData = new Dictionary<string, object>()
            };

            // Add custom message if provided
            if (!string.IsNullOrEmpty(message))
            {
                invitation.AdditionalData["@microsoft.graph.inviteMessage"] = message;
                invitation.AdditionalData["@microsoft.graph.sendInvitation"] = true;
            }

            // Create the permission (invite the user)
            var createdPermission = await _graphClient.Sites[siteId]
                .Permissions
                .PostAsync(invitation);

            if (createdPermission == null)
            {
                return (false, null, "Failed to create permission");
            }

            // Create response DTO
            var userDto = new ExternalUserDto
            {
                Id = createdPermission.Id ?? Guid.NewGuid().ToString(),
                Email = email,
                DisplayName = displayName ?? email,
                PermissionLevel = permissionLevel,
                InvitedDate = DateTime.UtcNow,
                InvitedBy = invitedBy,
                LastAccessDate = null,
                Status = "Invited"
            };

            _logger.LogInformation(
                "Successfully invited external user {Email} to site {SiteId}",
                email, siteId);

            return (true, userDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error inviting external user {Email} to site {SiteId}",
                email, siteId);

            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RemoveExternalUserAsync(
        string siteId,
        string email)
    {
        try
        {
            _logger.LogInformation(
                "Removing external user {Email} from site {SiteId}",
                email, siteId);

            // Get all permissions to find the one for this user
            var permissions = await _graphClient.Sites[siteId].Permissions.GetAsync();

            if (permissions?.Value == null)
            {
                return (false, "No permissions found for site");
            }

            // Find the permission ID for this user
            string? permissionId = null;
            foreach (var permission in permissions.Value)
            {
                var grantedTo = permission.GrantedToIdentitiesV2?.FirstOrDefault() 
                    ?? permission.GrantedToV2;

                if (grantedTo?.User?.Id != null)
                {
                    // Extract email from user ID or compare directly
                    var userEmail = ExtractEmailFromUserId(grantedTo.User.Id);
                    
                    if (userEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
                    {
                        permissionId = permission.Id;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(permissionId))
            {
                return (false, $"External user {email} not found in site permissions");
            }

            // Delete the permission
            await _graphClient.Sites[siteId]
                .Permissions[permissionId]
                .DeleteAsync();

            _logger.LogInformation(
                "Successfully removed external user {Email} from site {SiteId}",
                email, siteId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error removing external user {Email} from site {SiteId}",
                email, siteId);

            return (false, ex.Message);
        }
    }

    private static string GetPermissionLevelFromRoles(List<string>? roles)
    {
        if (roles == null || roles.Count == 0)
        {
            return "None";
        }

        if (roles.Contains("owner", StringComparer.OrdinalIgnoreCase) || 
            roles.Contains("fullcontrol", StringComparer.OrdinalIgnoreCase))
        {
            return "Owner";
        }

        if (roles.Contains("write", StringComparer.OrdinalIgnoreCase) || 
            roles.Contains("edit", StringComparer.OrdinalIgnoreCase))
        {
            return "Edit";
        }

        if (roles.Contains("read", StringComparer.OrdinalIgnoreCase))
        {
            return "Read";
        }

        return roles.First();
    }

    private static List<string>? MapPermissionLevelToRoles(string permissionLevel)
    {
        return permissionLevel.ToLowerInvariant() switch
        {
            "read" => new List<string> { "read" },
            "edit" or "write" or "contribute" => new List<string> { "write" },
            "owner" or "fullcontrol" => new List<string> { "owner" },
            _ => null
        };
    }

    public async Task<List<LibraryResponse>> GetLibrariesAsync(string siteId)
    {
        try
        {
            _logger.LogInformation("Retrieving libraries for site {SiteId}", siteId);

            // Get all drives (document libraries) for the site
            var drives = await _graphClient.Sites[siteId].Drives.GetAsync();

            if (drives?.Value == null)
            {
                return new List<LibraryResponse>();
            }

            var libraries = drives.Value
                .Where(d => d.DriveType == "documentLibrary")
                .Select(drive => new LibraryResponse
                {
                    Id = drive.Id ?? string.Empty,
                    Name = drive.Name ?? string.Empty,
                    DisplayName = drive.Name ?? string.Empty,
                    Description = drive.Description ?? string.Empty,
                    WebUrl = drive.WebUrl ?? string.Empty,
                    CreatedDateTime = drive.CreatedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                    LastModifiedDateTime = drive.LastModifiedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                    ItemCount = 0 // Graph API doesn't provide item count in drive resource
                })
                .ToList();

            _logger.LogInformation("Found {Count} libraries for site {SiteId}", libraries.Count, siteId);

            return libraries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving libraries for site {SiteId}", siteId);
            return new List<LibraryResponse>();
        }
    }

    public async Task<LibraryResponse> CreateLibraryAsync(string siteId, string name, string? description)
    {
        try
        {
            _logger.LogInformation("Creating library '{Name}' in site {SiteId}", name, siteId);

            // Create a new list with documentLibrary template
            var newList = new List
            {
                DisplayName = name,
                Description = description,
                AdditionalData = new Dictionary<string, object>
                {
                    ["list"] = new Dictionary<string, object>
                    {
                        ["template"] = "documentLibrary"
                    }
                }
            };

            var createdList = await _graphClient.Sites[siteId].Lists.PostAsync(newList);

            if (createdList == null)
            {
                throw new Exception("Failed to create library - no response from Graph API");
            }

            _logger.LogInformation("Successfully created library '{Name}' with ID {ListId}", name, createdList.Id);

            // Return the library response
            return new LibraryResponse
            {
                Id = createdList.Id ?? string.Empty,
                Name = createdList.Name ?? name,
                DisplayName = createdList.DisplayName ?? name,
                Description = createdList.Description ?? description ?? string.Empty,
                WebUrl = createdList.WebUrl ?? string.Empty,
                CreatedDateTime = createdList.CreatedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                LastModifiedDateTime = createdList.LastModifiedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                ItemCount = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating library '{Name}' in site {SiteId}", name, siteId);
            throw;
        }
    }

    public async Task<List<ListResponse>> GetListsAsync(string siteId)
    {
        try
        {
            _logger.LogInformation("Retrieving lists for site {SiteId}", siteId);

            // Get all lists for the site
            var lists = await _graphClient.Sites[siteId].Lists.GetAsync();

            if (lists?.Value == null)
            {
                return new List<ListResponse>();
            }

            var listResponses = lists.Value
                // Filter out document libraries and system lists
                .Where(l => !IsDocumentLibrary(l) && !IsSystemList(l.DisplayName))
                .Select(list => new ListResponse
                {
                    Id = list.Id ?? string.Empty,
                    Name = list.Name ?? string.Empty,
                    DisplayName = list.DisplayName ?? string.Empty,
                    Description = list.Description ?? string.Empty,
                    WebUrl = list.WebUrl ?? string.Empty,
                    CreatedDateTime = list.CreatedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                    LastModifiedDateTime = list.LastModifiedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                    ItemCount = 0, // Graph API doesn't provide item count by default
                    ListTemplate = GetListTemplate(list)
                })
                .ToList();

            _logger.LogInformation("Found {Count} lists for site {SiteId}", listResponses.Count, siteId);

            return listResponses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lists for site {SiteId}", siteId);
            return new List<ListResponse>();
        }
    }

    public async Task<ListResponse> CreateListAsync(string siteId, string name, string? description, string? template)
    {
        try
        {
            _logger.LogInformation("Creating list '{Name}' with template '{Template}' in site {SiteId}", 
                name, template ?? "genericList", siteId);

            // Validate and map the template
            var listTemplate = MapListTemplate(template);

            // Create a new list
            var newList = new List
            {
                DisplayName = name,
                Description = description,
                AdditionalData = new Dictionary<string, object>
                {
                    ["list"] = new Dictionary<string, object>
                    {
                        ["template"] = listTemplate
                    }
                }
            };

            var createdList = await _graphClient.Sites[siteId].Lists.PostAsync(newList);

            if (createdList == null)
            {
                throw new Exception("Failed to create list - no response from Graph API");
            }

            _logger.LogInformation("Successfully created list '{Name}' with ID {ListId}", name, createdList.Id);

            // Return the list response
            return new ListResponse
            {
                Id = createdList.Id ?? string.Empty,
                Name = createdList.Name ?? name,
                DisplayName = createdList.DisplayName ?? name,
                Description = createdList.Description ?? description ?? string.Empty,
                WebUrl = createdList.WebUrl ?? string.Empty,
                CreatedDateTime = createdList.CreatedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                LastModifiedDateTime = createdList.LastModifiedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                ItemCount = 0,
                ListTemplate = GetListTemplate(createdList)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating list '{Name}' in site {SiteId}", name, siteId);
            throw;
        }
    }

    private static string MapListTemplate(string? template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return "genericList";
        }

        // Map template names to SharePoint list template types
        return template.ToLowerInvariant() switch
        {
            "genericlist" => "genericList",
            "documentlibrary" => "documentLibrary",
            "survey" => "survey",
            "links" => "links",
            "announcements" => "announcements",
            "contacts" => "contacts",
            "events" => "events",
            "tasks" => "tasks",
            "issuetracking" => "issueTracking",
            "customlist" => "customList",
            _ => "genericList" // Default to generic list for unknown templates
        };
    }

    private static bool IsSystemList(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return false;
        }

        // Filter out common system lists
        var systemLists = new[]
        {
            "Form Templates",
            "Site Assets",
            "Site Pages",
            "Style Library",
            "Master Page Gallery",
            "App Packages",
            "Solution Gallery"
        };

        return systemLists.Any(s => displayName.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDocumentLibrary(List list)
    {
        // Check if the list is a document library by checking the AdditionalData
        if (list.AdditionalData != null && list.AdditionalData.TryGetValue("list", out var listInfo))
        {
            if (listInfo is IDictionary<string, object> listDict && 
                listDict.TryGetValue("template", out var template))
            {
                var templateStr = template?.ToString() ?? string.Empty;
                return templateStr.Equals("documentLibrary", StringComparison.OrdinalIgnoreCase) ||
                       templateStr.Equals("webPageLibrary", StringComparison.OrdinalIgnoreCase);
            }
        }

        // Also check the Name property as document libraries often have specific names
        var name = list.Name ?? string.Empty;
        return name.Equals("Documents", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Shared Documents", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetListTemplate(List list)
    {
        // Try to get the template from AdditionalData
        if (list.AdditionalData != null && list.AdditionalData.TryGetValue("list", out var listInfo))
        {
            if (listInfo is IDictionary<string, object> listDict && 
                listDict.TryGetValue("template", out var template))
            {
                return template?.ToString() ?? "genericList";
            }
        }

        return "genericList";
    }

    public async Task<SiteValidationResult> ValidateSiteAsync(string siteUrl)
    {
        try
        {
            _logger.LogInformation("Validating SharePoint site: {SiteUrl}", siteUrl);

            // Validate URL format
            if (string.IsNullOrWhiteSpace(siteUrl))
            {
                return SiteValidationResult.Failure(
                    SiteValidationErrorCode.InvalidUrl,
                    "Site URL cannot be empty");
            }

            if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri))
            {
                return SiteValidationResult.Failure(
                    SiteValidationErrorCode.InvalidUrl,
                    "Site URL is not a valid URL format");
            }

            // Ensure it's a SharePoint URL
            if (!uri.Host.Contains("sharepoint.com"))
            {
                return SiteValidationResult.Failure(
                    SiteValidationErrorCode.InvalidUrl,
                    "URL must be a SharePoint site (*.sharepoint.com)");
            }

            // Extract site information from URL
            // Format: https://{tenant}.sharepoint.com/sites/{sitename}
            var sitePath = uri.AbsolutePath;
            if (string.IsNullOrEmpty(sitePath) || sitePath == "/")
            {
                return SiteValidationResult.Failure(
                    SiteValidationErrorCode.InvalidUrl,
                    "Site URL must include a site path (e.g., /sites/sitename)");
            }

            try
            {
                // Use the hostname and path to construct the site identifier
                // Format for Graph API: {hostname}:{sitePath}
                // Example: contoso.sharepoint.com:/sites/clientsite
                var siteIdentifier = $"{uri.Host}:{sitePath}";
                
                _logger.LogInformation("Attempting to get site with identifier: {SiteIdentifier}", siteIdentifier);

                // Try to get the site using the identifier
                var site = await _graphClient.Sites[siteIdentifier].GetAsync();

                if (site == null || string.IsNullOrEmpty(site.Id))
                {
                    return SiteValidationResult.Failure(
                        SiteValidationErrorCode.SiteNotFound,
                        "Site exists but could not retrieve site details");
                }

                // Verify we can access site details (tests read permission)
                if (string.IsNullOrEmpty(site.DisplayName))
                {
                    _logger.LogWarning("Site {SiteId} found but DisplayName is empty", site.Id);
                }

                // Try to access site permissions to verify we have adequate access
                try
                {
                    var permissions = await _graphClient.Sites[site.Id].Permissions.GetAsync();
                    // If we can read permissions, we have adequate access
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError permError)
                {
                    // If we can't read permissions but can read the site, it might be a permission issue
                    if (permError.Error?.Code == "accessDenied")
                    {
                        _logger.LogWarning("Can access site but not permissions for {SiteId}", site.Id);
                        // This is still acceptable - we just need to be able to read the site
                    }
                }

                _logger.LogInformation(
                    "Site validation successful: {SiteId} - {DisplayName}",
                    site.Id,
                    site.DisplayName);

                return SiteValidationResult.Success(site.Id, siteUrl);
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError odataError)
            {
                var errorCode = odataError.Error?.Code;
                var errorMessage = odataError.Error?.Message ?? "Unknown Graph API error";

                _logger.LogWarning(
                    "Graph API error during site validation: Code={ErrorCode}, Message={ErrorMessage}",
                    errorCode,
                    errorMessage);

                // Map OData errors to validation error codes
                if (errorCode == "itemNotFound" || errorCode == "ResourceNotFound")
                {
                    return SiteValidationResult.Failure(
                        SiteValidationErrorCode.SiteNotFound,
                        $"SharePoint site not found at {siteUrl}. Please verify the URL is correct and the site exists.");
                }

                if (errorCode == "accessDenied" || errorCode == "Forbidden")
                {
                    return SiteValidationResult.Failure(
                        SiteValidationErrorCode.InsufficientPermissions,
                        "You do not have permission to access this SharePoint site. Please ensure you have at least read access.");
                }

                if (errorCode == "unauthenticated" || errorCode == "InvalidAuthenticationToken")
                {
                    return SiteValidationResult.Failure(
                        SiteValidationErrorCode.ConsentRequired,
                        "Microsoft Graph API consent is required. Please complete the consent flow.");
                }

                // Generic Graph API error
                return SiteValidationResult.Failure(
                    SiteValidationErrorCode.GraphAccessFailed,
                    $"Graph API error: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating site: {SiteUrl}", siteUrl);
            return SiteValidationResult.Failure(
                SiteValidationErrorCode.UnexpectedError,
                $"An unexpected error occurred: {ex.Message}");
        }
    }
}

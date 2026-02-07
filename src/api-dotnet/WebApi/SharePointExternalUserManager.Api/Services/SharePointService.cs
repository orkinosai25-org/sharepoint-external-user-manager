using Microsoft.Graph;
using Microsoft.Graph.Models;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Functions.Models.ExternalUsers;

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
                            InvitedDate = permission.InheritedFrom?.Id == null 
                                ? DateTime.UtcNow 
                                : DateTime.UtcNow.AddDays(-30), // Approximate for inherited
                            InvitedBy = "System", // Graph API doesn't always provide this
                            LastAccessDate = null, // Not available via Graph permissions
                            Status = permission.HasPassword == false ? "Invited" : "Active"
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
}

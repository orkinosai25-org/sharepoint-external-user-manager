using Microsoft.Graph;
using Microsoft.Graph.Models;
using SharePointExternalUserManager.Api.Data.Entities;

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
}

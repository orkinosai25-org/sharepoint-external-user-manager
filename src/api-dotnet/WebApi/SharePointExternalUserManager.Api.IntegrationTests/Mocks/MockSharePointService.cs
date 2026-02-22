using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models.ExternalUsers;
using SharePointExternalUserManager.Functions.Models.Libraries;
using SharePointExternalUserManager.Functions.Models.Lists;

namespace SharePointExternalUserManager.Api.IntegrationTests.Mocks;

/// <summary>
/// Mock implementation of ISharePointService for integration testing
/// Simulates Graph API interactions without requiring real tenant access
/// </summary>
public class MockSharePointService : ISharePointService
{
    private readonly Dictionary<string, List<ExternalUserDto>> _externalUsers = new();
    private readonly Dictionary<string, List<LibraryResponse>> _libraries = new();
    private readonly Dictionary<string, List<ListResponse>> _lists = new();
    private readonly Dictionary<string, string> _siteUrls = new();

    public Task<(bool Success, string? SiteId, string? SiteUrl, string? ErrorMessage)> CreateClientSiteAsync(
        ClientEntity client,
        string userEmail)
    {
        // Simulate site creation
        var siteId = $"mock-site-{Guid.NewGuid()}";
        var siteUrl = $"https://test.sharepoint.com/sites/{client.ClientReference}";

        _siteUrls[siteId] = siteUrl;
        _externalUsers[siteId] = new List<ExternalUserDto>();
        _libraries[siteId] = new List<LibraryResponse>
        {
            new LibraryResponse
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Documents",
                Description = "Default document library",
                WebUrl = $"{siteUrl}/Shared Documents",
                CreatedDateTime = DateTime.UtcNow
            }
        };
        _lists[siteId] = new List<ListResponse>();

        return Task.FromResult<(bool, string?, string?, string?)>((true, siteId, siteUrl, null));
    }

    public Task<List<ExternalUserDto>> GetExternalUsersAsync(string siteId)
    {
        if (!_externalUsers.ContainsKey(siteId))
        {
            _externalUsers[siteId] = new List<ExternalUserDto>();
        }

        return Task.FromResult(_externalUsers[siteId]);
    }

    public Task<(bool Success, ExternalUserDto? User, string? ErrorMessage)> InviteExternalUserAsync(
        string siteId,
        string email,
        string? displayName,
        string permissionLevel,
        string? message,
        string invitedBy)
    {
        if (!_externalUsers.ContainsKey(siteId))
        {
            _externalUsers[siteId] = new List<ExternalUserDto>();
        }

        // Check if user already exists
        var existingUser = _externalUsers[siteId].FirstOrDefault(u => u.Email == email);
        if (existingUser != null)
        {
            return Task.FromResult<(bool, ExternalUserDto?, string?)>(
                (false, null, $"User {email} is already invited to this site"));
        }

        var newUser = new ExternalUserDto
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            DisplayName = displayName ?? email,
            UserPrincipalName = email,
            InvitedBy = invitedBy,
            InvitedDate = DateTime.UtcNow,
            Status = "Active",
            PermissionLevel = permissionLevel,
            LastAccessDate = null
        };

        _externalUsers[siteId].Add(newUser);

        return Task.FromResult<(bool, ExternalUserDto?, string?)>((true, newUser, null));
    }

    public Task<(bool Success, string? ErrorMessage)> RemoveExternalUserAsync(
        string siteId,
        string email)
    {
        if (!_externalUsers.ContainsKey(siteId))
        {
            return Task.FromResult<(bool, string?)>((false, "Site not found"));
        }

        var user = _externalUsers[siteId].FirstOrDefault(u => u.Email == email);
        if (user == null)
        {
            return Task.FromResult<(bool, string?)>((false, $"User {email} not found in site"));
        }

        _externalUsers[siteId].Remove(user);

        return Task.FromResult<(bool, string?)>((true, null));
    }

    public Task<List<LibraryResponse>> GetLibrariesAsync(string siteId)
    {
        if (!_libraries.ContainsKey(siteId))
        {
            _libraries[siteId] = new List<LibraryResponse>();
        }

        return Task.FromResult(_libraries[siteId]);
    }

    public Task<LibraryResponse> CreateLibraryAsync(string siteId, string name, string? description)
    {
        if (!_libraries.ContainsKey(siteId))
        {
            _libraries[siteId] = new List<LibraryResponse>();
        }

        var library = new LibraryResponse
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            WebUrl = $"{_siteUrls.GetValueOrDefault(siteId, "https://test.sharepoint.com")}/{name}",
            CreatedDateTime = DateTime.UtcNow
        };

        _libraries[siteId].Add(library);

        return Task.FromResult(library);
    }

    public Task<List<ListResponse>> GetListsAsync(string siteId)
    {
        if (!_lists.ContainsKey(siteId))
        {
            _lists[siteId] = new List<ListResponse>();
        }

        return Task.FromResult(_lists[siteId]);
    }

    public Task<ListResponse> CreateListAsync(string siteId, string name, string? description, string? template)
    {
        if (!_lists.ContainsKey(siteId))
        {
            _lists[siteId] = new List<ListResponse>();
        }

        var list = new ListResponse
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            Template = template ?? "genericList",
            WebUrl = $"{_siteUrls.GetValueOrDefault(siteId, "https://test.sharepoint.com")}/Lists/{name}",
            CreatedDateTime = DateTime.UtcNow
        };

        _lists[siteId].Add(list);

        return Task.FromResult(list);
    }

    public Task<SiteValidationResult> ValidateSiteAsync(string siteUrl)
    {
        // Simulate site validation
        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            return Task.FromResult(new SiteValidationResult
            {
                IsValid = false,
                ErrorMessage = "Site URL cannot be empty"
            });
        }

        if (!siteUrl.Contains("sharepoint.com", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new SiteValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid SharePoint URL format"
            });
        }

        return Task.FromResult(new SiteValidationResult
        {
            IsValid = true,
            SiteId = $"mock-site-{Guid.NewGuid()}",
            SiteUrl = siteUrl,
            SiteTitle = "Mock Site",
            ExternalSharingEnabled = true,
            SharingCapability = "ExternalUserAndGuestSharing"
        });
    }
}

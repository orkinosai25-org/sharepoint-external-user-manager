using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Implementation of tenant user management service
/// </summary>
public class TenantUserService : ITenantUserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TenantUserService> _logger;

    public TenantUserService(
        ApplicationDbContext context,
        ILogger<TenantUserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TenantUserResponse>> GetTenantUsersAsync(int tenantId)
    {
        return await _context.TenantUsers
            .Where(tu => tu.TenantId == tenantId && tu.IsActive)
            .OrderByDescending(tu => tu.Role)
            .ThenBy(tu => tu.UserPrincipalName)
            .Select(tu => new TenantUserResponse
            {
                Id = tu.Id,
                AzureAdObjectId = tu.AzureAdObjectId,
                UserPrincipalName = tu.UserPrincipalName,
                DisplayName = tu.DisplayName,
                Role = tu.Role,
                IsActive = tu.IsActive,
                CreatedDate = tu.CreatedDate,
                CreatedBy = tu.CreatedBy
            })
            .ToListAsync();
    }

    public async Task<TenantUserResponse?> GetTenantUserAsync(int tenantId, string azureAdObjectId)
    {
        var user = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && 
                                      tu.AzureAdObjectId == azureAdObjectId &&
                                      tu.IsActive);

        if (user == null)
            return null;

        return new TenantUserResponse
        {
            Id = user.Id,
            AzureAdObjectId = user.AzureAdObjectId,
            UserPrincipalName = user.UserPrincipalName,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedDate = user.CreatedDate,
            CreatedBy = user.CreatedBy
        };
    }

    public async Task<TenantUserResponse> AssignRoleAsync(
        int tenantId, 
        AssignRoleRequest request, 
        string createdByUserId)
    {
        // Check if user already exists
        var existingUser = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && 
                                      tu.AzureAdObjectId == request.AzureAdObjectId);

        if (existingUser != null)
        {
            // Reactivate if inactive
            if (!existingUser.IsActive)
            {
                existingUser.IsActive = true;
                existingUser.ModifiedDate = DateTime.UtcNow;
            }

            // Update role and details
            existingUser.Role = request.Role;
            existingUser.UserPrincipalName = request.UserPrincipalName;
            existingUser.DisplayName = request.DisplayName;
            existingUser.ModifiedDate = DateTime.UtcNow;

            _logger.LogInformation(
                "Updated role for user {UserId} in tenant {TenantId} to {Role}",
                request.AzureAdObjectId, tenantId, request.Role);
        }
        else
        {
            // Create new user
            existingUser = new TenantUserEntity
            {
                TenantId = tenantId,
                AzureAdObjectId = request.AzureAdObjectId,
                UserPrincipalName = request.UserPrincipalName,
                DisplayName = request.DisplayName,
                Role = request.Role,
                IsActive = true,
                CreatedBy = createdByUserId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.TenantUsers.Add(existingUser);

            _logger.LogInformation(
                "Assigned role {Role} to user {UserId} in tenant {TenantId}",
                request.Role, request.AzureAdObjectId, tenantId);
        }

        await _context.SaveChangesAsync();

        return new TenantUserResponse
        {
            Id = existingUser.Id,
            AzureAdObjectId = existingUser.AzureAdObjectId,
            UserPrincipalName = existingUser.UserPrincipalName,
            DisplayName = existingUser.DisplayName,
            Role = existingUser.Role,
            IsActive = existingUser.IsActive,
            CreatedDate = existingUser.CreatedDate,
            CreatedBy = existingUser.CreatedBy
        };
    }

    public async Task<TenantUserResponse> UpdateRoleAsync(
        int tenantId, 
        string azureAdObjectId, 
        TenantRole newRole)
    {
        var user = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && 
                                      tu.AzureAdObjectId == azureAdObjectId &&
                                      tu.IsActive);

        if (user == null)
        {
            throw new InvalidOperationException($"User {azureAdObjectId} not found in tenant {tenantId}");
        }

        user.Role = newRole;
        user.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated role for user {UserId} in tenant {TenantId} to {Role}",
            azureAdObjectId, tenantId, newRole);

        return new TenantUserResponse
        {
            Id = user.Id,
            AzureAdObjectId = user.AzureAdObjectId,
            UserPrincipalName = user.UserPrincipalName,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedDate = user.CreatedDate,
            CreatedBy = user.CreatedBy
        };
    }

    public async Task<bool> RemoveUserAsync(int tenantId, string azureAdObjectId)
    {
        var user = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && 
                                      tu.AzureAdObjectId == azureAdObjectId);

        if (user == null)
            return false;

        // Soft delete
        user.IsActive = false;
        user.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Removed user {UserId} from tenant {TenantId}",
            azureAdObjectId, tenantId);

        return true;
    }

    public async Task<bool> HasRoleAsync(int tenantId, string azureAdObjectId, TenantRole minimumRole)
    {
        var user = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && 
                                      tu.AzureAdObjectId == azureAdObjectId &&
                                      tu.IsActive);

        return user != null && user.Role >= minimumRole;
    }

    public async Task<TenantRole?> GetUserRoleAsync(int tenantId, string azureAdObjectId)
    {
        var user = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && 
                                      tu.AzureAdObjectId == azureAdObjectId &&
                                      tu.IsActive);

        return user?.Role;
    }
}

using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service for managing tenant users and their roles
/// </summary>
public interface ITenantUserService
{
    /// <summary>
    /// Get all users and their roles for a tenant
    /// </summary>
    Task<List<TenantUserResponse>> GetTenantUsersAsync(int tenantId);

    /// <summary>
    /// Get a specific user's role in a tenant
    /// </summary>
    Task<TenantUserResponse?> GetTenantUserAsync(int tenantId, string azureAdObjectId);

    /// <summary>
    /// Assign a role to a user in a tenant
    /// </summary>
    Task<TenantUserResponse> AssignRoleAsync(int tenantId, AssignRoleRequest request, string createdByUserId);

    /// <summary>
    /// Update a user's role in a tenant
    /// </summary>
    Task<TenantUserResponse> UpdateRoleAsync(int tenantId, string azureAdObjectId, TenantRole newRole);

    /// <summary>
    /// Remove a user's access from a tenant
    /// </summary>
    Task<bool> RemoveUserAsync(int tenantId, string azureAdObjectId);

    /// <summary>
    /// Check if a user has a specific role or higher in a tenant
    /// </summary>
    Task<bool> HasRoleAsync(int tenantId, string azureAdObjectId, TenantRole minimumRole);

    /// <summary>
    /// Get user's current role in a tenant (returns null if user not found)
    /// </summary>
    Task<TenantRole?> GetUserRoleAsync(int tenantId, string azureAdObjectId);
}

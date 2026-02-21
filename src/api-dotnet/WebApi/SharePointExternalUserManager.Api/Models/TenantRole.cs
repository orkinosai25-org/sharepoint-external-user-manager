namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Defines roles for tenant-level access control
/// </summary>
public enum TenantRole
{
    /// <summary>
    /// Owner of the tenant - full administrative access including billing and user management
    /// </summary>
    TenantOwner = 3,

    /// <summary>
    /// Administrator - can manage clients, external users, and view all data but cannot manage billing or other admins
    /// </summary>
    TenantAdmin = 2,

    /// <summary>
    /// Viewer - read-only access to view clients and external users
    /// </summary>
    Viewer = 1
}

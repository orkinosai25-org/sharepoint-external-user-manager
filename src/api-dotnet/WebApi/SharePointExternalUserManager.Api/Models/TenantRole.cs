namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Defines the roles available for tenant users
/// </summary>
public enum TenantRole
{
    /// <summary>
    /// Tenant owner with full access to all operations
    /// </summary>
    Owner = 0,

    /// <summary>
    /// Tenant administrator with full management capabilities
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Viewer with read-only access to dashboard and data
    /// </summary>
    Viewer = 2
}

namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Defines the available roles for users within a tenant
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Tenant owner - full access to all tenant resources and settings
    /// Can manage users, subscriptions, and all client spaces
    /// </summary>
    TenantOwner = 1,

    /// <summary>
    /// Tenant administrator - can manage client spaces and external users
    /// Cannot modify subscription or tenant settings
    /// </summary>
    TenantAdmin = 2,

    /// <summary>
    /// Viewer - read-only access to dashboard and client spaces
    /// Cannot create, update, or delete resources
    /// </summary>
    Viewer = 3
}

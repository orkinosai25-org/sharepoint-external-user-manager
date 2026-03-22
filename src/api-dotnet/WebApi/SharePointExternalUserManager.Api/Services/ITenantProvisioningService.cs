using SharePointExternalUserManager.Api.Data.Entities;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service that handles just-in-time tenant provisioning.
/// When an authenticated user hits the API for the first time their tenant record, default
/// subscription, and initial TenantOwner role assignment are created automatically so that
/// subsequent calls can proceed without returning TENANT_NOT_FOUND errors.
/// </summary>
public interface ITenantProvisioningService
{
    /// <summary>
    /// Returns the <see cref="TenantEntity"/> for <paramref name="entraIdTenantId"/>, creating
    /// one (together with a default Free/Trial subscription and a TenantOwner role for
    /// <paramref name="userId"/>) if it does not yet exist.
    /// </summary>
    Task<TenantEntity> GetOrCreateTenantAsync(
        string entraIdTenantId,
        string? userId,
        string? userEmail,
        string? userName);
}

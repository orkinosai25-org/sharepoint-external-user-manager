using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Response model for tenant user information
/// </summary>
public class TenantUserResponse
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string EntraIdUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public TenantRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Request model for adding a tenant user
/// </summary>
public class AddTenantUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public TenantRole Role { get; set; } = TenantRole.Viewer;
}

/// <summary>
/// Request model for updating a tenant user role
/// </summary>
public class UpdateTenantUserRoleRequest
{
    public TenantRole Role { get; set; }
}

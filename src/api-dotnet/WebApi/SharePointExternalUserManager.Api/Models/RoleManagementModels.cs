using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Functions.Models;

/// <summary>
/// Request to assign a role to a user within a tenant
/// </summary>
public class AssignRoleRequest
{
    /// <summary>
    /// Azure AD Object ID of the user
    /// </summary>
    public string AzureAdObjectId { get; set; } = string.Empty;

    /// <summary>
    /// User Principal Name (email)
    /// </summary>
    public string UserPrincipalName { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the user (optional)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Role to assign
    /// </summary>
    public TenantRole Role { get; set; }
}

/// <summary>
/// Response containing user role information
/// </summary>
public class TenantUserResponse
{
    public int Id { get; set; }
    public string AzureAdObjectId { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public TenantRole Role { get; set; }
    public string RoleName => Role.ToString();
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Request to update a user's role
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// New role to assign
    /// </summary>
    public TenantRole Role { get; set; }
}

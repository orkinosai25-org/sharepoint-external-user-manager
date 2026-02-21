using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Data.Entities;

/// <summary>
/// Represents a user's role within a specific tenant
/// Maps Azure AD users to tenant-specific roles for RBAC
/// </summary>
[Table("TenantUsers")]
public class TenantUserEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to tenant
    /// </summary>
    [Required]
    public int TenantId { get; set; }

    /// <summary>
    /// Azure AD Object ID (oid claim from JWT)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string AzureAdObjectId { get; set; } = string.Empty;

    /// <summary>
    /// User's email/UPN
    /// </summary>
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string UserPrincipalName { get; set; } = string.Empty;

    /// <summary>
    /// User's display name (optional)
    /// </summary>
    [MaxLength(255)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Role assigned to this user within the tenant
    /// </summary>
    [Required]
    public TenantRole Role { get; set; } = TenantRole.Viewer;

    /// <summary>
    /// Whether this user is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the user was added to this tenant
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who assigned this role (object ID)
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation property
    public virtual TenantEntity? Tenant { get; set; }
}

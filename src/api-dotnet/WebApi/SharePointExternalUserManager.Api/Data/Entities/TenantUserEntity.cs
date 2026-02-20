using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Data.Entities;

/// <summary>
/// Maps users to tenants with assigned roles for multi-tenant RBAC
/// </summary>
[Table("TenantUsers")]
public class TenantUserEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Tenant
    /// </summary>
    [Required]
    public int TenantId { get; set; }

    /// <summary>
    /// Azure AD User Object ID (oid claim from JWT)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address (for display and lookup)
    /// </summary>
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name
    /// </summary>
    [MaxLength(255)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Assigned role within the tenant
    /// </summary>
    [Required]
    public UserRole Role { get; set; }

    /// <summary>
    /// When the user was added to the tenant
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who added this tenant user (for audit trail)
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation property
    public virtual TenantEntity? Tenant { get; set; }
}

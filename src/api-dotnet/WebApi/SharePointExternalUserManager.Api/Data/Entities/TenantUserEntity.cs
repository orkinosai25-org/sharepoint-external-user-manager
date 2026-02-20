using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Data.Entities;

/// <summary>
/// Tenant user entity for role-based access control
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
    /// Azure Entra ID User Object ID (oid claim)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntraIdUserId { get; set; } = string.Empty;

    /// <summary>
    /// User email address
    /// </summary>
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User display name
    /// </summary>
    [MaxLength(255)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Role assigned to the user within this tenant
    /// </summary>
    [Required]
    public TenantRole Role { get; set; } = TenantRole.Viewer;

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual TenantEntity? Tenant { get; set; }
}

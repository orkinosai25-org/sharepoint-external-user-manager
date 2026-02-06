using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharePointExternalUserManager.Api.Data.Entities;

/// <summary>
/// Client (customer/matter) entity representing a client space
/// </summary>
[Table("Clients")]
public class ClientEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Tenant for tenant isolation
    /// </summary>
    [Required]
    public int TenantId { get; set; }

    /// <summary>
    /// Client reference number (e.g., matter number)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ClientReference { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string ClientName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// SharePoint site ID for this client space
    /// </summary>
    [MaxLength(100)]
    public string? SharePointSiteId { get; set; }

    [MaxLength(500)]
    public string? SharePointSiteUrl { get; set; }

    [MaxLength(50)]
    public string ProvisioningStatus { get; set; } = "Pending";

    public DateTime? ProvisionedDate { get; set; }

    [MaxLength(500)]
    public string? ProvisioningError { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(255)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? ModifiedBy { get; set; }

    // Navigation properties
    [ForeignKey("TenantId")]
    public virtual TenantEntity Tenant { get; set; } = null!;
}

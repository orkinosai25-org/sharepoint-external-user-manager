using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharePointExternalUserManager.Api.Data.Entities;

/// <summary>
/// Tenant entity for multi-tenant SaaS platform
/// </summary>
[Table("Tenants")]
public class TenantEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Azure Entra ID Tenant ID (used for tenant isolation)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntraIdTenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OrganizationName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string PrimaryAdminEmail { get; set; } = string.Empty;

    public DateTime OnboardedDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// JSON settings storage
    /// </summary>
    public string? Settings { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ClientEntity> Clients { get; set; } = new List<ClientEntity>();
    public virtual ICollection<SubscriptionEntity> Subscriptions { get; set; } = new List<SubscriptionEntity>();
    public virtual ICollection<AuditLogEntity> AuditLogs { get; set; } = new List<AuditLogEntity>();
}

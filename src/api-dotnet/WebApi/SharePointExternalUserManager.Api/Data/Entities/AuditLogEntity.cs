using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharePointExternalUserManager.Api.Data.Entities;

/// <summary>
/// Audit log entity for tracking all system operations
/// </summary>
[Table("AuditLogs")]
public class AuditLogEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Foreign key to Tenant for tenant isolation
    /// </summary>
    [Required]
    public int TenantId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? UserId { get; set; }

    [MaxLength(255)]
    [EmailAddress]
    public string? UserEmail { get; set; }

    /// <summary>
    /// Action performed: CREATE_CLIENT, INVITE_USER, REMOVE_USER, etc.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Resource type: Client, User, Library, etc.
    /// </summary>
    [MaxLength(50)]
    public string? ResourceType { get; set; }

    [MaxLength(255)]
    public string? ResourceId { get; set; }

    /// <summary>
    /// JSON details of the action
    /// </summary>
    public string? Details { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Status: Success, Failed
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Success";

    // Navigation properties
    [ForeignKey("TenantId")]
    public virtual TenantEntity Tenant { get; set; } = null!;
}

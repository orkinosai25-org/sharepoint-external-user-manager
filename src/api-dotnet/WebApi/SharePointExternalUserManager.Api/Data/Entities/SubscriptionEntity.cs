using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharePointExternalUserManager.Api.Data.Entities;

/// <summary>
/// Subscription entity for billing and plan tracking
/// </summary>
[Table("Subscriptions")]
public class SubscriptionEntity
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
    /// Subscription tier: Free, Starter, Professional, Business, Enterprise
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Tier { get; set; } = "Free";

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime? EndDate { get; set; }

    public DateTime? TrialExpiry { get; set; }

    public DateTime? GracePeriodEnd { get; set; }

    /// <summary>
    /// Status: Trial, Active, Expired, Suspended, Cancelled
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Trial";

    /// <summary>
    /// Stripe subscription ID
    /// </summary>
    [MaxLength(255)]
    public string? StripeSubscriptionId { get; set; }

    /// <summary>
    /// Stripe customer ID
    /// </summary>
    [MaxLength(255)]
    public string? StripeCustomerId { get; set; }

    public int? MaxUsers { get; set; }

    public int? MaxClients { get; set; }

    /// <summary>
    /// JSON features configuration
    /// </summary>
    public string? Features { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("TenantId")]
    public virtual TenantEntity Tenant { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharePointExternalUserManager.Api.Data.Entities;

/// <summary>
/// AI Assistant settings per tenant
/// </summary>
[Table("AiSettings")]
public class AiSettingsEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenant isolation
    /// </summary>
    [Required]
    public int TenantId { get; set; }

    /// <summary>
    /// Enable/disable AI assistant for this tenant
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Enable marketing mode (public, non-authenticated)
    /// </summary>
    public bool MarketingModeEnabled { get; set; } = true;

    /// <summary>
    /// Enable in-product mode (authenticated, context-aware)
    /// </summary>
    public bool InProductModeEnabled { get; set; } = true;

    /// <summary>
    /// Maximum requests per hour per tenant
    /// </summary>
    public int MaxRequestsPerHour { get; set; } = 100;

    /// <summary>
    /// Maximum tokens per request
    /// </summary>
    public int MaxTokensPerRequest { get; set; } = 1000;

    /// <summary>
    /// Monthly token budget (0 = unlimited)
    /// </summary>
    public int MonthlyTokenBudget { get; set; } = 0;

    /// <summary>
    /// Tokens used this month
    /// </summary>
    public int TokensUsedThisMonth { get; set; } = 0;

    /// <summary>
    /// Last reset date for monthly budget
    /// </summary>
    public DateTime LastMonthlyReset { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Show AI disclaimer to users
    /// </summary>
    public bool ShowDisclaimer { get; set; } = true;

    /// <summary>
    /// Custom system prompt override (optional)
    /// </summary>
    public string? CustomSystemPrompt { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual TenantEntity Tenant { get; set; } = null!;
}

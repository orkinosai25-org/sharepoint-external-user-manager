using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharePointExternalUserManager.Api.Data.Entities;

/// <summary>
/// AI conversation logging for audit and analytics
/// </summary>
[Table("AiConversationLogs")]
public class AiConversationLogEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenant isolation (nullable for marketing mode)
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// User ID or session identifier
    /// </summary>
    [MaxLength(255)]
    public string? UserId { get; set; }

    /// <summary>
    /// Conversation ID for grouping messages
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// AI mode: "Marketing" or "InProduct"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Mode { get; set; } = "Marketing";

    /// <summary>
    /// Context information (JSON): ClientSpaceId, LibraryName, etc.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// User message prompt (sanitized)
    /// </summary>
    [Required]
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>
    /// AI assistant response
    /// </summary>
    [Required]
    public string AssistantResponse { get; set; } = string.Empty;

    /// <summary>
    /// Tokens used for this request
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Model used (e.g., "gpt-4", "gpt-3.5-turbo")
    /// </summary>
    [MaxLength(50)]
    public string? Model { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual TenantEntity? Tenant { get; set; }
}

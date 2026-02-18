using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharePointExternalUserManager.Api.Data.Entities;

/// <summary>
/// Tenant OAuth authentication tokens and consent information
/// Stores Microsoft Graph API tokens for each tenant
/// </summary>
[Table("TenantAuth")]
public class TenantAuthEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Tenant table (one-to-one relationship)
    /// </summary>
    [Required]
    public int TenantId { get; set; }

    /// <summary>
    /// OAuth access token for Microsoft Graph API
    /// TODO: Encrypt in production using Azure Key Vault
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// OAuth refresh token for obtaining new access tokens
    /// TODO: Encrypt in production using Azure Key Vault
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Timestamp when the access token expires
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// Granted OAuth scopes (space-separated)
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// UPN or email of the admin who granted consent
    /// </summary>
    [MaxLength(255)]
    public string? ConsentGrantedBy { get; set; }

    /// <summary>
    /// Timestamp when consent was granted
    /// </summary>
    public DateTime? ConsentGrantedAt { get; set; }

    /// <summary>
    /// Timestamp of the last successful token refresh
    /// </summary>
    public DateTime? LastTokenRefresh { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual TenantEntity? Tenant { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SharePointExternalUserManager.Functions.Models.ExternalUsers;

/// <summary>
/// Request model for inviting an external user to a client site
/// </summary>
public class InviteExternalUserRequest
{
    /// <summary>
    /// Email address of the external user to invite
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the external user
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Permission level to grant: Read or Edit
    /// Valid values: "Read", "Edit"
    /// </summary>
    [Required]
    public string PermissionLevel { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom message to include in the invitation email
    /// </summary>
    public string? Message { get; set; }
}

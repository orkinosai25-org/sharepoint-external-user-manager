using System.ComponentModel.DataAnnotations;

namespace SharePointExternalUserManager.Functions.Models.ExternalUsers;

/// <summary>
/// Request model for removing an external user from a client site
/// </summary>
public class RemoveExternalUserRequest
{
    /// <summary>
    /// Email address of the external user to remove
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

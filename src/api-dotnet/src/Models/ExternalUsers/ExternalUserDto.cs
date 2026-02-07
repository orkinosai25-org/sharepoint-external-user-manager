namespace SharePointExternalUserManager.Functions.Models.ExternalUsers;

/// <summary>
/// Response model for external user data
/// </summary>
public class ExternalUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string PermissionLevel { get; set; } = string.Empty;
    public DateTime InvitedDate { get; set; }
    public string InvitedBy { get; set; } = string.Empty;
    public DateTime? LastAccessDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

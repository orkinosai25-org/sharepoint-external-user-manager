namespace SharePointExternalUserManager.Functions.Models;

public class ExternalUser
{
    public Guid UserId { get; set; }
    public string SharePointUserId { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Company { get; set; }
    public string? Project { get; set; }
    public string InvitedBy { get; set; } = string.Empty;
    public DateTime InvitedDate { get; set; }
    public DateTime? AcceptedDate { get; set; }
    public DateTime? LastAccessDate { get; set; }
    public UserStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

public enum UserStatus
{
    Invited,
    Active,
    Suspended,
    Removed
}

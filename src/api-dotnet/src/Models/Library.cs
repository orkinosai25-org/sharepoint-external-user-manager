namespace SharePointExternalUserManager.Functions.Models;

public class Library
{
    public Guid LibraryId { get; set; }
    public string SharePointSiteId { get; set; } = string.Empty;
    public string SharePointLibraryId { get; set; } = string.Empty;
    public string LibraryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SiteUrl { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string? OwnerDisplayName { get; set; }
    public bool ExternalSharingEnabled { get; set; }
    public int ExternalUserCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? LastSyncDate { get; set; }
}

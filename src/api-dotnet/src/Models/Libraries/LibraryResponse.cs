namespace SharePointExternalUserManager.Functions.Models.Libraries;

/// <summary>
/// Response model for document library data
/// </summary>
public class LibraryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastModifiedDateTime { get; set; }
    public int ItemCount { get; set; }
}

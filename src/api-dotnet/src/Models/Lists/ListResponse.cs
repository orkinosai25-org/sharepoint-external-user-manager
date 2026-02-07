namespace SharePointExternalUserManager.Functions.Models.Lists;

/// <summary>
/// Response model for SharePoint list data
/// </summary>
public class ListResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastModifiedDateTime { get; set; }
    public int ItemCount { get; set; }
    public string ListTemplate { get; set; } = string.Empty;
}

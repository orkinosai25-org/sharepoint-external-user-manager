using System.ComponentModel.DataAnnotations;

namespace SharePointExternalUserManager.Functions.Models.Lists;

/// <summary>
/// Request model for creating a new SharePoint list
/// </summary>
public class CreateListRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// List template type (genericList, tasks, contacts, etc.)
    /// Defaults to 'genericList' if not specified
    /// </summary>
    public string? Template { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SharePointExternalUserManager.Functions.Models.Libraries;

/// <summary>
/// Request model for creating a new document library
/// </summary>
public class CreateLibraryRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string? Description { get; set; }
}

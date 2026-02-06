using System.ComponentModel.DataAnnotations;

namespace SharePointExternalUserManager.Functions.Models.Clients;

/// <summary>
/// Request model for creating a new client space
/// </summary>
public class CreateClientRequest
{
    /// <summary>
    /// Client reference number (e.g., matter number)
    /// </summary>
    [Required(ErrorMessage = "Client reference is required")]
    [MaxLength(100)]
    public string ClientReference { get; set; } = string.Empty;

    /// <summary>
    /// Client name (e.g., company or matter name)
    /// </summary>
    [Required(ErrorMessage = "Client name is required")]
    [MaxLength(255)]
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for the client space
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}

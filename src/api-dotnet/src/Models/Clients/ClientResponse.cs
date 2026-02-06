namespace SharePointExternalUserManager.Functions.Models.Clients;

/// <summary>
/// Response model for client space data
/// </summary>
public class ClientResponse
{
    public int Id { get; set; }
    public string ClientReference { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SharePointSiteId { get; set; }
    public string? SharePointSiteUrl { get; set; }
    public string ProvisioningStatus { get; set; } = string.Empty;
    public DateTime? ProvisionedDate { get; set; }
    public string? ProvisioningError { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

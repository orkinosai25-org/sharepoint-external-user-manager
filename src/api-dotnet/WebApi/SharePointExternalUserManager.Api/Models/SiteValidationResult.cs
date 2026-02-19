namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Result of SharePoint site validation
/// </summary>
public class SiteValidationResult
{
    /// <summary>
    /// Whether the site is valid and accessible
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The SharePoint site ID if validation succeeded
    /// </summary>
    public string? SiteId { get; set; }

    /// <summary>
    /// The SharePoint site URL if validation succeeded
    /// </summary>
    public string? SiteUrl { get; set; }

    /// <summary>
    /// Error code if validation failed
    /// </summary>
    public SiteValidationErrorCode? ErrorCode { get; set; }

    /// <summary>
    /// Detailed error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static SiteValidationResult Success(string siteId, string siteUrl) => new()
    {
        IsValid = true,
        SiteId = siteId,
        SiteUrl = siteUrl
    };

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static SiteValidationResult Failure(SiteValidationErrorCode errorCode, string errorMessage) => new()
    {
        IsValid = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Error codes for site validation failures
/// </summary>
public enum SiteValidationErrorCode
{
    /// <summary>
    /// Site URL is invalid or malformed
    /// </summary>
    InvalidUrl,

    /// <summary>
    /// Site does not exist or cannot be found
    /// </summary>
    SiteNotFound,

    /// <summary>
    /// User has insufficient permissions to access the site
    /// </summary>
    InsufficientPermissions,

    /// <summary>
    /// Microsoft Graph API consent is required
    /// </summary>
    ConsentRequired,

    /// <summary>
    /// Graph API access failed
    /// </summary>
    GraphAccessFailed,

    /// <summary>
    /// An unexpected error occurred during validation
    /// </summary>
    UnexpectedError
}

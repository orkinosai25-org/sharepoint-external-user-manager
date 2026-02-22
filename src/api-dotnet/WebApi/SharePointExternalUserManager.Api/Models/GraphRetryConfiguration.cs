namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// Configuration for Graph API retry policies
/// </summary>
public class GraphRetryConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts for transient failures
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Initial delay between retries in seconds
    /// </summary>
    public int InitialRetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Maximum delay between retries in seconds
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Whether to use exponential backoff for retries
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Jitter factor for retry delays (0.0 to 1.0) to prevent thundering herd
    /// </summary>
    public double JitterFactor { get; set; } = 0.2;

    /// <summary>
    /// Timeout in seconds for each Graph API request
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}

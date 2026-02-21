namespace SharePointExternalUserManager.Portal.Models;

/// <summary>
/// Represents a notification message to be displayed to the user
/// </summary>
public class NotificationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int DurationMs { get; set; } = 5000; // Default 5 seconds
    public bool AutoDismiss { get; set; } = true;
}

/// <summary>
/// Type of notification to display
/// </summary>
public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info
}

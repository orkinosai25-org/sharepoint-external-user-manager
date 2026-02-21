using SharePointExternalUserManager.Portal.Models;

namespace SharePointExternalUserManager.Portal.Services;

/// <summary>
/// Centralized service for managing toast notifications across the application
/// </summary>
public class NotificationService
{
    private readonly List<NotificationMessage> _notifications = new();
    private readonly object _lock = new();
    
    /// <summary>
    /// Event raised when the notification list changes
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Get all current notifications
    /// </summary>
    public IReadOnlyList<NotificationMessage> Notifications
    {
        get
        {
            lock (_lock)
            {
                return _notifications.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Show a success notification
    /// </summary>
    public void ShowSuccess(string message, int durationMs = 5000)
    {
        AddNotification(message, NotificationType.Success, durationMs);
    }

    /// <summary>
    /// Show an error notification
    /// </summary>
    public void ShowError(string message, int durationMs = 8000)
    {
        AddNotification(message, NotificationType.Error, durationMs);
    }

    /// <summary>
    /// Show a warning notification
    /// </summary>
    public void ShowWarning(string message, int durationMs = 6000)
    {
        AddNotification(message, NotificationType.Warning, durationMs);
    }

    /// <summary>
    /// Show an info notification
    /// </summary>
    public void ShowInfo(string message, int durationMs = 5000)
    {
        AddNotification(message, NotificationType.Info, durationMs);
    }

    /// <summary>
    /// Remove a specific notification
    /// </summary>
    public void Remove(Guid notificationId)
    {
        lock (_lock)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                _notifications.Remove(notification);
            }
        }
        NotifyStateChanged();
    }

    /// <summary>
    /// Clear all notifications
    /// </summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            _notifications.Clear();
        }
        NotifyStateChanged();
    }

    private void AddNotification(string message, NotificationType type, int durationMs)
    {
        var notification = new NotificationMessage
        {
            Message = message,
            Type = type,
            DurationMs = durationMs,
            AutoDismiss = true
        };

        lock (_lock)
        {
            _notifications.Add(notification);
        }
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

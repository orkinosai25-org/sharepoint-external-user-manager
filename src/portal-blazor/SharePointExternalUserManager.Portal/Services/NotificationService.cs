using SharePointExternalUserManager.Portal.Models;
using System.Collections.Concurrent;

namespace SharePointExternalUserManager.Portal.Services;

/// <summary>
/// Centralized service for managing toast notifications across the application
/// </summary>
public class NotificationService
{
    private readonly ConcurrentBag<NotificationMessage> _notifications = new();
    
    /// <summary>
    /// Event raised when the notification list changes
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Get all current notifications
    /// </summary>
    public IReadOnlyList<NotificationMessage> Notifications => _notifications.ToList().AsReadOnly();

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
        var updatedList = _notifications.Where(n => n.Id != notificationId).ToList();
        
        // Clear and repopulate (ConcurrentBag doesn't support direct removal)
        while (_notifications.TryTake(out _)) { }
        
        foreach (var notification in updatedList)
        {
            _notifications.Add(notification);
        }
        
        NotifyStateChanged();
    }

    /// <summary>
    /// Clear all notifications
    /// </summary>
    public void ClearAll()
    {
        while (_notifications.TryTake(out _)) { }
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

        _notifications.Add(notification);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

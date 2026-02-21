# Global Notification System - Quick Reference

## Quick Start

### 1. Inject the Service

```csharp
@inject Services.NotificationService NotificationService
```

### 2. Show Notifications

```csharp
// Success
NotificationService.ShowSuccess("Operation completed successfully!");

// Error
NotificationService.ShowError("Operation failed. Please try again.");

// Warning
NotificationService.ShowWarning("This action cannot be undone.");

// Info
NotificationService.ShowInfo("New features are available!");
```

## Common Patterns

### Success/Error Pattern

```csharp
private async Task SaveChanges()
{
    try
    {
        await ApiClient.SaveAsync(data);
        NotificationService.ShowSuccess("Changes saved successfully!");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Save failed");
        NotificationService.ShowError("Failed to save changes. Please try again.");
    }
}
```

### Validation Pattern

```csharp
private void ValidateForm()
{
    if (string.IsNullOrWhiteSpace(name))
    {
        NotificationService.ShowError("Name is required");
        return;
    }
    
    if (email.Contains("@") == false)
    {
        NotificationService.ShowWarning("Please enter a valid email address");
        return;
    }
    
    // Proceed with form submission
}
```

### Long Operation Pattern

```csharp
private async Task ProcessLongOperation()
{
    NotificationService.ShowInfo("Processing started...");
    
    try
    {
        await Task.Delay(5000); // Long operation
        NotificationService.ShowSuccess("Processing completed!");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Processing failed");
        NotificationService.ShowError("Processing failed. Please try again.");
    }
}
```

## Customization

### Custom Duration

```csharp
// Display for 10 seconds
NotificationService.ShowSuccess("Important message", durationMs: 10000);

// Display for 3 seconds
NotificationService.ShowInfo("Quick tip", durationMs: 3000);
```

### Clear All Notifications

```csharp
// Clear all active notifications
NotificationService.ClearAll();
```

## UI Examples

### Create Operation
```csharp
var result = await ApiClient.CreateAsync(request);
NotificationService.ShowSuccess($"'{result.Name}' created successfully!");
```

### Update Operation
```csharp
await ApiClient.UpdateAsync(id, request);
NotificationService.ShowSuccess("Settings updated successfully!");
```

### Delete Operation
```csharp
await ApiClient.DeleteAsync(id);
NotificationService.ShowSuccess("Item deleted successfully!");
```

### Invite User
```csharp
await ApiClient.InviteUserAsync(email);
NotificationService.ShowSuccess($"Invitation sent to {email}");
```

### Upload File
```csharp
await ApiClient.UploadAsync(file);
NotificationService.ShowSuccess($"'{file.Name}' uploaded successfully!");
```

## Best Practices

### ✅ DO
- Keep messages concise (1-2 sentences)
- Use friendly, actionable language
- Include context when helpful
- Log detailed errors separately
- Use appropriate notification type

### ❌ DON'T
- Show stack traces in notifications
- Use technical jargon
- Display multiple notifications for one action
- Use notifications for form validation (use inline instead)
- Show sensitive information

## Message Examples

### Good Messages ✅
```csharp
"Client space created successfully!"
"Invitation sent to user@example.com"
"Failed to save changes. Please try again."
"Processing... This may take a few moments."
```

### Bad Messages ❌
```csharp
"Success!" // Too vague
"Error: NullReferenceException at line 42" // Too technical
"The operation you attempted has been completed successfully and the data has been persisted to the database" // Too long
"Failed" // No context or action
```

## Default Timeouts

- **Success**: 5 seconds
- **Info**: 5 seconds
- **Warning**: 6 seconds
- **Error**: 8 seconds (users need more time to read)

## Integration Checklist

When adding notifications to a page:

- [ ] Inject `NotificationService`
- [ ] Replace error variables with `ShowError()`
- [ ] Add success notification for operations
- [ ] Remove inline alert divs
- [ ] Test all notification scenarios
- [ ] Verify messages are user-friendly

## Troubleshooting

### Notifications don't appear
1. Check `ToastContainer` is in `MainLayout.razor`
2. Verify service is injected: `@inject Services.NotificationService NotificationService`
3. Check browser console for errors

### Notifications don't auto-dismiss
1. Check custom duration isn't set too high
2. Look for JavaScript errors in console

### Multiple notifications overlap
1. Clear browser cache
2. Verify CSS is loaded properly
3. Check z-index in browser DevTools

## Browser Support

✅ Chrome/Edge (Chromium)  
✅ Firefox  
✅ Safari  
✅ Mobile browsers (iOS/Android)

## More Information

See `ISSUE_13_NOTIFICATION_SYSTEM_IMPLEMENTATION.md` for complete documentation.

---

**Quick Start Time**: < 5 minutes  
**Ready to Use**: Yes ✅  
**Production Ready**: Yes ✅

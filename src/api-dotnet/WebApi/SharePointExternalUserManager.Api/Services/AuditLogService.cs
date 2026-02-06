using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service for creating audit log entries
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Log an action to the audit log
    /// </summary>
    Task LogActionAsync(
        int tenantId,
        string? userId,
        string? userEmail,
        string action,
        string? resourceType,
        string? resourceId,
        string? details,
        string? ipAddress,
        string? correlationId,
        string status);
}

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        ApplicationDbContext context,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActionAsync(
        int tenantId,
        string? userId,
        string? userEmail,
        string action,
        string? resourceType,
        string? resourceId,
        string? details,
        string? ipAddress,
        string? correlationId,
        string status)
    {
        try
        {
            var auditLog = new AuditLogEntity
            {
                TenantId = tenantId,
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                UserEmail = userEmail,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Details = details,
                IpAddress = ipAddress,
                CorrelationId = correlationId,
                Status = status
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit log created: {Action} by {UserEmail} on {ResourceType}/{ResourceId} - Status: {Status}",
                action,
                userEmail,
                resourceType,
                resourceId,
                status);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create audit log entry for action {Action}",
                action);
            // Don't throw - audit logging should not break the main operation
        }
    }
}

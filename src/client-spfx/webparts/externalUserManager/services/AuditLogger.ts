import { WebPartContext } from '@microsoft/sp-webpart-base';

/**
 * Audit Logger for SharePoint External User Manager
 * 
 * Provides comprehensive logging for compliance and troubleshooting:
 * - User actions (create, delete, modify libraries)
 * - System events (errors, warnings, info)
 * - Performance metrics
 * - Security events
 */
export interface IAuditLogEntry {
  timestamp: Date;
  userId: string;
  userDisplayName: string;
  action: string;
  details: string;
  level: 'info' | 'warning' | 'error';
  metadata?: any;
  sessionId?: string;
}

export class AuditLogger {
  private context: WebPartContext;
  private sessionId: string;

  constructor(context: WebPartContext) {
    this.context = context;
    this.sessionId = this.generateSessionId();
  }

  /**
   * Log informational events
   */
  public logInfo(action: string, details: string, metadata?: any): void {
    this.log('info', action, details, metadata);
  }

  /**
   * Log warning events
   */
  public logWarning(action: string, details: string, metadata?: any): void {
    this.log('warning', action, details, metadata);
  }

  /**
   * Log error events
   */
  public logError(action: string, details: string, error?: any, metadata?: any): void {
    const errorDetails = error ? `${details} - Error: ${error.message || error}` : details;
    const errorMetadata = {
      ...metadata,
      error: error ? {
        message: error.message,
        stack: error.stack,
        name: error.name
      } : undefined
    };
    this.log('error', action, errorDetails, errorMetadata);
  }

  /**
   * Core logging method
   */
  private log(level: 'info' | 'warning' | 'error', action: string, details: string, metadata?: any): void {
    const logEntry: IAuditLogEntry = {
      timestamp: new Date(),
      userId: this.context.pageContext.user.loginName,
      userDisplayName: this.context.pageContext.user.displayName,
      action,
      details,
      level,
      metadata,
      sessionId: this.sessionId
    };

    // Log to browser console for development
    this.logToConsole(logEntry);

    // In production, you would also log to:
    // - SharePoint list for audit trail
    // - Application Insights
    // - Azure Log Analytics
    // - Custom logging service
    this.logToSharePointList(logEntry);
  }

  /**
   * Log to browser console for development/debugging
   */
  private logToConsole(entry: IAuditLogEntry): void {
    const message = `[${entry.level.toUpperCase()}] ${entry.action}: ${entry.details}`;
    
    switch (entry.level) {
      case 'error':
        console.error(message, entry);
        break;
      case 'warning':
        console.warn(message, entry);
        break;
      default:
        console.log(message, entry);
        break;
    }
  }

  /**
   * Log to SharePoint list for persistent audit trail
   * Note: This requires a pre-configured audit log list
   */
  private async logToSharePointList(entry: IAuditLogEntry): Promise<void> {
    try {
      // In a real implementation, you would:
      // 1. Check if audit log list exists, create if needed
      // 2. Add item to the list with proper error handling
      // 3. Handle batch operations for performance
      
      // For now, we'll simulate this
      if (this.context.pageContext.web.absoluteUrl) {
        // This would be the actual SharePoint list logging implementation
        // await sp.web.lists.getByTitle("AuditLog").items.add({
        //   Title: entry.action,
        //   UserId: entry.userId,
        //   UserDisplayName: entry.userDisplayName,
        //   Details: entry.details,
        //   Level: entry.level,
        //   Metadata: JSON.stringify(entry.metadata),
        //   SessionId: entry.sessionId,
        //   Timestamp: entry.timestamp.toISOString()
        // });
      }
    } catch (error) {
      // Don't let audit logging failures break the main functionality
      console.error('Failed to log to SharePoint audit list:', error);
    }
  }

  /**
   * Generate a unique session ID for tracking related operations
   */
  public generateSessionId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Get all audit logs for a specific action or time period
   * This would be used by admin reporting features
   */
  public async getAuditLogs(filter?: {
    action?: string;
    userId?: string;
    level?: 'info' | 'warning' | 'error';
    fromDate?: Date;
    toDate?: Date;
  }): Promise<IAuditLogEntry[]> {
    try {
      // In a real implementation, this would query the SharePoint audit log list
      // with appropriate filters and return the results
      
      // For now, return empty array as this is primarily for future functionality
      return [];
    } catch (error) {
      console.error('Failed to retrieve audit logs:', error);
      return [];
    }
  }

  /**
   * Export audit logs to CSV for compliance reporting
   */
  public async exportAuditLogs(filter?: {
    fromDate?: Date;
    toDate?: Date;
  }): Promise<string> {
    try {
      const logs = await this.getAuditLogs(filter);
      
      // Convert to CSV format
      const headers = ['Timestamp', 'User', 'Action', 'Details', 'Level', 'SessionId'];
      const csvRows = [headers.join(',')];
      
      logs.forEach(log => {
        const row = [
          log.timestamp.toISOString(),
          `"${log.userDisplayName}"`,
          `"${log.action}"`,
          `"${log.details.replace(/"/g, '""')}"`,
          log.level,
          log.sessionId || ''
        ];
        csvRows.push(row.join(','));
      });
      
      return csvRows.join('\n');
    } catch (error) {
      console.error('Failed to export audit logs:', error);
      return '';
    }
  }
}
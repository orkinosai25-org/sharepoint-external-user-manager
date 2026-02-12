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
export declare class AuditLogger {
    private context;
    private sessionId;
    constructor(context: WebPartContext);
    /**
     * Log informational events
     */
    logInfo(action: string, details: string, metadata?: any): void;
    /**
     * Log warning events
     */
    logWarning(action: string, details: string, metadata?: any): void;
    /**
     * Log error events
     */
    logError(action: string, details: string, error?: any, metadata?: any): void;
    /**
     * Core logging method
     */
    private log;
    /**
     * Log to browser console for development/debugging
     */
    private logToConsole;
    /**
     * Log to SharePoint list for persistent audit trail
     * Note: This requires a pre-configured audit log list
     */
    private logToSharePointList;
    /**
     * Generate a unique session ID for tracking related operations
     */
    generateSessionId(): string;
    /**
     * Get all audit logs for a specific action or time period
     * This would be used by admin reporting features
     */
    getAuditLogs(filter?: {
        action?: string;
        userId?: string;
        level?: 'info' | 'warning' | 'error';
        fromDate?: Date;
        toDate?: Date;
    }): Promise<IAuditLogEntry[]>;
    /**
     * Export audit logs to CSV for compliance reporting
     */
    exportAuditLogs(filter?: {
        fromDate?: Date;
        toDate?: Date;
    }): Promise<string>;
}
//# sourceMappingURL=AuditLogger.d.ts.map
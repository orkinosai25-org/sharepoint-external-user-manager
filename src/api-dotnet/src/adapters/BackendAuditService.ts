/**
 * Audit Logger Adapter for Backend API
 * 
 * This adapter implements the IAuditService interface from the shared services layer
 * and bridges it to the backend's audit logging system
 * 
 * TODO: This is a temporary workaround using console logging. The original auditLogger
 * requires TenantContext as the first parameter, which is not compatible with the
 * IAuditService interface from the shared services layer. This should be refactored
 * to properly integrate with the auditLogger once the signature incompatibility is resolved.
 */

import { IAuditService } from '../../../services/interfaces';

export class BackendAuditService implements IAuditService {
  /**
   * Log an informational message
   */
  logInfo(operation: string, message: string, data?: any): void {
    // TODO: Replace with proper audit logging once signature compatibility is resolved
    console.log(`[INFO] ${operation}: ${message}`, data);
  }

  /**
   * Log a warning message
   */
  logWarning(operation: string, message: string, data?: any): void {
    // TODO: Replace with proper audit logging once signature compatibility is resolved
    console.warn(`[WARN] ${operation}: ${message}`, data);
  }

  /**
   * Log an error
   */
  logError(operation: string, message: string, error: any): void {
    // TODO: Replace with proper audit logging once signature compatibility is resolved
    console.error(`[ERROR] ${operation}: ${message}`, error);
  }

  /**
   * Generate a unique session ID for tracking related operations
   */
  generateSessionId(): string {
    return `session-${Date.now()}-${Math.random().toString(36).substring(7)}`;
  }
}

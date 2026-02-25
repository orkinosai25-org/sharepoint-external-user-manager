/**
 * Audit Logger Adapter for Backend API
 * 
 * This adapter implements the IAuditService interface from the shared services layer
 * and bridges it to the backend's audit logging system
 */

import { IAuditService } from '../../../services/interfaces';

export class BackendAuditService implements IAuditService {
  /**
   * Log an informational message
   */
  logInfo(operation: string, message: string, data?: any): void {
    // Simple console logging for info messages
    console.log(`[INFO] ${operation}: ${message}`, data);
  }

  /**
   * Log a warning message
   */
  logWarning(operation: string, message: string, data?: any): void {
    // Simple console logging for warning messages
    console.warn(`[WARN] ${operation}: ${message}`, data);
  }

  /**
   * Log an error
   */
  logError(operation: string, message: string, error: any): void {
    // Simple console logging for error messages
    console.error(`[ERROR] ${operation}: ${message}`, error);
  }

  /**
   * Generate a unique session ID for tracking related operations
   */
  generateSessionId(): string {
    return `session-${Date.now()}-${Math.random().toString(36).substring(7)}`;
  }
}

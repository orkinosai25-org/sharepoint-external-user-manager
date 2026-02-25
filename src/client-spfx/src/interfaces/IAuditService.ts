/**
 * Audit Logging Service Interface
 * 
 * Handles audit logging for compliance and monitoring
 */
export interface IAuditService {
  /**
   * Log an informational message
   * @param operation - Operation name
   * @param message - Log message
   * @param data - Additional data
   */
  logInfo(operation: string, message: string, data?: any): void;
  
  /**
   * Log a warning message
   * @param operation - Operation name
   * @param message - Log message
   * @param data - Additional data
   */
  logWarning(operation: string, message: string, data?: any): void;
  
  /**
   * Log an error
   * @param operation - Operation name
   * @param message - Error message
   * @param error - Error object or data
   */
  logError(operation: string, message: string, error: any): void;
  
  /**
   * Generate a unique session ID for tracking related operations
   * @returns Session ID
   */
  generateSessionId(): string;
}

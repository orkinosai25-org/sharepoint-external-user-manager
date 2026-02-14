/**
 * SPFx Audit Service Adapter
 * 
 * This adapter implements the IAuditService interface from the shared services layer
 * and bridges it to SPFx's console/logging
 */

import { WebPartContext } from '@microsoft/sp-webpart-base';
import { IAuditService } from '../../../../services/interfaces';

export class SPFxAuditService implements IAuditService {
  constructor(private context: WebPartContext) {}

  /**
   * Log an informational message
   */
  logInfo(operation: string, message: string, data?: any): void {
    console.log(`[INFO] ${operation}: ${message}`, data || '');
    
    // Could also send to Application Insights or other logging service
    // if (this.context.applicationInsights) {
    //   this.context.applicationInsights.trackEvent({
    //     name: operation,
    //     properties: { message, ...data }
    //   });
    // }
  }

  /**
   * Log a warning message
   */
  logWarning(operation: string, message: string, data?: any): void {
    console.warn(`[WARN] ${operation}: ${message}`, data || '');
  }

  /**
   * Log an error
   */
  logError(operation: string, message: string, error: any): void {
    console.error(`[ERROR] ${operation}: ${message}`, error);
    
    // Could also send to Application Insights
    // if (this.context.applicationInsights) {
    //   this.context.applicationInsights.trackException({
    //     exception: error,
    //     properties: { operation, message }
    //   });
    // }
  }

  /**
   * Generate a unique session ID for tracking related operations
   */
  generateSessionId(): string {
    return `spfx-${this.context.pageContext.correlationId || Date.now()}-${Math.random().toString(36).substring(7)}`;
  }
}

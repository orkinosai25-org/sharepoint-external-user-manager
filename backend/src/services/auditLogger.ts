/**
 * Audit logging service
 */

import { databaseService } from './database';
import { CreateAuditLogEntry, AuditAction, ResourceType, AuditStatus } from '../models/audit';
import { TenantContext } from '../models/common';
import { config } from '../utils/config';

class AuditLoggerService {
  async log(
    context: TenantContext,
    action: AuditAction,
    resourceType: ResourceType,
    resourceId: string,
    details?: any,
    ipAddress?: string,
    correlationId?: string,
    status: AuditStatus = 'Success'
  ): Promise<void> {
    if (!config.features.enableAuditLogging) {
      console.log('Audit logging disabled, skipping log entry');
      return;
    }

    try {
      const entry: CreateAuditLogEntry = {
        tenantId: context.tenantId,
        userId: context.userId,
        userEmail: context.userEmail,
        action,
        resourceType,
        resourceId,
        details: details || {},
        ipAddress: ipAddress || 'unknown',
        correlationId: correlationId || 'unknown',
        status
      };

      await databaseService.createAuditLog(entry);
    } catch (error) {
      console.error('Failed to write audit log:', error);
      // Don't throw - audit failures shouldn't break the main operation
    }
  }

  async logSuccess(
    context: TenantContext,
    action: AuditAction,
    resourceType: ResourceType,
    resourceId: string,
    details?: any,
    ipAddress?: string,
    correlationId?: string
  ): Promise<void> {
    await this.log(context, action, resourceType, resourceId, details, ipAddress, correlationId, 'Success');
  }

  async logFailure(
    context: TenantContext,
    action: AuditAction,
    resourceType: ResourceType,
    resourceId: string,
    details?: any,
    ipAddress?: string,
    correlationId?: string
  ): Promise<void> {
    await this.log(context, action, resourceType, resourceId, details, ipAddress, correlationId, 'Failed');
  }
}

export const auditLogger = new AuditLoggerService();

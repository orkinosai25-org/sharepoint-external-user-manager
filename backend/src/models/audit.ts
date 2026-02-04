/**
 * Audit log model and interfaces
 */

export type AuditAction =
  | 'TenantOnboarded'
  | 'SubscriptionUpdated'
  | 'UserInvited'
  | 'UserRemoved'
  | 'PermissionChanged'
  | 'PolicyUpdated'
  | 'PolicyCreated'
  | 'AuditExported'
  | 'LoginSuccess'
  | 'LoginFailed'
  | 'Unauthorized'
  | 'ClientCreated'
  | 'SiteProvisioned'
  | 'SiteProvisioningFailed';

export type ResourceType = 'Tenant' | 'Subscription' | 'ExternalUser' | 'Policy' | 'Library' | 'Client';

export type AuditStatus = 'Success' | 'Failed';

export interface AuditLog {
  id: number;
  tenantId: number;
  timestamp: Date;
  userId: string;
  userEmail: string;
  action: AuditAction;
  resourceType: ResourceType;
  resourceId: string;
  details: AuditDetails;
  ipAddress: string;
  correlationId: string;
  status: AuditStatus;
}

export interface AuditDetails {
  [key: string]: any;
}

export interface AuditLogRequest {
  action?: AuditAction;
  userId?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
}

export interface AuditLogResponse {
  id: number;
  timestamp: string;
  userId: string;
  userEmail: string;
  action: AuditAction;
  resourceType: ResourceType;
  resourceId: string;
  details: AuditDetails;
  ipAddress: string;
  status: AuditStatus;
}

export interface CreateAuditLogEntry {
  tenantId: number;
  userId: string;
  userEmail: string;
  action: AuditAction;
  resourceType: ResourceType;
  resourceId: string;
  details?: AuditDetails;
  ipAddress: string;
  correlationId: string;
  status: AuditStatus;
}

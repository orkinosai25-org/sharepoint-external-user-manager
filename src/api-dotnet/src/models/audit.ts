/**
 * Audit log model and interfaces
 * 
 * Provides immutable audit trail for all system operations with tenant isolation.
 * All audit entries are permanently retained for compliance and security purposes.
 */

export type AuditAction =
  | 'TenantOnboarded'
  | 'TenantConsentGranted'
  | 'SubscriptionUpdated'
  | 'SubscriptionActivated'
  | 'PaymentFailed'
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

export type ResourceType = 'Tenant' | 'Subscription' | 'ExternalUser' | 'Policy' | 'Library' | 'Client' | 'Invoice';

export type AuditStatus = 'Success' | 'Failed';

/**
 * Core audit log model
 * Represents a single auditable event in the system with full context
 */
export interface AuditLog {
  /** Unique audit log identifier */
  id: number;
  /** Tenant ID for multi-tenant isolation (required for all events) */
  tenantId: number;
  /** Timestamp when the event occurred */
  timestamp: Date;
  /** User identifier who performed the action */
  userId: string;
  /** User email who performed the action */
  userEmail: string;
  /** Type of action performed */
  action: AuditAction;
  /** Type of resource affected */
  resourceType: ResourceType;
  /** Identifier of the resource affected */
  resourceId: string;
  /** Additional details about the event (JSON object) */
  details: AuditDetails;
  /** IP address of the request origin */
  ipAddress: string;
  /** Correlation ID for request tracing */
  correlationId: string;
  /** Success or failure status of the action */
  status: AuditStatus;
}

/**
 * Flexible details structure for audit events
 * Allows storing event-specific information
 */
export interface AuditDetails {
  [key: string]: any;
}

/**
 * Request parameters for querying audit logs
 */
export interface AuditLogRequest {
  /** Filter by action type */
  action?: AuditAction;
  /** Filter by user ID */
  userId?: string;
  /** Filter by start date (ISO 8601 string) */
  startDate?: string;
  /** Filter by end date (ISO 8601 string) */
  endDate?: string;
  /** Page number (1-based) */
  page?: number;
  /** Number of results per page */
  pageSize?: number;
}

/**
 * Response format for audit log entries
 */
export interface AuditLogResponse {
  /** Audit log identifier */
  id: number;
  /** Event timestamp (ISO 8601 string) */
  timestamp: string;
  /** User identifier */
  userId: string;
  /** User email */
  userEmail: string;
  /** Action performed */
  action: AuditAction;
  /** Resource type */
  resourceType: ResourceType;
  /** Resource identifier */
  resourceId: string;
  /** Event details */
  details: AuditDetails;
  /** IP address */
  ipAddress: string;
  /** Status */
  status: AuditStatus;
}

/**
 * Request payload for creating an audit log entry
 * Used internally by the system to log events
 */
export interface CreateAuditLogEntry {
  /** Tenant ID (required for tenant isolation) */
  tenantId: number;
  /** User identifier */
  userId: string;
  /** User email */
  userEmail: string;
  /** Action type */
  action: AuditAction;
  /** Resource type */
  resourceType: ResourceType;
  /** Resource identifier */
  resourceId: string;
  /** Optional event details */
  details?: AuditDetails;
  /** Request IP address */
  ipAddress: string;
  /** Request correlation ID */
  correlationId: string;
  /** Event status */
  status: AuditStatus;
}

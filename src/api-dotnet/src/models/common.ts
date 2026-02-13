/**
 * Common types and interfaces used across the application
 */

export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  error?: ErrorResponse;
  meta?: ResponseMeta;
  pagination?: PaginationInfo;
}

export interface ErrorResponse {
  code: string;
  message: string;
  details?: string;
  correlationId: string;
}

export interface ResponseMeta {
  correlationId: string;
  timestamp: string;
}

export interface PaginationInfo {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
  hasNext: boolean;
  hasPrev: boolean;
}

export interface TenantContext {
  tenantId: number;
  entraIdTenantId: string;
  userId: string;
  userEmail: string;
  roles: UserRole[];
  subscriptionTier: 'Free' | 'Pro' | 'Enterprise';
}

export type UserRole = 'Owner' | 'Admin' | 'User' | 'ReadOnly' | 'FirmAdmin' | 'FirmUser';

export type SubscriptionStatus = 'Trial' | 'Active' | 'Expired' | 'Cancelled' | 'GracePeriod';

export type TenantStatus = 'Active' | 'Suspended' | 'Cancelled';

export interface TokenClaims {
  tid: string; // Tenant ID
  oid: string; // User object ID
  email?: string;
  upn?: string; // User principal name
  roles?: string[];
  iss: string; // Issuer
  aud: string; // Audience
  exp: number; // Expiration
  iat: number; // Issued at
}

export class AppError extends Error {
  constructor(
    public statusCode: number,
    public code: string,
    message: string,
    public details?: string
  ) {
    super(message);
    this.name = 'AppError';
  }
}

export class ValidationError extends AppError {
  constructor(message: string, details?: string) {
    super(400, 'VALIDATION_ERROR', message, details);
    this.name = 'ValidationError';
  }
}

export class UnauthorizedError extends AppError {
  constructor(message: string = 'Unauthorized', details?: string) {
    super(401, 'UNAUTHORIZED', message, details);
    this.name = 'UnauthorizedError';
  }
}

export class ForbiddenError extends AppError {
  constructor(message: string = 'Forbidden', details?: string) {
    super(403, 'FORBIDDEN', message, details);
    this.name = 'ForbiddenError';
  }
}

export class NotFoundError extends AppError {
  constructor(resource: string, details?: string) {
    super(404, `${resource.toUpperCase()}_NOT_FOUND`, `${resource} not found`, details);
    this.name = 'NotFoundError';
  }
}

export class ConflictError extends AppError {
  constructor(message: string, details?: string) {
    super(409, 'CONFLICT', message, details);
    this.name = 'ConflictError';
  }
}

export class SubscriptionError extends AppError {
  constructor(message: string, details?: string) {
    super(402, 'SUBSCRIPTION_ERROR', message, details);
    this.name = 'SubscriptionError';
  }
}

export class FeatureNotAvailableError extends AppError {
  constructor(feature: string, requiredTier: string) {
    super(
      402,
      'FEATURE_NOT_AVAILABLE',
      `Feature "${feature}" requires ${requiredTier} tier`,
      `Current subscription tier does not support this feature`
    );
    this.name = 'FeatureNotAvailableError';
  }
}

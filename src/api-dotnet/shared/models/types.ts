// Shared TypeScript Interfaces and Types

export interface Tenant {
  id: string;
  tenantId: string;
  displayName: string;
  status: 'active' | 'trial_expired' | 'suspended' | 'cancelled';
  subscriptionTier: 'free' | 'trial' | 'pro' | 'enterprise';
  subscriptionStatus: 'active' | 'suspended' | 'cancelled';
  trialEndDate?: string;
  billingEmail: string;
  adminEmail: string;
  createdDate: string;
  lastModifiedDate: string;
  settings: TenantSettings;
  azureAdAppId?: string;
  onboardingCompleted: boolean;
  dataLocation: string;
}

export interface TenantSettings {
  apiBaseUrl: string;
  webhookUrl?: string;
  features: {
    auditExport: boolean;
    bulkOperations: boolean;
    advancedReporting: boolean;
    customPolicies: boolean;
  };
}

export interface Subscription {
  id: string;
  tenantId: string;
  tier: 'free' | 'trial' | 'pro' | 'enterprise';
  status: 'active' | 'suspended' | 'cancelled';
  startDate: string;
  endDate?: string;
  autoRenew: boolean;
  billingCycle: 'monthly' | 'annual';
  pricing: {
    amount: number;
    currency: string;
    perSeat: boolean;
  };
  limits: SubscriptionLimits;
  usage: SubscriptionUsage;
  paymentMethod?: string;
  marketplaceIntegration?: {
    enabled: boolean;
    marketplaceSubscriptionId?: string;
    offerName?: string;
  };
  createdDate: string;
  lastModifiedDate: string;
}

export interface SubscriptionLimits {
  maxExternalUsers: number;
  maxLibraries: number;
  apiCallsPerMonth: number;
  auditRetentionDays: number;
  maxAdmins: number;
}

export interface SubscriptionUsage {
  externalUsersCount: number;
  librariesCount: number;
  apiCallsThisMonth: number;
  storageUsedMB: number;
}

export interface ExternalUser {
  userId: string;
  email: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  company?: string;
  project?: string;
  department?: string;
  invitedBy: string;
  invitedDate: string;
  lastAccessDate?: string;
  status: 'invited' | 'active' | 'expired' | 'revoked';
  expirationDate?: string;
  azureAdGuestId?: string;
  libraries: UserLibraryAccess[];
}

export interface UserLibraryAccess {
  libraryId: string;
  libraryName: string;
  siteUrl?: string;
  permissions: 'read' | 'contribute' | 'fullcontrol';
  grantedDate: string;
  grantedBy: string;
}

export interface Policy {
  expirationPolicy: {
    enabled: boolean;
    defaultExpirationDays: number;
    sendReminderDays: number;
    autoRevoke: boolean;
  };
  approvalPolicy: {
    enabled: boolean;
    requireApprovalForInvites: boolean;
    approvers: string[];
    autoApproveInternalRequests: boolean;
  };
  restrictionPolicy: {
    enabled: boolean;
    allowedDomains: string[];
    blockedDomains: string[];
    requireCompanyField: boolean;
    requireProjectField: boolean;
  };
  notificationPolicy: {
    enabled: boolean;
    notifyOnInvite: boolean;
    notifyOnRemoval: boolean;
    notifyOnExpiration: boolean;
    notificationEmail?: string;
  };
  lastModifiedDate: string;
  lastModifiedBy: string;
}

export interface AuditLog {
  auditId: number | string;
  timestamp: string;
  correlationId: string;
  tenantId: string;
  eventType: string;
  actor: {
    userId?: string;
    email: string;
    displayName?: string;
    ipAddress?: string;
  };
  action: string;
  status: 'success' | 'failure';
  target: {
    resourceType: string;
    resourceId: string;
    email?: string;
  };
  metadata?: Record<string, unknown>;
  changes?: {
    before: unknown;
    after: unknown;
  };
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: {
    code: string;
    message: string;
    details?: string;
    correlationId?: string;
    timestamp?: string;
  };
  pagination?: {
    page: number;
    pageSize: number;
    total: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
  };
}

export interface TokenPayload {
  iss: string;
  aud: string;
  exp: number;
  nbf: number;
  sub: string;
  tid: string; // Tenant ID
  upn?: string; // User Principal Name
  email?: string;
  name?: string;
  oid?: string; // Object ID
  roles?: string[];
}

export interface TenantContext {
  tenantId: string;
  userId: string;
  userEmail: string;
  userName?: string;
  roles: string[];
  subscription: Subscription;
}

export enum UserRole {
  TenantOwner = 'TenantOwner',
  TenantAdmin = 'TenantAdmin',
  LibraryOwner = 'LibraryOwner',
  LibraryContributor = 'LibraryContributor',
  LibraryReader = 'LibraryReader'
}

export enum ErrorCode {
  UNAUTHORIZED = 'UNAUTHORIZED',
  FORBIDDEN = 'FORBIDDEN',
  NOT_FOUND = 'NOT_FOUND',
  CONFLICT = 'CONFLICT',
  VALIDATION_ERROR = 'VALIDATION_ERROR',
  SUBSCRIPTION_REQUIRED = 'SUBSCRIPTION_REQUIRED',
  RATE_LIMIT_EXCEEDED = 'RATE_LIMIT_EXCEEDED',
  INTERNAL_ERROR = 'INTERNAL_ERROR'
}

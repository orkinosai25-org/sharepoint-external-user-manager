/**
 * Policy model and interfaces
 */

export type PolicyType = 
  | 'ExternalSharingDefault'
  | 'GuestExpiration'
  | 'RequireApproval'
  | 'AllowedDomains'
  | 'ReviewCampaigns';

export interface Policy {
  id: number;
  tenantId: number;
  policyType: PolicyType;
  enabled: boolean;
  configuration: PolicyConfiguration;
  createdDate: Date;
  modifiedDate: Date;
}

export interface PolicyConfiguration {
  [key: string]: any;
}

export interface GuestExpirationConfig extends PolicyConfiguration {
  expirationDays: number;
  notifyBeforeDays: number;
  autoRenew?: boolean;
}

export interface RequireApprovalConfig extends PolicyConfiguration {
  approvers: string[];
  requireAllApprovers?: boolean;
  autoApproveAfterDays?: number;
}

export interface AllowedDomainsConfig extends PolicyConfiguration {
  mode: 'whitelist' | 'blacklist';
  domains: string[];
  enforceOnInvite: boolean;
}

export interface ReviewCampaignsConfig extends PolicyConfiguration {
  frequency: 'monthly' | 'quarterly' | 'yearly';
  reviewers: string[];
  autoRemoveIfNotReviewed: boolean;
}

export interface PolicyResponse {
  id: number;
  policyType: PolicyType;
  enabled: boolean;
  configuration: PolicyConfiguration;
  modifiedDate: string;
}

export interface UpdatePolicyRequest {
  policyType: PolicyType;
  enabled: boolean;
  configuration: PolicyConfiguration;
}

/**
 * Subscription model and interfaces
 */

import { SubscriptionStatus } from './common';
import { PlanTier } from './plan';

export type SubscriptionTier = 'Free' | 'Pro' | 'Enterprise';

// Alias for new plan tier system (backward compatible)
export type { PlanTier };

export interface Subscription {
  id: number;
  tenantId: number;
  tier: SubscriptionTier;
  startDate: Date;
  endDate?: Date | null;
  trialExpiry?: Date | null;
  gracePeriodEnd?: Date | null;
  status: SubscriptionStatus;
  maxUsers: number;
  features: SubscriptionFeatures;
  createdDate: Date;
  modifiedDate: Date;
}

export interface SubscriptionFeatures {
  auditHistoryDays: number;
  exportEnabled: boolean;
  scheduledReviews: boolean;
  advancedPolicies: boolean;
  customReports: boolean;
  apiAccess: boolean;
  [key: string]: any;
}

export interface SubscriptionResponse {
  tier: SubscriptionTier;
  status: SubscriptionStatus;
  startDate: string;
  endDate: string | null;
  trialExpiry: string | null;
  gracePeriodEnd: string | null;
  maxUsers: number;
  features: SubscriptionFeatures;
  usage?: UsageStats;
}

export interface UsageStats {
  currentUsers: number;
  apiCallsThisMonth: number;
}

export const TIER_LIMITS: Record<SubscriptionTier, SubscriptionFeatures> = {
  Free: {
    auditHistoryDays: 30,
    exportEnabled: false,
    scheduledReviews: false,
    advancedPolicies: false,
    customReports: false,
    apiAccess: false
  },
  Pro: {
    auditHistoryDays: 90,
    exportEnabled: true,
    scheduledReviews: false,
    advancedPolicies: true,
    customReports: false,
    apiAccess: true
  },
  Enterprise: {
    auditHistoryDays: 365,
    exportEnabled: true,
    scheduledReviews: true,
    advancedPolicies: true,
    customReports: true,
    apiAccess: true
  }
};

export const USER_LIMITS: Record<SubscriptionTier, number> = {
  Free: 10,
  Pro: 100,
  Enterprise: 999999 // Effectively unlimited
};

export const RATE_LIMITS: Record<SubscriptionTier, { requestsPerMinute: number; burst: number }> = {
  Free: { requestsPerMinute: 10, burst: 20 },
  Pro: { requestsPerMinute: 100, burst: 150 },
  Enterprise: { requestsPerMinute: 500, burst: 1000 }
};

/**
 * Map legacy subscription tier to new plan tier
 */
export function mapSubscriptionToPlanTier(tier: SubscriptionTier): PlanTier {
  const mapping: Record<SubscriptionTier, PlanTier> = {
    'Free': 'Starter',
    'Pro': 'Professional',
    'Enterprise': 'Enterprise'
  };
  return mapping[tier];
}

/**
 * Map new plan tier to legacy subscription tier (for backward compatibility)
 */
export function mapPlanToSubscriptionTier(tier: PlanTier): SubscriptionTier {
  const mapping: Record<PlanTier, SubscriptionTier> = {
    'Starter': 'Free',
    'Professional': 'Pro',
    'Business': 'Enterprise',
    'Enterprise': 'Enterprise'
  };
  return mapping[tier];
}

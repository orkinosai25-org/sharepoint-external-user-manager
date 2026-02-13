/**
 * Plan enforcement service
 * Determines tenant's active plan and enforces feature/usage limits
 */

import { 
  PlanTier, 
  PlanLimits, 
  PlanFeatures,
  getPlanDefinition,
  getPlanLimits,
  getPlanFeatures,
  hasFeature,
  isUnlimited,
  getMinimumTierForFeature
} from '../models/plan';
import { TenantContext } from '../models/common';
import { FeatureNotAvailableError, ForbiddenError } from '../models/common';

/**
 * Plan enforcement result
 */
export interface PlanEnforcementResult {
  allowed: boolean;
  reason?: string;
  requiredTier?: PlanTier;
  currentUsage?: number;
  limit?: number;
}

/**
 * Get tenant's active plan tier
 */
export function getTenantPlan(context: TenantContext): PlanTier {
  // Map legacy subscription tiers to new plan tiers
  const tierMapping: Record<string, PlanTier> = {
    'Free': 'Starter',
    'Pro': 'Professional',
    'Enterprise': 'Enterprise',
    'Starter': 'Starter',
    'Professional': 'Professional',
    'Business': 'Business'
  };

  return tierMapping[context.subscriptionTier] || 'Starter';
}

/**
 * Get plan limits for tenant
 */
export function getTenantPlanLimits(context: TenantContext): PlanLimits {
  const planTier = getTenantPlan(context);
  return getPlanLimits(planTier);
}

/**
 * Get plan features for tenant
 */
export function getTenantPlanFeatures(context: TenantContext): PlanFeatures {
  const planTier = getTenantPlan(context);
  return getPlanFeatures(planTier);
}

/**
 * Check if tenant has access to a specific feature
 */
export function checkFeatureAccess(
  context: TenantContext,
  featureName: keyof PlanFeatures
): PlanEnforcementResult {
  const planTier = getTenantPlan(context);
  const hasAccess = hasFeature(planTier, featureName);

  if (!hasAccess) {
    const requiredTier = getMinimumTierForFeature(featureName);
    return {
      allowed: false,
      reason: `Feature "${featureName}" requires ${requiredTier} plan or higher`,
      requiredTier: requiredTier || undefined
    };
  }

  return { allowed: true };
}

/**
 * Check if tenant can create more client spaces
 */
export function checkClientSpaceLimit(
  context: TenantContext,
  currentCount: number
): PlanEnforcementResult {
  const planTier = getTenantPlan(context);
  const limits = getPlanLimits(planTier);

  // Check if unlimited
  if (isUnlimited(planTier, 'maxClientSpaces')) {
    return { allowed: true };
  }

  if (currentCount >= limits.maxClientSpaces) {
    return {
      allowed: false,
      reason: `Client space limit reached. Your ${planTier} plan allows up to ${limits.maxClientSpaces} client spaces.`,
      currentUsage: currentCount,
      limit: limits.maxClientSpaces,
      requiredTier: getUpgradeTier(planTier)
    };
  }

  return {
    allowed: true,
    currentUsage: currentCount,
    limit: limits.maxClientSpaces
  };
}

/**
 * Check if tenant can create more external users
 */
export function checkExternalUserLimit(
  context: TenantContext,
  currentCount: number
): PlanEnforcementResult {
  const planTier = getTenantPlan(context);
  const limits = getPlanLimits(planTier);

  if (currentCount >= limits.maxExternalUsers) {
    return {
      allowed: false,
      reason: `External user limit reached. Your ${planTier} plan allows up to ${limits.maxExternalUsers} external users.`,
      currentUsage: currentCount,
      limit: limits.maxExternalUsers,
      requiredTier: getUpgradeTier(planTier)
    };
  }

  return {
    allowed: true,
    currentUsage: currentCount,
    limit: limits.maxExternalUsers
  };
}

/**
 * Check if tenant can create more libraries
 */
export function checkLibraryLimit(
  context: TenantContext,
  currentCount: number
): PlanEnforcementResult {
  const planTier = getTenantPlan(context);
  const limits = getPlanLimits(planTier);

  if (currentCount >= limits.maxLibraries) {
    return {
      allowed: false,
      reason: `Library limit reached. Your ${planTier} plan allows up to ${limits.maxLibraries} libraries.`,
      currentUsage: currentCount,
      limit: limits.maxLibraries,
      requiredTier: getUpgradeTier(planTier)
    };
  }

  return {
    allowed: true,
    currentUsage: currentCount,
    limit: limits.maxLibraries
  };
}

/**
 * Check API rate limit for tenant
 */
export function checkApiCallLimit(
  context: TenantContext,
  currentMonthCalls: number
): PlanEnforcementResult {
  const planTier = getTenantPlan(context);
  const limits = getPlanLimits(planTier);

  if (currentMonthCalls >= limits.apiCallsPerMonth) {
    return {
      allowed: false,
      reason: `API call limit reached. Your ${planTier} plan allows up to ${limits.apiCallsPerMonth} API calls per month.`,
      currentUsage: currentMonthCalls,
      limit: limits.apiCallsPerMonth,
      requiredTier: getUpgradeTier(planTier)
    };
  }

  return {
    allowed: true,
    currentUsage: currentMonthCalls,
    limit: limits.apiCallsPerMonth
  };
}

/**
 * Get audit retention days for tenant
 */
export function getAuditRetentionDays(context: TenantContext): number {
  const planTier = getTenantPlan(context);
  const limits = getPlanLimits(planTier);
  return limits.auditRetentionDays;
}

/**
 * Check if audit data should be retained
 */
export function shouldRetainAudit(
  context: TenantContext,
  auditDate: Date
): boolean {
  const planTier = getTenantPlan(context);
  
  // Check if unlimited retention
  if (isUnlimited(planTier, 'auditRetentionDays')) {
    return true;
  }

  const retentionDays = getAuditRetentionDays(context);
  const cutoffDate = new Date();
  cutoffDate.setDate(cutoffDate.getDate() - retentionDays);

  return auditDate >= cutoffDate;
}

/**
 * Get support level for tenant
 */
export function getSupportLevel(context: TenantContext): string {
  const planTier = getTenantPlan(context);
  const limits = getPlanLimits(planTier);
  return limits.supportLevel;
}

/**
 * Enforce feature access - throws error if not allowed
 */
export function enforceFeatureAccess(
  context: TenantContext,
  featureName: keyof PlanFeatures
): void {
  const result = checkFeatureAccess(context, featureName);
  
  if (!result.allowed) {
    const planTier = getTenantPlan(context);
    throw new FeatureNotAvailableError(featureName, result.requiredTier || planTier);
  }
}

/**
 * Enforce client space limit - throws error if limit exceeded
 */
export function enforceClientSpaceLimit(
  context: TenantContext,
  currentCount: number
): void {
  const result = checkClientSpaceLimit(context, currentCount);
  
  if (!result.allowed) {
    throw new ForbiddenError(
      result.reason || 'Client space limit exceeded',
      `Please upgrade to ${result.requiredTier} plan to add more client spaces`
    );
  }
}

/**
 * Enforce external user limit - throws error if limit exceeded
 */
export function enforceExternalUserLimit(
  context: TenantContext,
  currentCount: number
): void {
  const result = checkExternalUserLimit(context, currentCount);
  
  if (!result.allowed) {
    throw new ForbiddenError(
      result.reason || 'External user limit exceeded',
      `Please upgrade to ${result.requiredTier} plan to add more users`
    );
  }
}

/**
 * Get next tier for upgrade
 */
function getUpgradeTier(currentTier: PlanTier): PlanTier | undefined {
  const tierOrder: PlanTier[] = ['Starter', 'Professional', 'Business', 'Enterprise'];
  const currentIndex = tierOrder.indexOf(currentTier);
  
  if (currentIndex >= 0 && currentIndex < tierOrder.length - 1) {
    return tierOrder[currentIndex + 1];
  }
  
  return undefined;
}

/**
 * Get plan definition for tenant
 */
export function getTenantPlanDefinition(context: TenantContext) {
  const planTier = getTenantPlan(context);
  return getPlanDefinition(planTier);
}

/**
 * Check if tenant is on a specific tier or higher
 */
export function hasMinimumTier(context: TenantContext, minimumTier: PlanTier): boolean {
  const tierOrder: PlanTier[] = ['Starter', 'Professional', 'Business', 'Enterprise'];
  const currentTier = getTenantPlan(context);
  
  const currentIndex = tierOrder.indexOf(currentTier);
  const minimumIndex = tierOrder.indexOf(minimumTier);
  
  return currentIndex >= minimumIndex;
}

/**
 * Check if tenant has access to global search (cross-client search)
 */
export function checkGlobalSearchAccess(
  context: TenantContext
): PlanEnforcementResult {
  const result = checkFeatureAccess(context, 'globalSearch');
  
  if (!result.allowed) {
    return {
      allowed: false,
      reason: 'Global search requires Professional plan or higher',
      requiredTier: result.requiredTier
    };
  }

  return { allowed: true };
}

/**
 * Check if tenant has access to full-text search
 */
export function checkFullTextSearchAccess(
  context: TenantContext
): PlanEnforcementResult {
  const result = checkFeatureAccess(context, 'fullTextSearch');
  
  if (!result.allowed) {
    return {
      allowed: false,
      reason: 'Full-text search requires Professional plan or higher',
      requiredTier: result.requiredTier
    };
  }

  return { allowed: true };
}

/**
 * Check if tenant has access to advanced search filters
 */
export function checkAdvancedSearchFiltersAccess(
  context: TenantContext
): PlanEnforcementResult {
  const result = checkFeatureAccess(context, 'advancedSearchFilters');
  
  if (!result.allowed) {
    return {
      allowed: false,
      reason: 'Advanced search filters require Professional plan or higher',
      requiredTier: result.requiredTier
    };
  }

  return { allowed: true };
}

/**
 * Enforce global search access - throws error if not allowed
 */
export function enforceGlobalSearchAccess(context: TenantContext): void {
  const result = checkGlobalSearchAccess(context);
  
  if (!result.allowed) {
    throw new FeatureNotAvailableError('globalSearch', result.requiredTier || 'Professional');
  }
}

/**
 * Enforce full-text search access - throws error if not allowed
 */
export function enforceFullTextSearchAccess(context: TenantContext): void {
  const result = checkFullTextSearchAccess(context);
  
  if (!result.allowed) {
    throw new FeatureNotAvailableError('fullTextSearch', result.requiredTier || 'Professional');
  }
}

/**
 * Enforce advanced search filters access - throws error if not allowed
 */
export function enforceAdvancedSearchFiltersAccess(context: TenantContext): void {
  const result = checkAdvancedSearchFiltersAccess(context);
  
  if (!result.allowed) {
    throw new FeatureNotAvailableError('advancedSearchFilters', result.requiredTier || 'Professional');
  }
}

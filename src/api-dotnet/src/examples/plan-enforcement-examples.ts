/**
 * Example integration of plan enforcement in API handlers
 * This file demonstrates how to properly integrate plan checking and enforcement
 */

// Unused imports are kept for reference in examples
// eslint-disable-next-line @typescript-eslint/no-unused-vars
import { HttpRequest, InvocationContext } from '@azure/functions';
import { TenantContext } from '../models/common';
import {
  getTenantPlan,
  getTenantPlanLimits,
  getTenantPlanFeatures,
  getTenantPlanDefinition,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  checkFeatureAccess,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  checkClientSpaceLimit,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  checkExternalUserLimit,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  enforceFeatureAccess,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  enforceClientSpaceLimit,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  enforceExternalUserLimit,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  getAuditRetentionDays,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  shouldRetainAudit,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  getSupportLevel,
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  hasMinimumTier
} from '../services/plan-enforcement';

/**
 * Example 1: Get tenant plan information
 * Use this in a GET /tenant/plan endpoint to show plan details to users
 */
// eslint-disable-next-line @typescript-eslint/explicit-function-return-type
export async function getTenantPlanInfo(context: TenantContext) {
  const plan = getTenantPlan(context);
  const definition = getTenantPlanDefinition(context);
  const limits = getTenantPlanLimits(context);
  const features = getTenantPlanFeatures(context);

  // Get current usage (would come from database in real scenario)
  const currentUsage = {
    clientSpaces: 3,
    externalUsers: 25,
    libraries: 15,
    apiCallsThisMonth: 5000
  };

  return {
    plan: {
      tier: plan,
      displayName: definition.displayName,
      description: definition.description,
      pricing: definition.pricing
    },
    limits: {
      clientSpaces: {
        current: currentUsage.clientSpaces,
        max: limits.maxClientSpaces,
        unlimited: limits.unlimitedClientSpaces || false
      },
      externalUsers: {
        current: currentUsage.externalUsers,
        max: limits.maxExternalUsers
      },
      libraries: {
        current: currentUsage.libraries,
        max: limits.maxLibraries
      },
      apiCalls: {
        currentMonth: currentUsage.apiCallsThisMonth,
        max: limits.apiCallsPerMonth
      },
      auditRetentionDays: limits.auditRetentionDays,
      supportLevel: limits.supportLevel
    },
    features: {
      auditExport: features.auditExport,
      bulkOperations: features.bulkOperations,
      advancedReporting: features.advancedReporting,
      customPolicies: features.customPolicies,
      apiAccess: features.apiAccess,
      scheduledReviews: features.scheduledReviews,
      ssoIntegration: features.ssoIntegration,
      customBranding: features.customBranding
    }
  };
}

/**
 * Example 2: Checking feature availability before showing UI
 * Use this in GET /features endpoint to determine what UI to show
 */
// eslint-disable-next-line @typescript-eslint/explicit-function-return-type
export async function getAvailableFeatures(context: TenantContext) {
  const features = getTenantPlanFeatures(context);

  return {
    features: {
      // Features available to all plans
      basicUserManagement: true,
      basicAuditLogs: true,
      emailNotifications: true,

      // Plan-gated features
      auditExport: features.auditExport,
      bulkOperations: features.bulkOperations,
      advancedReporting: features.advancedReporting,
      customPolicies: features.customPolicies,
      apiAccess: features.apiAccess,
      scheduledReviews: features.scheduledReviews,
      ssoIntegration: features.ssoIntegration,
      customBranding: features.customBranding
    },
    // Include what tier is required for unavailable features
    upgradePrompts: {
      auditExport: !features.auditExport ? 'Professional' : null,
      advancedReporting: !features.advancedReporting ? 'Business' : null,
      customBranding: !features.customBranding ? 'Enterprise' : null
    }
  };
}

/**
 * Example 3: Soft checking limits to show warnings
 * Use this to show warnings in UI before user hits the limit
 */
// eslint-disable-next-line @typescript-eslint/explicit-function-return-type
export async function checkQuotaStatus(
  context: TenantContext,
  currentCounts: {
    clientSpaces: number;
    externalUsers: number;
    libraries: number;
    apiCallsThisMonth: number;
  }
) {
  const limits = getTenantPlanLimits(context);

  return {
    clientSpaces: {
      current: currentCounts.clientSpaces,
      limit: limits.maxClientSpaces,
      percentage: (currentCounts.clientSpaces / limits.maxClientSpaces) * 100,
      nearLimit: currentCounts.clientSpaces >= limits.maxClientSpaces * 0.8,
      atLimit: currentCounts.clientSpaces >= limits.maxClientSpaces
    },
    externalUsers: {
      current: currentCounts.externalUsers,
      limit: limits.maxExternalUsers,
      percentage: (currentCounts.externalUsers / limits.maxExternalUsers) * 100,
      nearLimit: currentCounts.externalUsers >= limits.maxExternalUsers * 0.8,
      atLimit: currentCounts.externalUsers >= limits.maxExternalUsers
    },
    libraries: {
      current: currentCounts.libraries,
      limit: limits.maxLibraries,
      percentage: (currentCounts.libraries / limits.maxLibraries) * 100,
      nearLimit: currentCounts.libraries >= limits.maxLibraries * 0.8,
      atLimit: currentCounts.libraries >= limits.maxLibraries
    },
    apiCalls: {
      current: currentCounts.apiCallsThisMonth,
      limit: limits.apiCallsPerMonth,
      percentage: (currentCounts.apiCallsThisMonth / limits.apiCallsPerMonth) * 100,
      nearLimit: currentCounts.apiCallsThisMonth >= limits.apiCallsPerMonth * 0.8,
      atLimit: currentCounts.apiCallsThisMonth >= limits.apiCallsPerMonth
    }
  };
}

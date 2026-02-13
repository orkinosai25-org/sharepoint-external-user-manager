/**
 * Plan definitions and pricing configuration
 * Defines subscription plans with their limits and features
 */

export type PlanTier = 'Starter' | 'Professional' | 'Business' | 'Enterprise';

export type SupportLevel = 'community' | 'email' | 'priority' | 'dedicated';

/**
 * Plan limits interface
 */
export interface PlanLimits {
  maxClientSpaces: number;
  auditRetentionDays: number;
  supportLevel: SupportLevel;
  maxExternalUsers: number;
  maxLibraries: number;
  apiCallsPerMonth: number;
  maxAdmins: number;
  unlimitedClientSpaces?: boolean;
  unlimitedAuditRetention?: boolean;
}

/**
 * Plan features interface
 */
export interface PlanFeatures {
  auditExport: boolean;
  bulkOperations: boolean;
  advancedReporting: boolean;
  customPolicies: boolean;
  apiAccess: boolean;
  scheduledReviews: boolean;
  ssoIntegration: boolean;
  customBranding: boolean;
  // Search features
  globalSearch: boolean;
  fullTextSearch: boolean;
  advancedSearchFilters: boolean;
}

/**
 * Full plan definition
 */
export interface PlanDefinition {
  tier: PlanTier;
  displayName: string;
  description: string;
  pricing: {
    monthly: number;
    annual: number;
    currency: string;
  };
  limits: PlanLimits;
  features: PlanFeatures;
}

/**
 * Plan definitions for all tiers
 */
export const PLAN_DEFINITIONS: Record<PlanTier, PlanDefinition> = {
  Starter: {
    tier: 'Starter',
    displayName: 'Starter',
    description: 'Perfect for small teams getting started with external user management',
    pricing: {
      monthly: 29,
      annual: 290,
      currency: 'USD'
    },
    limits: {
      maxClientSpaces: 5,
      auditRetentionDays: 30,
      supportLevel: 'community',
      maxExternalUsers: 50,
      maxLibraries: 25,
      apiCallsPerMonth: 10000,
      maxAdmins: 2
    },
    features: {
      auditExport: false,
      bulkOperations: false,
      advancedReporting: false,
      customPolicies: false,
      apiAccess: false,
      scheduledReviews: false,
      ssoIntegration: false,
      customBranding: false,
      globalSearch: false,
      fullTextSearch: false,
      advancedSearchFilters: false
    }
  },
  Professional: {
    tier: 'Professional',
    displayName: 'Professional',
    description: 'Advanced features for growing businesses',
    pricing: {
      monthly: 99,
      annual: 990,
      currency: 'USD'
    },
    limits: {
      maxClientSpaces: 20,
      auditRetentionDays: 90,
      supportLevel: 'email',
      maxExternalUsers: 250,
      maxLibraries: 100,
      apiCallsPerMonth: 50000,
      maxAdmins: 5
    },
    features: {
      auditExport: true,
      bulkOperations: true,
      advancedReporting: false,
      customPolicies: true,
      apiAccess: true,
      scheduledReviews: false,
      ssoIntegration: false,
      customBranding: false,
      globalSearch: true,
      fullTextSearch: true,
      advancedSearchFilters: true
    }
  },
  Business: {
    tier: 'Business',
    displayName: 'Business',
    description: 'Comprehensive solution for established organizations',
    pricing: {
      monthly: 299,
      annual: 2990,
      currency: 'USD'
    },
    limits: {
      maxClientSpaces: 100,
      auditRetentionDays: 365,
      supportLevel: 'priority',
      maxExternalUsers: 1000,
      maxLibraries: 500,
      apiCallsPerMonth: 250000,
      maxAdmins: 15
    },
    features: {
      auditExport: true,
      bulkOperations: true,
      advancedReporting: true,
      customPolicies: true,
      apiAccess: true,
      scheduledReviews: true,
      ssoIntegration: true,
      customBranding: false,
      globalSearch: true,
      fullTextSearch: true,
      advancedSearchFilters: true
    }
  },
  Enterprise: {
    tier: 'Enterprise',
    displayName: 'Enterprise',
    description: 'Ultimate solution with unlimited resources and dedicated support',
    pricing: {
      monthly: 999,
      annual: 9990,
      currency: 'USD'
    },
    limits: {
      maxClientSpaces: 999999,
      auditRetentionDays: 2555,
      supportLevel: 'dedicated',
      maxExternalUsers: 999999,
      maxLibraries: 999999,
      apiCallsPerMonth: 999999,
      maxAdmins: 999,
      unlimitedClientSpaces: true,
      unlimitedAuditRetention: true
    },
    features: {
      auditExport: true,
      bulkOperations: true,
      advancedReporting: true,
      customPolicies: true,
      apiAccess: true,
      scheduledReviews: true,
      ssoIntegration: true,
      customBranding: true,
      globalSearch: true,
      fullTextSearch: true,
      advancedSearchFilters: true
    }
  }
};

/**
 * Get plan definition by tier
 */
export function getPlanDefinition(tier: PlanTier): PlanDefinition {
  return PLAN_DEFINITIONS[tier];
}

/**
 * Get plan limits by tier
 */
export function getPlanLimits(tier: PlanTier): PlanLimits {
  return PLAN_DEFINITIONS[tier].limits;
}

/**
 * Get plan features by tier
 */
export function getPlanFeatures(tier: PlanTier): PlanFeatures {
  return PLAN_DEFINITIONS[tier].features;
}

/**
 * Check if a limit is unlimited for a plan
 */
export function isUnlimited(tier: PlanTier, limitName: keyof PlanLimits): boolean {
  const plan = PLAN_DEFINITIONS[tier];
  
  if (tier === 'Enterprise') {
    if (limitName === 'maxClientSpaces' && plan.limits.unlimitedClientSpaces) {
      return true;
    }
    if (limitName === 'auditRetentionDays' && plan.limits.unlimitedAuditRetention) {
      return true;
    }
  }
  
  return false;
}

/**
 * Compare if a feature is available in a given tier
 */
export function hasFeature(tier: PlanTier, featureName: keyof PlanFeatures): boolean {
  return PLAN_DEFINITIONS[tier].features[featureName];
}

/**
 * Get the minimum tier required for a feature
 */
export function getMinimumTierForFeature(featureName: keyof PlanFeatures): PlanTier | null {
  const tierOrder: PlanTier[] = ['Starter', 'Professional', 'Business', 'Enterprise'];
  
  for (const tier of tierOrder) {
    if (PLAN_DEFINITIONS[tier].features[featureName]) {
      return tier;
    }
  }
  
  return null;
}

/**
 * Get all available plan tiers
 */
export function getAllPlanTiers(): PlanTier[] {
  return Object.keys(PLAN_DEFINITIONS) as PlanTier[];
}

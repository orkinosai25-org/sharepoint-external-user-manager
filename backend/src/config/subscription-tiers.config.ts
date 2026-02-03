export interface SubscriptionTier {
  name: string;
  limits: SubscriptionLimits;
}

export interface SubscriptionLimits {
  maxExternalUsers: number;
  auditLogRetentionDays: number;
  apiRateLimit: number;
  advancedPolicies: boolean;
  supportLevel: 'community' | 'email' | 'priority' | 'dedicated';
}

export const SUBSCRIPTION_TIERS: Record<string, SubscriptionTier> = {
  Free: {
    name: 'Free',
    limits: {
      maxExternalUsers: 10,
      auditLogRetentionDays: 30,
      apiRateLimit: 50,
      advancedPolicies: false,
      supportLevel: 'community'
    }
  },
  Trial: {
    name: 'Trial',
    limits: {
      maxExternalUsers: 100,
      auditLogRetentionDays: 365,
      apiRateLimit: 200,
      advancedPolicies: true,
      supportLevel: 'email'
    }
  },
  Pro: {
    name: 'Pro',
    limits: {
      maxExternalUsers: 100,
      auditLogRetentionDays: 365,
      apiRateLimit: 200,
      advancedPolicies: true,
      supportLevel: 'priority'
    }
  },
  Enterprise: {
    name: 'Enterprise',
    limits: {
      maxExternalUsers: -1, // Unlimited
      auditLogRetentionDays: -1, // Unlimited
      apiRateLimit: 1000,
      advancedPolicies: true,
      supportLevel: 'dedicated'
    }
  }
};

export function getSubscriptionTier(tierName: string): SubscriptionTier {
  return SUBSCRIPTION_TIERS[tierName] || SUBSCRIPTION_TIERS.Free;
}

export function canAccessFeature(
  tier: string,
  feature: string
): boolean {
  const tierConfig = getSubscriptionTier(tier);
  
  switch (feature) {
    case 'advancedPolicies':
      return tierConfig.limits.advancedPolicies;
    case 'unlimitedUsers':
      return tierConfig.limits.maxExternalUsers === -1;
    case 'prioritySupport':
      return ['priority', 'dedicated'].includes(tierConfig.limits.supportLevel);
    default:
      return true;
  }
}

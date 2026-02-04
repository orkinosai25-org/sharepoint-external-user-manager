import { HttpRequest, InvocationContext } from '@azure/functions';
import { TenantContext, ApiResponse, ErrorCode } from '../models/types';

/**
 * License check middleware
 */
export async function checkLicense(
  request: HttpRequest,
  context: TenantContext,
  invocationContext: InvocationContext,
  feature?: string
): Promise<{ allowed: boolean; response?: ApiResponse<never> }> {
  try {
    const subscription = context.subscription;
    
    // Check if subscription is active
    if (subscription.status !== 'active') {
      return {
        allowed: false,
        response: {
          success: false,
          error: {
            code: ErrorCode.SUBSCRIPTION_REQUIRED,
            message: 'Active subscription required',
            details: `Subscription status: ${subscription.status}. Please upgrade to continue.`,
            timestamp: new Date().toISOString()
          }
        }
      };
    }

    // Check trial expiration
    if (subscription.tier === 'trial' && subscription.endDate) {
      const endDate = new Date(subscription.endDate);
      if (endDate < new Date()) {
        return {
          allowed: false,
          response: {
            success: false,
            error: {
              code: ErrorCode.SUBSCRIPTION_REQUIRED,
              message: 'Trial expired',
              details: 'Your trial has expired. Please upgrade to continue using the service.',
              timestamp: new Date().toISOString()
            }
          }
        };
      }
    }

    // Check feature-specific entitlements
    if (feature) {
      const hasFeature = checkFeatureEntitlement(subscription.tier, feature);
      if (!hasFeature) {
        return {
          allowed: false,
          response: {
            success: false,
            error: {
              code: ErrorCode.FORBIDDEN,
              message: 'Feature not available',
              details: `This feature requires ${getRequiredTierForFeature(feature)} or higher subscription.`,
              timestamp: new Date().toISOString()
            }
          }
        };
      }
    }

    // Check usage limits
    const limitsCheck = checkUsageLimits(subscription);
    if (!limitsCheck.allowed) {
      return {
        allowed: false,
        response: limitsCheck.response
      };
    }

    return { allowed: true };
  } catch (error) {
    invocationContext.error('Error checking license', error);
    return {
      allowed: false,
      response: {
        success: false,
        error: {
          code: ErrorCode.INTERNAL_ERROR,
          message: 'License check failed',
          details: 'An error occurred while validating your subscription',
          timestamp: new Date().toISOString()
        }
      }
    };
  }
}

/**
 * Check if subscription tier has access to feature
 */
function checkFeatureEntitlement(tier: string, feature: string): boolean {
  const featureMap: Record<string, string[]> = {
    'trial': ['basic', 'user_management', 'library_management'],
    'pro': ['basic', 'user_management', 'library_management', 'audit_basic', 'bulk_operations'],
    'enterprise': ['basic', 'user_management', 'library_management', 'audit_basic', 'bulk_operations', 'audit_export', 'advanced_reporting', 'custom_policies']
  };

  return featureMap[tier]?.includes(feature) ?? false;
}

/**
 * Get required tier for a feature
 */
function getRequiredTierForFeature(feature: string): string {
  const requiresEnterprise = ['audit_export', 'advanced_reporting', 'custom_policies'];
  const requiresPro = ['audit_basic', 'bulk_operations'];
  
  if (requiresEnterprise.includes(feature)) return 'Enterprise';
  if (requiresPro.includes(feature)) return 'Pro';
  return 'Trial';
}

/**
 * Check usage limits
 */
function checkUsageLimits(subscription: any): { allowed: boolean; response?: ApiResponse<never> } {
  const { limits, usage } = subscription;

  // Check external users limit
  if (usage.externalUsersCount >= limits.maxExternalUsers) {
    return {
      allowed: false,
      response: {
        success: false,
        error: {
          code: ErrorCode.FORBIDDEN,
          message: 'User limit reached',
          details: `You have reached the maximum number of external users (${limits.maxExternalUsers}). Please upgrade your plan.`,
          timestamp: new Date().toISOString()
        }
      }
    };
  }

  // Check libraries limit
  if (usage.librariesCount >= limits.maxLibraries) {
    return {
      allowed: false,
      response: {
        success: false,
        error: {
          code: ErrorCode.FORBIDDEN,
          message: 'Library limit reached',
          details: `You have reached the maximum number of libraries (${limits.maxLibraries}). Please upgrade your plan.`,
          timestamp: new Date().toISOString()
        }
      }
    };
  }

  // Check API calls limit (soft limit - log warning but allow)
  if (usage.apiCallsThisMonth >= limits.apiCallsPerMonth) {
    // Log warning but don't block
    console.warn(`Tenant approaching API limit: ${usage.apiCallsThisMonth}/${limits.apiCallsPerMonth}`);
  }

  return { allowed: true };
}

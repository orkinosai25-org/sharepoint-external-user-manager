import { Response, NextFunction } from 'express';
import { TenantRequest } from './tenant.middleware';
import { getSubscriptionTier, canAccessFeature } from '../config/subscription-tiers.config';

export interface LicensingRequest extends TenantRequest {
  subscriptionLimits?: {
    maxExternalUsers: number;
    apiRateLimit: number;
    advancedPolicies: boolean;
  };
}

export function requireFeature(featureName: string) {
  return async (
    req: LicensingRequest,
    res: Response,
    next: NextFunction
  ): Promise<void> => {
    try {
      const tier = req.tenant?.subscription_tier || 'Free';
      
      if (!canAccessFeature(tier, featureName)) {
        res.status(402).json({
          success: false,
          error: {
            code: 'FEATURE_NOT_AVAILABLE',
            message: `This feature is not available in your current subscription tier`,
            details: {
              feature: featureName,
              current_tier: tier,
              required_tier: 'Pro',
              upgrade_url: `${process.env.APP_BASE_URL}/subscription/upgrade`
            }
          }
        });
        return;
      }
      
      next();
    } catch (error) {
      next(error);
    }
  };
}

export async function checkUserQuota(
  req: LicensingRequest,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const tier = req.tenant?.subscription_tier || 'Free';
    const tierConfig = getSubscriptionTier(tier);
    
    // Mock current user count - replace with actual database query
    const currentUserCount = 5;
    
    // Check if adding new user would exceed quota
    if (
      tierConfig.limits.maxExternalUsers !== -1 &&
      currentUserCount >= tierConfig.limits.maxExternalUsers
    ) {
      res.status(402).json({
        success: false,
        error: {
          code: 'QUOTA_EXCEEDED',
          message: 'External user limit reached for your subscription tier',
          details: {
            current_users: currentUserCount,
            max_users: tierConfig.limits.maxExternalUsers,
            subscription_tier: tier
          },
          upgrade_url: `${process.env.APP_BASE_URL}/subscription/upgrade`
        }
      });
      return;
    }
    
    // Attach limits to request for use in route handlers
    req.subscriptionLimits = {
      maxExternalUsers: tierConfig.limits.maxExternalUsers,
      apiRateLimit: tierConfig.limits.apiRateLimit,
      advancedPolicies: tierConfig.limits.advancedPolicies
    };
    
    next();
  } catch (error) {
    next(error);
  }
}

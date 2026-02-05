/**
 * Subscription enforcement middleware
 */

import { HttpRequest } from '@azure/functions';
import { TenantContext, SubscriptionError, FeatureNotAvailableError } from '../models/common';
import { SubscriptionTier, TIER_LIMITS, USER_LIMITS } from '../models/subscription';
import { databaseService } from '../services/database';

export async function enforceSubscription(
  _req: HttpRequest,
  context: TenantContext
): Promise<void> {
  const subscription = await databaseService.getSubscriptionByTenantId(context.tenantId);
  
  if (!subscription) {
    throw new SubscriptionError('No subscription found');
  }

  // Check subscription status
  if (subscription.status === 'Expired') {
    throw new SubscriptionError('Subscription expired', 'Please renew your subscription to continue');
  }

  if (subscription.status === 'Cancelled') {
    throw new SubscriptionError('Subscription cancelled');
  }

  // Check trial expiry
  if (subscription.status === 'Trial' && subscription.trialExpiry) {
    const now = new Date();
    if (now > subscription.trialExpiry) {
      // Check if in grace period
      if (subscription.gracePeriodEnd && now <= subscription.gracePeriodEnd) {
        console.log('Subscription in grace period');
      } else {
        throw new SubscriptionError(
          'Trial expired',
          'Your trial has expired. Please upgrade to continue using the service.'
        );
      }
    }
  }
}

export function checkFeatureAccess(
  context: TenantContext,
  featureName: string
): void {
  const tierLimits = TIER_LIMITS[context.subscriptionTier];
  
  if (!(featureName in tierLimits) || !tierLimits[featureName as keyof typeof tierLimits]) {
    const requiredTier = findRequiredTier(featureName);
    throw new FeatureNotAvailableError(featureName, requiredTier);
  }
}

export async function checkUserQuota(context: TenantContext): Promise<void> {
  const maxUsers = USER_LIMITS[context.subscriptionTier];
  
  // In a real implementation, you would query the actual user count
  // For now, we'll just check against the limit
  // const currentUsers = await databaseService.getUserCount(context.tenantId);
  const currentUsers = 0; // Placeholder
  
  if (currentUsers >= maxUsers) {
    throw new SubscriptionError(
      'User quota exceeded',
      `Your current plan allows up to ${maxUsers} users. Please upgrade to add more users.`
    );
  }
}

function findRequiredTier(featureName: string): SubscriptionTier {
  if (featureName in TIER_LIMITS.Pro && TIER_LIMITS.Pro[featureName as keyof typeof TIER_LIMITS.Pro]) {
    return 'Pro';
  }
  return 'Enterprise';
}

export function getSubscriptionTier(req: HttpRequest): SubscriptionTier {
  const context = (req as any).tenantContext as TenantContext;
  if (!context) {
    throw new Error('Tenant context not found');
  }
  return context.subscriptionTier;
}

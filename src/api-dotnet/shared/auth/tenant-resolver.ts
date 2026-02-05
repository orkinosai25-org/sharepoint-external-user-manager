import { InvocationContext } from '@azure/functions';
import { TokenPayload, TenantContext, Subscription } from '../models/types';
import { getTenantSubscription } from '../storage/subscription-repository';

/**
 * Resolve tenant context from token and load subscription
 */
export async function resolveTenantContext(
  tokenPayload: TokenPayload,
  tenantId: string,
  context: InvocationContext
): Promise<TenantContext | null> {
  try {
    // Load subscription from Cosmos DB
    const subscription = await getTenantSubscription(tenantId);
    
    if (!subscription) {
      context.warn(`No subscription found for tenant: ${tenantId}`);
      return null;
    }

    // Check if subscription is active
    if (subscription.status !== 'active') {
      context.warn(`Tenant subscription is not active: ${tenantId}, status: ${subscription.status}`);
      return null;
    }

    // Build tenant context
    const tenantContext: TenantContext = {
      tenantId: tenantId,
      userId: tokenPayload.oid || tokenPayload.sub,
      userEmail: tokenPayload.email || tokenPayload.upn || '',
      userName: tokenPayload.name,
      roles: tokenPayload.roles || [],
      subscription: subscription
    };

    return tenantContext;
  } catch (error) {
    context.error('Error resolving tenant context', error);
    return null;
  }
}

/**
 * Validate tenant ID matches token tenant
 */
export function validateTenantId(tokenPayload: TokenPayload, requestedTenantId: string): boolean {
  // For multi-tenant apps, token tenant ID should match requested tenant
  return tokenPayload.tid === requestedTenantId;
}

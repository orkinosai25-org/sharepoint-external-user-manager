import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { getTenant } from '../shared/storage/tenant-repository';
import { getTenantSubscription } from '../shared/storage/subscription-repository';
import { validateToken, getTenantId, createUnauthorizedResponse } from '../shared/auth/jwt-validator';
import { resolveTenantContext } from '../shared/auth/tenant-resolver';
import { handleError, NotFoundError } from '../shared/middleware/error-handler';
import { generateCorrelationId } from '../shared/utils/helpers';
import { checkRateLimit } from '../shared/middleware/rate-limit';
import { ApiResponse } from '../shared/models/types';

/**
 * GET /tenants/me
 * Get current tenant information
 */
async function getTenantInfo(request: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
  const correlationId = generateCorrelationId();
  
  try {
    // Validate authentication
    const tokenPayload = await validateToken(request, context);
    if (!tokenPayload) {
      return {
        status: 401,
        jsonBody: createUnauthorizedResponse(correlationId)
      };
    }

    // Get tenant ID
    const tenantId = getTenantId(request, tokenPayload);
    if (!tenantId) {
      return {
        status: 400,
        jsonBody: {
          success: false,
          error: {
            code: 'BAD_REQUEST',
            message: 'Tenant ID required',
            details: 'X-Tenant-ID header is required',
            correlationId
          }
        }
      };
    }

    // Rate limiting
    const rateLimitResult = await checkRateLimit(request, tenantId, context);
    if (!rateLimitResult.allowed) {
      return {
        status: 429,
        headers: rateLimitResult.headers,
        jsonBody: rateLimitResult.response
      };
    }

    // Get tenant and subscription
    const tenant = await getTenant(tenantId);
    if (!tenant) {
      throw new NotFoundError('Tenant not found', `Tenant ${tenantId} does not exist`);
    }

    const subscription = await getTenantSubscription(tenantId);
    if (!subscription) {
      throw new NotFoundError('Subscription not found', `Subscription for tenant ${tenantId} does not exist`);
    }

    // Build response
    const response: ApiResponse<any> = {
      success: true,
      data: {
        tenantId: tenant.tenantId,
        displayName: tenant.displayName,
        status: tenant.status,
        subscriptionTier: tenant.subscriptionTier,
        subscriptionStatus: tenant.subscriptionStatus,
        subscriptionEndDate: subscription.endDate,
        features: tenant.settings.features,
        limits: {
          maxExternalUsers: subscription.limits.maxExternalUsers,
          maxLibraries: subscription.limits.maxLibraries,
          apiCallsPerMonth: subscription.limits.apiCallsPerMonth,
          currentExternalUsers: subscription.usage.externalUsersCount,
          currentLibraries: subscription.usage.librariesCount,
          apiCallsThisMonth: subscription.usage.apiCallsThisMonth
        },
        createdDate: tenant.createdDate,
        lastModifiedDate: tenant.lastModifiedDate
      }
    };

    return {
      status: 200,
      headers: rateLimitResult.headers,
      jsonBody: response
    };
  } catch (error) {
    const errorResponse = handleError(error, context, correlationId);
    const status = errorResponse.error?.code === 'NOT_FOUND' ? 404 : 500;
    
    return {
      status,
      jsonBody: errorResponse
    };
  }
}

// Register HTTP trigger
app.http('getTenant', {
  methods: ['GET'],
  route: 'tenants/me',
  authLevel: 'anonymous',
  handler: getTenantInfo
});

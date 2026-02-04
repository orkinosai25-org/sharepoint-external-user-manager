/**
 * GET /tenants/subscription - Get subscription status and features
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { authenticateRequest } from '../../middleware/auth';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { SubscriptionResponse } from '../../models/subscription';

async function getSubscription(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
  const correlationId = attachCorrelationId(req);

  try {
    // Handle CORS preflight
    const corsResponse = handleCorsPreFlight(req);
    if (corsResponse) {
      return corsResponse;
    }

    // Ensure database is connected
    await databaseService.connect();

    // Authenticate request
    const tenantContext = await authenticateRequest(req, context);

    // Get subscription details
    const subscription = await databaseService.getSubscriptionByTenantId(tenantContext.tenantId);
    if (!subscription) {
      throw new Error('Subscription not found');
    }

    // Build response with usage stats (placeholder)
    const response: SubscriptionResponse = {
      tier: subscription.tier,
      status: subscription.status,
      startDate: subscription.startDate.toISOString(),
      endDate: subscription.endDate?.toISOString() || null,
      trialExpiry: subscription.trialExpiry?.toISOString() || null,
      gracePeriodEnd: subscription.gracePeriodEnd?.toISOString() || null,
      maxUsers: subscription.maxUsers,
      features: subscription.features,
      usage: {
        currentUsers: 0, // Placeholder - would query actual user count
        apiCallsThisMonth: 0 // Placeholder - would query from metrics
      }
    };

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('getSubscription', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'tenants/subscription',
  handler: getSubscription
});

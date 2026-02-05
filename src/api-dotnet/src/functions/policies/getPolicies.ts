/**
 * GET /policies - Get collaboration policies for the tenant
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { authenticateRequest } from '../../middleware/auth';
import { enforceSubscription } from '../../middleware/subscription';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { PolicyResponse } from '../../models/policy';

async function getPolicies(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Enforce subscription
    await enforceSubscription(req, tenantContext);

    // Get policies from database
    const policies = await databaseService.getPoliciesByTenantId(tenantContext.tenantId);

    // Format response
    const policyResponses: PolicyResponse[] = policies.map(policy => ({
      id: policy.id,
      policyType: policy.policyType,
      enabled: policy.enabled,
      configuration: policy.configuration,
      modifiedDate: policy.modifiedDate.toISOString()
    }));

    const successResponse = createSuccessResponse(policyResponses, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('getPolicies', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'policies',
  handler: getPolicies
});

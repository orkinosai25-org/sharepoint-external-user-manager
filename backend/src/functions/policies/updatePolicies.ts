/**
 * PUT /policies - Update collaboration policies
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { auditLogger } from '../../services/auditLogger';
import { authenticateRequest } from '../../middleware/auth';
import { enforceSubscription, checkFeatureAccess } from '../../middleware/subscription';
import { validateBody, updatePolicySchema } from '../../utils/validation';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { UpdatePolicyRequest, PolicyResponse } from '../../models/policy';
import { ForbiddenError } from '../../models/common';
import { enforceFeatureAccess } from '../../services/plan-enforcement';

async function updatePolicies(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Parse and validate request body
    const body = await req.json() as any;
    const validatedRequest = validateBody<UpdatePolicyRequest>(body, updatePolicySchema);

    // Check if advanced policies feature is available for certain policy types
    const advancedPolicyTypes = ['RequireApproval', 'ReviewCampaigns'];
    if (advancedPolicyTypes.includes(validatedRequest.policyType)) {
      // Use new plan enforcement service
      enforceFeatureAccess(tenantContext, 'customPolicies');
    }

    // Update policy
    const policy = await databaseService.updatePolicy(
      tenantContext.tenantId,
      validatedRequest.policyType,
      validatedRequest.enabled,
      validatedRequest.configuration
    );

    // Log audit event
    await auditLogger.logSuccess(
      tenantContext,
      'PolicyUpdated',
      'Policy',
      policy.id.toString(),
      {
        policyType: policy.policyType,
        enabled: policy.enabled,
        configuration: policy.configuration
      },
      req.headers.get('x-forwarded-for') || 'unknown',
      correlationId
    );

    // Format response
    const response: PolicyResponse = {
      id: policy.id,
      policyType: policy.policyType,
      enabled: policy.enabled,
      configuration: policy.configuration,
      modifiedDate: policy.modifiedDate.toISOString()
    };

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('updatePolicies', {
  methods: ['PUT', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'policies',
  handler: updatePolicies
});

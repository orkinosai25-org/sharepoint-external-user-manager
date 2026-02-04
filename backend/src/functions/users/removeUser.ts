/**
 * DELETE /external-users - Remove external user access from client site
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { graphClient } from '../../services/graphClient';
import { auditLogger } from '../../services/auditLogger';
import { databaseService } from '../../services/database';
import { authenticateRequest } from '../../middleware/auth';
import { enforceSubscription } from '../../middleware/subscription';
import { requirePermission, Permissions } from '../../middleware/permissions';
import { validateBody, removeUserSchema } from '../../utils/validation';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { RemoveUserRequest } from '../../models/user';
import { ApiResponse } from '../../models/common';

async function removeUser(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
  const correlationId = attachCorrelationId(req);
  let requestBody: any = null;
  let email = 'unknown';

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

    // Check permissions - requires EXTERNAL_USERS_DELETE permission
    requirePermission(tenantContext, Permissions.EXTERNAL_USERS_DELETE, 'remove external users');

    // Parse and validate request body
    requestBody = await req.json() as any;
    const validatedRequest = validateBody<RemoveUserRequest>(requestBody, removeUserSchema);
    email = validatedRequest.email;

    // Remove external user access via Graph API
    await graphClient.removeExternalUser(
      validatedRequest.email,
      validatedRequest.library
    );

    // Log audit event for successful removal
    await auditLogger.logSuccess(
      tenantContext,
      'UserRemoved',
      'ExternalUser',
      email,
      {
        email: email,
        library: validatedRequest.library
      },
      req.headers.get('x-forwarded-for') || 'unknown',
      correlationId
    );

    // Format response
    const response: ApiResponse<{ message: string }> = {
      success: true,
      data: {
        message: `External user ${email} access removed from ${validatedRequest.library}`
      },
      meta: {
        correlationId,
        timestamp: new Date().toISOString()
      }
    };

    const successResponse = {
      status: 200,
      jsonBody: response,
      headers: {
        'Content-Type': 'application/json',
        'X-Correlation-ID': correlationId
      }
    };
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    // Log audit event for failed removal
    try {
      const tenantContext = await authenticateRequest(req, context);
      
      await auditLogger.logFailure(
        tenantContext,
        'UserRemoved',
        'ExternalUser',
        email,
        {
          email: email,
          library: requestBody?.library,
          error: error instanceof Error ? error.message : 'Unknown error'
        },
        req.headers.get('x-forwarded-for') || 'unknown',
        correlationId
      );
    } catch (auditError) {
      // Ignore audit errors in error handler
      console.error('Failed to log audit for failed removal:', auditError);
    }

    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('removeUser', {
  methods: ['DELETE', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'external-users',
  handler: removeUser
});

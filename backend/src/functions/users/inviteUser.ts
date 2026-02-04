/**
 * POST /external-users - Invite external user to client site
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { graphClient } from '../../services/graphClient';
import { auditLogger } from '../../services/auditLogger';
import { databaseService } from '../../services/database';
import { authenticateRequest } from '../../middleware/auth';
import { enforceSubscription } from '../../middleware/subscription';
import { requirePermission, Permissions } from '../../middleware/permissions';
import { validateBody, inviteUserSchema } from '../../utils/validation';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { InviteUserRequest, ExternalUserResponse } from '../../models/user';
import { ApiResponse } from '../../models/common';

async function inviteUser(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Check permissions - requires EXTERNAL_USERS_WRITE permission
    requirePermission(tenantContext, Permissions.EXTERNAL_USERS_WRITE, 'invite external users');

    // Parse and validate request body
    requestBody = await req.json() as any;
    const validatedRequest = validateBody<InviteUserRequest>(requestBody, inviteUserSchema);
    email = validatedRequest.email;

    // Invite external user via Graph API
    const invitedUser = await graphClient.inviteExternalUser(
      validatedRequest.email,
      validatedRequest.displayName,
      validatedRequest.library,
      validatedRequest.permissions,
      validatedRequest.message
    );

    // If metadata was provided, attach it to the user
    if (validatedRequest.metadata) {
      invitedUser.metadata = validatedRequest.metadata;
    }

    // Log audit event for successful invitation
    await auditLogger.logSuccess(
      tenantContext,
      'UserInvited',
      'ExternalUser',
      invitedUser.id,
      {
        email: invitedUser.email,
        displayName: invitedUser.displayName,
        library: invitedUser.library,
        permissions: invitedUser.permissions,
        metadata: invitedUser.metadata
      },
      req.headers.get('x-forwarded-for') || 'unknown',
      correlationId
    );

    // Format response
    const userResponse: ExternalUserResponse = {
      id: invitedUser.id,
      email: invitedUser.email,
      displayName: invitedUser.displayName,
      library: invitedUser.library,
      permissions: invitedUser.permissions,
      invitedBy: invitedUser.invitedBy,
      invitedDate: invitedUser.invitedDate.toISOString(),
      lastAccess: invitedUser.lastAccess?.toISOString() || null,
      status: invitedUser.status,
      metadata: invitedUser.metadata
    };

    const response: ApiResponse<ExternalUserResponse> = {
      success: true,
      data: userResponse,
      meta: {
        correlationId,
        timestamp: new Date().toISOString()
      }
    };

    const successResponse = {
      status: 201,
      jsonBody: response,
      headers: {
        'Content-Type': 'application/json',
        'X-Correlation-ID': correlationId
      }
    };
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    // Log audit event for failed invitation
    try {
      const tenantContext = await authenticateRequest(req, context);
      
      await auditLogger.logFailure(
        tenantContext,
        'UserInvited',
        'ExternalUser',
        email,
        {
          email: email,
          error: error instanceof Error ? error.message : 'Unknown error'
        },
        req.headers.get('x-forwarded-for') || 'unknown',
        correlationId
      );
    } catch (auditError) {
      // Ignore audit errors in error handler
      console.error('Failed to log audit for failed invitation:', auditError);
    }

    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('inviteUser', {
  methods: ['POST', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'external-users',
  handler: inviteUser
});

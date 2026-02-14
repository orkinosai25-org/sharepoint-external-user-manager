/**
 * POST /external-users - Invite external user to client site
 * 
 * REFACTORED VERSION using shared services layer
 * This demonstrates the new pattern for CS-SAAS-REF-01
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
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
import { enforceExternalUserLimit } from '../../services/plan-enforcement';

// NEW: Import shared services and adapters
import { ExternalUserService } from '../../../../services/core';
import { BackendGraphClient, BackendAuditService } from '../../adapters';

// NEW: Create service instances (these could be singletons)
const graphClient = new BackendGraphClient();
const auditService = new BackendAuditService();
const userService = new ExternalUserService(graphClient, auditService);

async function inviteUserRefactored(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Enforce external user limit based on plan
    const currentUserCount = await databaseService.getExternalUserCount(tenantContext.tenantId);
    enforceExternalUserLimit(tenantContext, currentUserCount);

    // Parse and validate request body
    requestBody = await req.json() as any;
    const validatedRequest = validateBody<InviteUserRequest>(requestBody, inviteUserSchema);
    email = validatedRequest.email;

    // NEW: Use shared ExternalUserService instead of direct graphClient call
    const invitationResult = await userService.inviteUser({
      email: validatedRequest.email,
      displayName: validatedRequest.displayName,
      resourceUrl: validatedRequest.library,
      permission: validatedRequest.permissions,
      message: validatedRequest.message,
      metadata: validatedRequest.metadata
    });

    // Check if invitation was successful
    if (!invitationResult.success || !invitationResult.user) {
      throw new Error(invitationResult.error || 'Failed to invite user');
    }

    const invitedUser = invitationResult.user;

    // Log audit event for successful invitation
    // Note: Audit logging is also done within the service, 
    // but we keep this for tenant-specific audit trail in database
    await databaseService.logAudit({
      tenantId: tenantContext.tenantId,
      userId: tenantContext.userId,
      action: 'UserInvited',
      resourceType: 'ExternalUser',
      resourceId: invitedUser.id,
      details: {
        email: invitedUser.email,
        displayName: invitedUser.displayName,
        library: invitedUser.libraryUrl,
        permissions: invitedUser.permissions,
        metadata: invitedUser.metadata
      },
      ipAddress: req.headers.get('x-forwarded-for') || 'unknown',
      correlationId,
      success: true
    });

    // Format response
    const userResponse: ExternalUserResponse = {
      id: invitedUser.id,
      email: invitedUser.email,
      displayName: invitedUser.displayName,
      library: invitedUser.libraryUrl,
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
      
      await databaseService.logAudit({
        tenantId: tenantContext.tenantId,
        userId: tenantContext.userId,
        action: 'UserInvited',
        resourceType: 'ExternalUser',
        resourceId: email,
        details: {
          email: email,
          error: error instanceof Error ? error.message : 'Unknown error'
        },
        ipAddress: req.headers.get('x-forwarded-for') || 'unknown',
        correlationId,
        success: false
      });
    } catch (auditError) {
      // Ignore audit errors in error handler
      console.error('Failed to log audit for failed invitation:', auditError);
    }

    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

// Note: This is a demonstration file showing the refactored pattern
// To use this, rename to inviteUser.ts and update the function name
// app.http('inviteUserRefactored', {
//   methods: ['POST', 'OPTIONS'],
//   authLevel: 'anonymous',
//   route: 'external-users',
//   handler: inviteUserRefactored
// });

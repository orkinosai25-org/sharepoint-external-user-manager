/**
 * GET /external-users - List external users with filtering
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { graphClient } from '../../services/graphClient';
import { auditLogger } from '../../services/auditLogger';
import { databaseService } from '../../services/database';
import { authenticateRequest } from '../../middleware/auth';
import { enforceSubscription } from '../../middleware/subscription';
import { validateQuery, listUsersQuerySchema } from '../../utils/validation';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { ListUsersRequest, ExternalUserResponse } from '../../models/user';
import { ApiResponse, PaginationInfo } from '../../models/common';

async function listUsers(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Parse and validate query parameters
    const queryParams: Record<string, string> = {};
    req.query.forEach((value: string, key: string) => {
      queryParams[key] = value;
    });

    const validatedQuery = validateQuery<ListUsersRequest>(queryParams, listUsersQuerySchema);

    // Get external users from Graph API
    const library = validatedQuery.library || 'https://contoso.sharepoint.com/sites/default/docs';
    const users = await graphClient.listExternalUsers(library);

    // Apply filters
    let filteredUsers = users;

    if (validatedQuery.status) {
      filteredUsers = filteredUsers.filter(u => u.status === validatedQuery.status);
    }

    if (validatedQuery.email) {
      filteredUsers = filteredUsers.filter(u => 
        u.email.toLowerCase().includes(validatedQuery.email!.toLowerCase())
      );
    }

    if (validatedQuery.company) {
      filteredUsers = filteredUsers.filter(u => 
        u.metadata?.company?.toLowerCase().includes(validatedQuery.company!.toLowerCase())
      );
    }

    if (validatedQuery.project) {
      filteredUsers = filteredUsers.filter(u => 
        u.metadata?.project?.toLowerCase().includes(validatedQuery.project!.toLowerCase())
      );
    }

    // Apply pagination
    const page = validatedQuery.page || 1;
    const pageSize = validatedQuery.pageSize || 50;
    const total = filteredUsers.length;
    const totalPages = Math.ceil(total / pageSize);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const paginatedUsers = filteredUsers.slice(startIndex, endIndex);

    // Format response
    const userResponses: ExternalUserResponse[] = paginatedUsers.map(user => ({
      id: user.id,
      email: user.email,
      displayName: user.displayName,
      library: user.library,
      permissions: user.permissions,
      invitedBy: user.invitedBy,
      invitedDate: user.invitedDate.toISOString(),
      lastAccess: user.lastAccess?.toISOString() || null,
      status: user.status,
      metadata: user.metadata
    }));

    const pagination: PaginationInfo = {
      page,
      pageSize,
      total,
      totalPages,
      hasNext: page < totalPages,
      hasPrev: page > 1
    };

    // Log audit event
    await auditLogger.logSuccess(
      tenantContext,
      'UserInvited', // Use generic action for list
      'ExternalUser',
      'list',
      { filters: validatedQuery, resultCount: paginatedUsers.length },
      req.headers.get('x-forwarded-for') || 'unknown',
      correlationId
    );

    const response: ApiResponse<ExternalUserResponse[]> = {
      success: true,
      data: userResponses,
      pagination,
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
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('listUsers', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'external-users',
  handler: listUsers
});

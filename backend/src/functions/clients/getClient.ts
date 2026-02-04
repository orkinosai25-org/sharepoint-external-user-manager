/**
 * GET /clients/:id - Get a specific client
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { authenticateRequest } from '../../middleware/auth';
import { requirePermission, Permissions } from '../../middleware/permissions';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { NotFoundError } from '../../models/common';
import { ClientResponse } from '../../models/client';

async function getClient(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Check permissions - both FirmAdmin and FirmUser can read clients
    requirePermission(tenantContext, Permissions.CLIENTS_READ, 'view clients');

    // Get client ID from route parameter
    const clientId = parseInt(req.params.id || '0');
    if (!clientId || isNaN(clientId)) {
      throw new NotFoundError('Client');
    }

    // Get client details (with tenant isolation)
    const client = await databaseService.getClientById(tenantContext.tenantId, clientId);
    if (!client) {
      throw new NotFoundError('Client');
    }

    // Build response
    const response: ClientResponse = {
      id: client.id,
      tenantId: client.tenantId,
      clientName: client.clientName,
      siteUrl: client.siteUrl,
      siteId: client.siteId,
      createdBy: client.createdBy,
      createdAt: client.createdAt.toISOString(),
      status: client.status
    };

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('getClient', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'clients/{id}',
  handler: getClient
});

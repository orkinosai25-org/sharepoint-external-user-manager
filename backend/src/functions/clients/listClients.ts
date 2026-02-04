/**
 * GET /clients - List all clients for the tenant
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { authenticateRequest } from '../../middleware/auth';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { ClientResponse } from '../../models/client';

async function listClients(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Get all clients for the tenant (ordered by created date)
    const clients = await databaseService.getClientsByTenantId(tenantContext.tenantId);

    // Build response
    const response: ClientResponse[] = clients.map(client => ({
      id: client.id,
      tenantId: client.tenantId,
      clientName: client.clientName,
      siteUrl: client.siteUrl,
      siteId: client.siteId,
      createdBy: client.createdBy,
      createdAt: client.createdAt.toISOString(),
      status: client.status
    }));

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('listClients', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'clients',
  handler: listClients
});

/**
 * GET /clients/:id/lists - Get lists for a specific client
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { sharePointService } from '../../services/sharePointService';
import { authenticateRequest } from '../../middleware/auth';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { NotFoundError } from '../../models/common';
import { ListResponse } from '../../models/client';

async function getClientLists(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Check if site is provisioned
    if (client.status !== 'Active' || !client.siteId) {
      throw new Error('Client site is not yet provisioned or is in error state');
    }

    // Get lists from SharePoint
    const lists = await sharePointService.getLists(client.siteId);

    // Transform to UI-friendly format
    const response: ListResponse[] = lists.map(list => ({
      id: list.id,
      name: list.name,
      displayName: list.displayName || list.name,
      description: list.description || '',
      webUrl: list.webUrl,
      createdDateTime: list.createdDateTime,
      lastModifiedDateTime: list.lastModifiedDateTime,
      itemCount: 0, // Would need additional API call to get item count
      listTemplate: list.list?.template || 'genericList'
    }));

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('getClientLists', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'clients/{id}/lists',
  handler: getClientLists
});

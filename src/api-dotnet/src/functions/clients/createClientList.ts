/**
 * POST /clients/:id/lists - Create a new list for a specific client
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { sharePointService } from '../../services/sharePointService';
import { authenticateRequest } from '../../middleware/auth';
import { requirePermission, Permissions } from '../../middleware/permissions';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { NotFoundError } from '../../models/common';
import { CreateListRequest, ListResponse } from '../../models/client';
import { validateBody, createListSchema } from '../../utils/validation';

async function createClientList(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Check permissions - require CLIENTS_WRITE to create lists
    requirePermission(tenantContext, Permissions.CLIENTS_WRITE, 'create client lists');

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

    // Parse and validate request body
    const requestBody = await req.json();
    const validatedRequest = validateBody<CreateListRequest>(requestBody, createListSchema);

    // Create list in SharePoint
    const createdList = await sharePointService.createList(
      client.siteId,
      validatedRequest.name,
      validatedRequest.description,
      validatedRequest.template
    );

    // Transform to UI-friendly format
    const response: ListResponse = {
      id: createdList.id,
      name: createdList.name,
      displayName: createdList.displayName || createdList.name,
      description: createdList.description || '',
      webUrl: createdList.webUrl,
      createdDateTime: createdList.createdDateTime,
      lastModifiedDateTime: createdList.lastModifiedDateTime || createdList.createdDateTime,
      itemCount: 0,
      listTemplate: createdList.list?.template || validatedRequest.template || 'genericList'
    };

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('createClientList', {
  methods: ['POST', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'clients/{id}/lists',
  handler: createClientList
});

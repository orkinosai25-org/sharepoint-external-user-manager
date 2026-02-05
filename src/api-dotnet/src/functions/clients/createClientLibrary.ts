/**
 * POST /clients/:id/libraries - Create a new document library for a specific client
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
import { CreateLibraryRequest, LibraryResponse } from '../../models/client';
import { validateBody, createLibrarySchema } from '../../utils/validation';

async function createClientLibrary(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Check permissions - require CLIENTS_WRITE to create libraries
    requirePermission(tenantContext, Permissions.CLIENTS_WRITE, 'create client libraries');

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
    const validatedRequest = validateBody<CreateLibraryRequest>(requestBody, createLibrarySchema);

    // Create library in SharePoint
    const createdLibrary = await sharePointService.createLibrary(
      client.siteId,
      validatedRequest.name,
      validatedRequest.description
    );

    // Transform to UI-friendly format
    const response: LibraryResponse = {
      id: createdLibrary.id,
      name: createdLibrary.name || createdLibrary.displayName,
      displayName: createdLibrary.displayName || createdLibrary.name,
      description: createdLibrary.description || '',
      webUrl: createdLibrary.webUrl,
      createdDateTime: createdLibrary.createdDateTime,
      lastModifiedDateTime: createdLibrary.lastModifiedDateTime || createdLibrary.createdDateTime,
      itemCount: 0
    };

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('createClientLibrary', {
  methods: ['POST', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'clients/{id}/libraries',
  handler: createClientLibrary
});

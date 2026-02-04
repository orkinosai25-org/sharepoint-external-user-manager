/**
 * GET /clients/:id/libraries - Get document libraries for a specific client
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { sharePointService } from '../../services/sharePointService';
import { authenticateRequest } from '../../middleware/auth';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { NotFoundError } from '../../models/common';
import { LibraryResponse } from '../../models/client';

async function getClientLibraries(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Get libraries from SharePoint
    const libraries = await sharePointService.getLibraries(client.siteId);

    // Transform to UI-friendly format
    const response: LibraryResponse[] = libraries.map(lib => ({
      id: lib.id,
      name: lib.name,
      displayName: lib.name,
      description: lib.description || '',
      webUrl: lib.webUrl,
      createdDateTime: lib.createdDateTime,
      lastModifiedDateTime: lib.lastModifiedDateTime,
      itemCount: 0 // Graph API drives endpoint doesn't include item count by default
    }));

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('getClientLibraries', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'clients/{id}/libraries',
  handler: getClientLibraries
});

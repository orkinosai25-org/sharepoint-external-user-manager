/**
 * POST /clients - Create a new client
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { auditLogger } from '../../services/auditLogger';
import { authenticateRequest } from '../../middleware/auth';
import { validateBody, createClientSchema } from '../../utils/validation';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { CreateClientRequest, ClientResponse } from '../../models/client';

async function createClient(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Parse and validate request body
    const body = await req.json() as any;
    const validatedRequest = validateBody<CreateClientRequest>(body, createClientSchema);

    // Create client
    const client = await databaseService.createClient({
      tenantId: tenantContext.tenantId,
      clientName: validatedRequest.clientName,
      siteUrl: validatedRequest.siteUrl,
      siteId: validatedRequest.siteId,
      createdBy: tenantContext.userEmail,
      status: 'Provisioning'
    });

    // Log audit event
    await auditLogger.logSuccess(
      tenantContext,
      'ClientCreated',
      'Client',
      client.id.toString(),
      {
        clientName: client.clientName,
        siteUrl: client.siteUrl
      },
      req.headers.get('x-forwarded-for') || 'unknown',
      correlationId
    );

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

    const successResponse = createSuccessResponse(response, correlationId, 201);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('createClient', {
  methods: ['POST', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'clients',
  handler: createClient
});

/**
 * POST /clients - Create a new client
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { auditLogger } from '../../services/auditLogger';
import { sharePointService } from '../../services/sharePointService';
import { authenticateRequest } from '../../middleware/auth';
import { requirePermission, Permissions } from '../../middleware/permissions';
import { validateBody, createClientSchema } from '../../utils/validation';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { CreateClientRequest, ClientResponse } from '../../models/client';
import { enforceClientSpaceLimit } from '../../services/plan-enforcement';

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

    // Check permissions - only FirmAdmin can create clients
    requirePermission(tenantContext, Permissions.CLIENTS_WRITE, 'create clients');

    // Enforce client space limit based on plan
    const currentClientCount = await databaseService.getClientCount(tenantContext.tenantId);
    enforceClientSpaceLimit(tenantContext, currentClientCount);

    // Parse and validate request body
    const body = await req.json() as any;
    const validatedRequest = validateBody<CreateClientRequest>(body, createClientSchema);

    // Create client record with initial "Provisioning" status
    // Note: siteUrl and siteId are empty and will be populated after async provisioning
    const client = await databaseService.createClient({
      tenantId: tenantContext.tenantId,
      clientName: validatedRequest.clientName,
      siteUrl: '',
      siteId: '',
      createdBy: tenantContext.userEmail,
      status: 'Provisioning'
    });

    // Log audit event for client creation
    await auditLogger.logSuccess(
      tenantContext,
      'ClientCreated',
      'Client',
      client.id.toString(),
      {
        clientName: client.clientName,
        siteTemplate: validatedRequest.siteTemplate || 'Team'
      },
      req.headers.get('x-forwarded-for') || 'unknown',
      correlationId
    );

    // Provision SharePoint site asynchronously
    // IMPORTANT: In production, this should use Azure Queue Storage, Service Bus, 
    // or Durable Functions for reliable async processing. Using setTimeout in 
    // Azure Functions is not reliable as the execution context may terminate.
    // This implementation is for MVP demonstration purposes only.
    setTimeout(async () => {
      try {
        const provisionResult = await sharePointService.provisionSite(
          validatedRequest.clientName,
          validatedRequest.siteTemplate || 'Team'
        );

        if (provisionResult.success) {
          // Update client with site information
          await databaseService.updateClientStatus(
            tenantContext.tenantId,
            client.id,
            'Active',
            provisionResult.siteUrl,
            provisionResult.siteId
          );

          // Log successful provisioning
          await auditLogger.logSuccess(
            tenantContext,
            'SiteProvisioned',
            'Client',
            client.id.toString(),
            {
              clientName: client.clientName,
              siteUrl: provisionResult.siteUrl,
              siteId: provisionResult.siteId
            },
            'system',
            correlationId
          );
        } else {
          // Update client with error status
          await databaseService.updateClientStatus(
            tenantContext.tenantId,
            client.id,
            'Error',
            undefined,
            undefined,
            provisionResult.error
          );

          // Log provisioning failure
          await auditLogger.logFailure(
            tenantContext,
            'SiteProvisioningFailed',
            'Client',
            client.id.toString(),
            {
              clientName: client.clientName,
              error: provisionResult.error
            },
            'system',
            correlationId
          );
        }
      } catch (error: any) {
        // Handle unexpected errors during provisioning
        const errorMessage = error.message || 'Unexpected error during provisioning';
        
        await databaseService.updateClientStatus(
          tenantContext.tenantId,
          client.id,
          'Error',
          undefined,
          undefined,
          errorMessage
        );

        await auditLogger.logFailure(
          tenantContext,
          'SiteProvisioningFailed',
          'Client',
          client.id.toString(),
          {
            clientName: client.clientName,
            error: errorMessage
          },
          'system',
          correlationId
        );
      }
    }, 0);

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

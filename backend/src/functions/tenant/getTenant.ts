/**
 * GET /tenants/me - Get current tenant information
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { authenticateRequest } from '../../middleware/auth';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { TenantResponse } from '../../models/tenant';

async function getTenant(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Get tenant details
    const tenant = await databaseService.getTenantById(tenantContext.tenantId);
    if (!tenant) {
      throw new Error('Tenant not found');
    }

    // Build response
    const response: TenantResponse = {
      tenantId: tenant.id,
      entraIdTenantId: tenant.entraIdTenantId,
      organizationName: tenant.organizationName,
      primaryAdminEmail: tenant.primaryAdminEmail,
      onboardedDate: tenant.onboardedDate.toISOString(),
      status: tenant.status,
      settings: tenant.settings
    };

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('getTenant', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'tenants/me',
  handler: getTenant
});

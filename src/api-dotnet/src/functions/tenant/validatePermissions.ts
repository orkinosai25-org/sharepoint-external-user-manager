/**
 * GET /auth/permissions - Validate Microsoft Graph permissions for tenant
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { authenticateRequest } from '../../middleware/auth';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { oauthService } from '../../services/oauth';
import { databaseService } from '../../services/database';
import { NotFoundError } from '../../models/common';

async function validatePermissions(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Get tenant auth record
    const tenantAuth = await databaseService.getTenantAuth(tenantContext.tenantId);
    if (!tenantAuth || !tenantAuth.accessToken) {
      throw new NotFoundError(
        'TenantAuth',
        'Tenant has not completed OAuth consent flow. Please connect your tenant first.'
      );
    }

    // Check if token is expired
    const isTokenExpired = tenantAuth.tokenExpiresAt && new Date(tenantAuth.tokenExpiresAt) < new Date();
    
    let accessToken = tenantAuth.accessToken;

    // Refresh token if expired
    if (isTokenExpired && tenantAuth.refreshToken) {
      const tenant = await databaseService.getTenantById(tenantContext.tenantId);
      if (!tenant) {
        throw new NotFoundError('Tenant', 'Tenant not found');
      }

      const tokenResponse = await oauthService.refreshAccessToken(
        tenantAuth.refreshToken,
        tenant.entraIdTenantId
      );

      // Update token in database
      await databaseService.refreshTenantToken(tenantContext.tenantId, tokenResponse);
      accessToken = tokenResponse.access_token;
    }

    // Validate permissions
    const permissionsValidation = await oauthService.validatePermissions(accessToken);

    const response = {
      ...permissionsValidation,
      tokenExpired: isTokenExpired,
      tokenRefreshed: isTokenExpired && tenantAuth.refreshToken ? true : false,
      consentGrantedAt: tenantAuth.consentGrantedAt,
      consentGrantedBy: tenantAuth.consentGrantedBy
    };

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('validatePermissions', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'auth/permissions',
  handler: validatePermissions
});

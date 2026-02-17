/**
 * POST /auth/connect - Initiate OAuth admin consent flow
 * Returns authorization URL for admin to grant consent
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { authenticateRequest } from '../../middleware/auth';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { oauthService } from '../../services/oauth';
import { ConnectTenantRequest, ConnectTenantResponse } from '../../models/tenant-auth';
import { randomBytes } from 'crypto';
import { databaseService } from '../../services/database';

async function connectTenant(req: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
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

    // Parse request body
    const body = await req.json() as ConnectTenantRequest;
    
    if (!body.redirectUri) {
      throw new Error('redirectUri is required');
    }

    // Generate secure state parameter for CSRF protection
    const state = randomBytes(32).toString('hex');
    
    // Store state in session/cache for validation (in production, use Redis or similar)
    // For now, we'll include tenant context in the state
    const stateData = {
      state,
      tenantId: tenantContext.tenantId,
      timestamp: Date.now()
    };
    
    // In production, store this in a cache with expiry
    // For now, we'll encode it in the state itself (not recommended for production)
    const encodedState = Buffer.from(JSON.stringify(stateData)).toString('base64');

    // Generate admin consent URL
    const authorizationUrl = oauthService.generateAdminConsentUrl(encodedState, body.redirectUri);

    const response: ConnectTenantResponse = {
      authorizationUrl,
      state: encodedState
    };

    const successResponse = createSuccessResponse(response, correlationId);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('connectTenant', {
  methods: ['POST', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'auth/connect',
  handler: connectTenant
});

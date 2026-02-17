/**
 * GET /auth/callback - Handle OAuth admin consent callback
 * Exchanges authorization code for tokens and stores them
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { oauthService } from '../../services/oauth';
import { databaseService } from '../../services/database';
import { auditLogger } from '../../services/auditLogger';
import { AdminConsentRequest } from '../../models/tenant-auth';
import { UnauthorizedError, BadRequestError } from '../../models/common';

async function authCallback(req: HttpRequest, _context: InvocationContext): Promise<HttpResponseInit> {
  const correlationId = attachCorrelationId(req);

  try {
    // Handle CORS preflight
    const corsResponse = handleCorsPreFlight(req);
    if (corsResponse) {
      return corsResponse;
    }

    // Ensure database is connected
    await databaseService.connect();

    // Parse query parameters
    const code = req.query.get('code');
    const state = req.query.get('state');
    const adminConsent = req.query.get('admin_consent');
    const tenantIdParam = req.query.get('tenant');
    const error = req.query.get('error');
    const errorDescription = req.query.get('error_description');

    // Check for errors from OAuth provider
    if (error) {
      throw new BadRequestError(
        `OAuth consent failed: ${error}`,
        errorDescription || 'Admin consent was not granted'
      );
    }

    // Validate required parameters
    if (!code || !state || !tenantIdParam) {
      throw new BadRequestError(
        'Missing required parameters',
        'code, state, and tenant are required'
      );
    }

    if (adminConsent !== 'True') {
      throw new UnauthorizedError('Admin consent was not granted');
    }

    // Decode and validate state
    let stateData: any;
    try {
      stateData = JSON.parse(Buffer.from(state, 'base64').toString());
    } catch (e) {
      throw new BadRequestError('Invalid state parameter');
    }

    // Validate state timestamp (should be recent, e.g., within 10 minutes)
    const stateAge = Date.now() - stateData.timestamp;
    if (stateAge > 10 * 60 * 1000) {
      throw new UnauthorizedError('State parameter has expired');
    }

    // Get tenant from database
    const tenant = await databaseService.getTenantById(stateData.tenantId);
    if (!tenant) {
      throw new BadRequestError('Tenant not found');
    }

    // Get redirect URI from query or use default
    const redirectUri = req.query.get('redirect_uri');
    
    // Validate redirect URI against whitelist
    const allowedRedirectUris = (config.cors.allowedOrigins || []).map(origin => 
      `${origin}/onboarding/consent`
    );
    
    if (!redirectUri || !allowedRedirectUris.includes(redirectUri)) {
      throw new BadRequestError(
        'Invalid redirect URI',
        'Redirect URI must be from an allowed origin'
      );
    }

    // Exchange authorization code for tokens
    const tokenResponse = await oauthService.exchangeAuthorizationCode(
      code,
      redirectUri,
      tenantIdParam
    );

    // Calculate token expiry
    const tokenExpiresAt = new Date(Date.now() + tokenResponse.expires_in * 1000);

    // TODO: Get actual consenting user from token claims or Graph API
    // The tenant.primaryAdminEmail may not be the actual user who granted consent
    // Consider extracting from JWT claims or querying Graph API with the access token
    const consentGrantedBy = tenant.primaryAdminEmail;

    // Save tokens to database
    await databaseService.saveTenantAuth({
      tenantId: tenant.id,
      accessToken: tokenResponse.access_token,
      refreshToken: tokenResponse.refresh_token,
      tokenExpiresAt,
      scope: tokenResponse.scope,
      consentGrantedBy,
      consentGrantedAt: new Date(),
      lastTokenRefresh: new Date()
    });

    // Validate that required permissions were granted
    const permissionsValidation = await oauthService.validatePermissions(tokenResponse.access_token);

    // Log audit event
    const tenantContext = {
      tenantId: tenant.id,
      entraIdTenantId: tenant.entraIdTenantId,
      userId: 'system',
      userEmail: tenant.primaryAdminEmail,
      roles: ['Owner'] as any[],
      subscriptionTier: 'Free' as any
    };

    await auditLogger.logSuccess(
      tenantContext,
      'TenantConsentGranted',
      'Tenant',
      tenant.id.toString(),
      {
        hasRequiredPermissions: permissionsValidation.hasRequiredPermissions,
        grantedPermissions: permissionsValidation.grantedPermissions,
        missingPermissions: permissionsValidation.missingPermissions
      },
      req.headers.get('x-forwarded-for') || 'unknown',
      correlationId
    );

    // Return success response with redirect
    const response = {
      success: true,
      message: 'Admin consent granted successfully',
      redirectUrl: redirectUri,
      permissions: permissionsValidation
    };

    const successResponse = createSuccessResponse(response, correlationId);
    
    // Add redirect header
    successResponse.headers = {
      ...successResponse.headers,
      'Location': redirectUri
    };
    successResponse.status = 302;

    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('authCallback', {
  methods: ['GET', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'auth/callback',
  handler: authCallback
});

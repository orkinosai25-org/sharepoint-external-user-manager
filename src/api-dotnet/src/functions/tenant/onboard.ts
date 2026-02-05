/**
 * POST /tenants/onboard - Onboard a new tenant
 */

import { app, HttpRequest, HttpResponseInit, InvocationContext } from '@azure/functions';
import { databaseService } from '../../services/database';
import { auditLogger } from '../../services/auditLogger';
import { validateBody, tenantOnboardSchema } from '../../utils/validation';
import { attachCorrelationId } from '../../utils/correlation';
import { handleError, createSuccessResponse } from '../../middleware/errorHandler';
import { handleCorsPreFlight, applyCorsHeaders } from '../../middleware/cors';
import { ConflictError, UnauthorizedError } from '../../models/common';
import { OnboardTenantRequest, TenantResponse } from '../../models/tenant';
import { TIER_LIMITS, USER_LIMITS } from '../../models/subscription';
import { verify } from 'jsonwebtoken';
import jwksClient from 'jwks-rsa';
import { config } from '../../utils/config';

const client = jwksClient({
  jwksUri: `https://login.microsoftonline.com/common/discovery/v2.0/keys`,
  cache: true,
  cacheMaxAge: 86400000
});

function getKey(header: any, callback: (err: any, key?: string) => void): void {
  client.getSigningKey(header.kid, (err, key) => {
    if (err) {
      callback(err);
      return;
    }
    const signingKey = key?.getPublicKey();
    callback(null, signingKey);
  });
}

async function validateToken(token: string): Promise<any> {
  return new Promise((resolve, reject) => {
    verify(token, getKey, { audience: config.auth.audience, algorithms: ['RS256'] }, (err: any, decoded: any) => {
      if (err) reject(err);
      else resolve(decoded);
    });
  });
}

async function onboardTenant(req: HttpRequest, _context: InvocationContext): Promise<HttpResponseInit> {
  const correlationId = attachCorrelationId(req);

  try {
    // Handle CORS preflight
    const corsResponse = handleCorsPreFlight(req);
    if (corsResponse) {
      return corsResponse;
    }

    // Ensure database is connected
    await databaseService.connect();

    // Extract and validate token
    const authHeader = req.headers.get('authorization');
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      throw new UnauthorizedError('Missing or invalid Authorization header');
    }

    const token = authHeader.substring(7);
    const claims = await validateToken(token);

    const entraIdTenantId = claims.tid;
    const userId = claims.oid;
    const userEmail = claims.email || claims.upn || 'unknown';

    // Check if tenant already exists
    const existingTenant = await databaseService.getTenantByEntraId(entraIdTenantId);
    if (existingTenant) {
      throw new ConflictError('Tenant already onboarded', `Tenant ${entraIdTenantId} is already registered`);
    }

    // Parse and validate request body
    const body = await req.json() as any;
    const validatedRequest = validateBody<OnboardTenantRequest>(body, tenantOnboardSchema);

    // Create tenant
    const tenant = await databaseService.createTenant({
      entraIdTenantId,
      organizationName: validatedRequest.organizationName,
      primaryAdminEmail: validatedRequest.primaryAdminEmail,
      onboardedDate: new Date(),
      status: 'Active',
      settings: validatedRequest.settings || {}
    });

    // Create default subscription (Free tier with 30-day trial)
    const trialExpiry = new Date();
    trialExpiry.setDate(trialExpiry.getDate() + 30);

    const subscription = await databaseService.createSubscription({
      tenantId: tenant.id,
      tier: 'Free',
      startDate: new Date(),
      endDate: null,
      trialExpiry,
      gracePeriodEnd: null,
      status: 'Trial',
      maxUsers: USER_LIMITS.Free,
      features: TIER_LIMITS.Free
    });

    // Log audit event
    const tenantContext = {
      tenantId: tenant.id,
      entraIdTenantId: tenant.entraIdTenantId,
      userId,
      userEmail,
      roles: ['Owner'], // User who onboards is the owner
      subscriptionTier: subscription.tier
    };

    await auditLogger.logSuccess(
      tenantContext,
      'TenantOnboarded',
      'Tenant',
      tenant.id.toString(),
      {
        organizationName: tenant.organizationName,
        subscriptionTier: subscription.tier
      },
      req.headers.get('x-forwarded-for') || 'unknown',
      correlationId
    );

    // Build response
    const response: TenantResponse & { subscription: any } = {
      tenantId: tenant.id,
      entraIdTenantId: tenant.entraIdTenantId,
      organizationName: tenant.organizationName,
      primaryAdminEmail: tenant.primaryAdminEmail,
      onboardedDate: tenant.onboardedDate.toISOString(),
      status: tenant.status,
      settings: tenant.settings,
      subscription: {
        tier: subscription.tier,
        status: subscription.status,
        trialExpiry: subscription.trialExpiry?.toISOString(),
        maxUsers: subscription.maxUsers
      }
    };

    const successResponse = createSuccessResponse(response, correlationId, 201);
    return applyCorsHeaders(successResponse, req);
  } catch (error) {
    const errorResponse = handleError(error, correlationId);
    return applyCorsHeaders(errorResponse, req);
  }
}

app.http('onboardTenant', {
  methods: ['POST', 'OPTIONS'],
  authLevel: 'anonymous',
  route: 'tenants/onboard',
  handler: onboardTenant
});

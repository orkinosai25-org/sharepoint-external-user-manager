/**
 * Authentication middleware
 */

import { HttpRequest, InvocationContext } from '@azure/functions';
import { verify } from 'jsonwebtoken';
import jwksClient from 'jwks-rsa';
import { config } from '../utils/config';
import { TenantContext, TokenClaims, UnauthorizedError, NotFoundError } from '../models/common';
import { databaseService } from '../services/database';

const client = jwksClient({
  jwksUri: `https://login.microsoftonline.com/common/discovery/v2.0/keys`,
  cache: true,
  cacheMaxAge: 86400000 // 24 hours
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

async function validateToken(token: string): Promise<TokenClaims> {
  return new Promise((resolve, reject) => {
    verify(
      token,
      getKey,
      {
        audience: config.auth.audience,
        algorithms: ['RS256']
      },
      (err, decoded) => {
        if (err) {
          reject(new UnauthorizedError('Invalid token', err.message));
          return;
        }
        resolve(decoded as TokenClaims);
      }
    );
  });
}

export async function authenticateRequest(
  req: HttpRequest,
  _context: InvocationContext
): Promise<TenantContext> {
  try {
    // Extract bearer token
    const authHeader = req.headers.get('authorization');
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      throw new UnauthorizedError('Missing or invalid Authorization header');
    }

    const token = authHeader.substring(7);

    // Validate JWT token
    const claims = await validateToken(token);

    // Extract user information
    const userId = claims.oid;
    const userEmail = claims.email || claims.upn || 'unknown';
    const entraIdTenantId = claims.tid;

    if (!userId || !entraIdTenantId) {
      throw new UnauthorizedError('Invalid token claims');
    }

    // Resolve tenant from database
    const tenant = await databaseService.getTenantByEntraId(entraIdTenantId);
    if (!tenant) {
      throw new NotFoundError('Tenant', `Tenant not onboarded for Entra ID: ${entraIdTenantId}`);
    }

    // Check tenant status
    if (tenant.status !== 'Active') {
      throw new UnauthorizedError('Tenant is not active', `Tenant status: ${tenant.status}`);
    }

    // Get subscription
    const subscription = await databaseService.getSubscriptionByTenantId(tenant.id);
    if (!subscription) {
      throw new UnauthorizedError('No active subscription found');
    }

    // Build tenant context
    const tenantContext: TenantContext = {
      tenantId: tenant.id,
      entraIdTenantId: tenant.entraIdTenantId,
      userId,
      userEmail,
      subscriptionTier: subscription.tier
    };

    // Attach to request for downstream use
    (req as any).tenantContext = tenantContext;

    return tenantContext;
  } catch (error) {
    if (error instanceof UnauthorizedError || error instanceof NotFoundError) {
      throw error;
    }
    throw new UnauthorizedError('Authentication failed', error instanceof Error ? error.message : 'Unknown error');
  }
}

export function getTenantContext(req: HttpRequest): TenantContext {
  const context = (req as any).tenantContext;
  if (!context) {
    throw new UnauthorizedError('Request not authenticated');
  }
  return context;
}

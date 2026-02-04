import { HttpRequest, InvocationContext } from '@azure/functions';
import { verify, decode, JwtPayload } from 'jsonwebtoken';
import * as jwksClient from 'jwks-rsa';
import { TokenPayload, ApiResponse, ErrorCode } from '../models/types';

const client = jwksClient({
  jwksUri: 'https://login.microsoftonline.com/common/discovery/v2.0/keys',
  cache: true,
  cacheMaxAge: 86400000 // 24 hours
});

/**
 * Get signing key from JWKS
 */
function getKey(header: jwksClient.SigningKey, callback: jwksClient.SigningKeyCallback): void {
  client.getSigningKey(header.kid, (err, key) => {
    if (err) {
      callback(err);
      return;
    }
    const signingKey = key?.getPublicKey();
    callback(null, signingKey);
  });
}

/**
 * Validate JWT token from Authorization header
 */
export async function validateToken(request: HttpRequest, context: InvocationContext): Promise<TokenPayload | null> {
  try {
    const authHeader = request.headers.get('authorization');
    
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      context.warn('Missing or invalid Authorization header');
      return null;
    }

    const token = authHeader.substring(7); // Remove 'Bearer ' prefix
    
    // Decode token to get header
    const decoded = decode(token, { complete: true });
    if (!decoded || typeof decoded === 'string') {
      context.warn('Invalid token format');
      return null;
    }

    // Verify token
    return new Promise((resolve, reject) => {
      verify(
        token,
        getKey,
        {
          audience: process.env.AZURE_AD_CLIENT_ID,
          issuer: [
            'https://sts.windows.net/',
            'https://login.microsoftonline.com/'
          ].map(iss => `${iss}${decoded.payload.tid}/`),
          algorithms: ['RS256']
        },
        (err, verifiedToken) => {
          if (err) {
            context.error('Token verification failed', err);
            reject(err);
            return;
          }
          resolve(verifiedToken as TokenPayload);
        }
      );
    });
  } catch (error) {
    context.error('Error validating token', error);
    return null;
  }
}

/**
 * Extract tenant ID from request
 */
export function getTenantId(request: HttpRequest, tokenPayload?: TokenPayload): string | null {
  // First try X-Tenant-ID header
  const headerTenantId = request.headers.get('x-tenant-id');
  if (headerTenantId) {
    return headerTenantId;
  }

  // Fall back to token tenant ID
  if (tokenPayload?.tid) {
    return tokenPayload.tid;
  }

  return null;
}

/**
 * Check if user has required role
 */
export function hasRole(tokenPayload: TokenPayload, requiredRole: string): boolean {
  return tokenPayload.roles?.includes(requiredRole) ?? false;
}

/**
 * Create unauthorized response
 */
export function createUnauthorizedResponse(correlationId: string): ApiResponse<never> {
  return {
    success: false,
    error: {
      code: ErrorCode.UNAUTHORIZED,
      message: 'Authentication required',
      details: 'Missing or invalid authentication token',
      correlationId,
      timestamp: new Date().toISOString()
    }
  };
}

/**
 * Create forbidden response
 */
export function createForbiddenResponse(correlationId: string, message?: string): ApiResponse<never> {
  return {
    success: false,
    error: {
      code: ErrorCode.FORBIDDEN,
      message: message || 'Insufficient permissions',
      details: 'You do not have permission to perform this action',
      correlationId,
      timestamp: new Date().toISOString()
    }
  };
}

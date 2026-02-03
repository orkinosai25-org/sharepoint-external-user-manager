import { Request, Response, NextFunction } from 'express';
import jwt from 'jsonwebtoken';
import jwksClient from 'jwks-rsa';
import { config } from '../config';

export interface TokenPayload {
  sub: string;
  tid: string;
  oid: string;
  email?: string;
  roles?: string[];
  iss: string;
  aud: string;
  exp: number;
}

export interface AuthenticatedRequest extends Request {
  user?: {
    id: string;
    email: string;
    tenantId: string;
    roles: string[];
  };
}

const client = jwksClient({
  jwksUri: `https://login.microsoftonline.com/common/discovery/v2.0/keys`,
  cache: true,
  cacheMaxAge: 86400000 // 24 hours
});

function getKey(header: jwt.JwtHeader, callback: jwt.SigningKeyCallback): void {
  client.getSigningKey(header.kid, (err, key) => {
    if (err) {
      callback(err);
    } else {
      const signingKey = key?.getPublicKey();
      callback(null, signingKey);
    }
  });
}

async function validateToken(token: string): Promise<TokenPayload> {
  return new Promise((resolve, reject) => {
    jwt.verify(
      token,
      getKey,
      {
        audience: config.azureAd.clientId,
        issuer: `https://login.microsoftonline.com/${config.azureAd.tenantId}/v2.0`,
        algorithms: ['RS256']
      },
      (err, decoded) => {
        if (err) {
          reject(new Error('Invalid token'));
        } else {
          resolve(decoded as TokenPayload);
        }
      }
    );
  });
}

export async function authMiddleware(
  req: AuthenticatedRequest,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    const authHeader = req.headers.authorization;
    
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      res.status(401).json({
        success: false,
        error: {
          code: 'UNAUTHORIZED',
          message: 'Missing or invalid authorization header'
        }
      });
      return;
    }

    const token = authHeader.substring(7);
    const payload = await validateToken(token);
    
    req.user = {
      id: payload.oid,
      email: payload.email || '',
      tenantId: payload.tid,
      roles: payload.roles || []
    };
    
    next();
  } catch (error) {
    res.status(401).json({
      success: false,
      error: {
        code: 'UNAUTHORIZED',
        message: 'Authentication failed',
        details: error instanceof Error ? error.message : 'Unknown error'
      }
    });
  }
}

export function requireRole(...allowedRoles: string[]) {
  return (req: AuthenticatedRequest, res: Response, next: NextFunction): void => {
    const userRoles = req.user?.roles || [];
    
    const hasRole = allowedRoles.some(role => userRoles.includes(role));
    
    if (!hasRole) {
      res.status(403).json({
        success: false,
        error: {
          code: 'FORBIDDEN',
          message: 'Insufficient permissions',
          details: `Required roles: ${allowedRoles.join(', ')}`
        }
      });
      return;
    }
    
    next();
  };
}

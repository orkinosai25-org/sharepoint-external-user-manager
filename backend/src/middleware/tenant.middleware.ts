import { Response, NextFunction } from 'express';
import { AuthenticatedRequest } from './auth.middleware';

export interface TenantInfo {
  tenant_id: string;
  tenant_name: string;
  status: string;
  subscription_tier: string;
}

export interface TenantRequest extends AuthenticatedRequest {
  tenant?: TenantInfo;
}

// Mock database query - replace with actual database implementation
async function getTenantById(tenantId: string): Promise<TenantInfo | null> {
  // This will be replaced with actual database query
  // For now, return a mock tenant for development
  if (process.env.NODE_ENV === 'development') {
    return {
      tenant_id: tenantId,
      tenant_name: 'Development Tenant',
      status: 'Active',
      subscription_tier: 'Pro'
    };
  }
  return null;
}

async function setSessionContext(tenantId: string): Promise<void> {
  // This will be replaced with actual database session context
  // For SQL Server: EXEC sp_set_session_context 'tenant_id', @tenantId
  console.log(`Setting session context for tenant: ${tenantId}`);
}

export async function tenantMiddleware(
  req: TenantRequest,
  res: Response,
  next: NextFunction
): Promise<void> {
  try {
    // Extract tenant ID from header or token
    const tenantId = (req.headers['x-tenant-id'] as string) || req.user?.tenantId;
    
    if (!tenantId) {
      res.status(400).json({
        success: false,
        error: {
          code: 'MISSING_TENANT_ID',
          message: 'Tenant ID is required'
        }
      });
      return;
    }
    
    // Verify tenant exists and is active
    const tenant = await getTenantById(tenantId);
    
    if (!tenant) {
      res.status(404).json({
        success: false,
        error: {
          code: 'TENANT_NOT_FOUND',
          message: 'Tenant not found'
        }
      });
      return;
    }
    
    if (tenant.status !== 'Active' && tenant.status !== 'Trial') {
      res.status(403).json({
        success: false,
        error: {
          code: 'TENANT_NOT_ACTIVE',
          message: `Tenant is ${tenant.status.toLowerCase()}`
        }
      });
      return;
    }
    
    // Verify user belongs to this tenant
    if (req.user?.tenantId && req.user.tenantId !== tenantId) {
      res.status(403).json({
        success: false,
        error: {
          code: 'FORBIDDEN',
          message: 'Access denied to this tenant'
        }
      });
      return;
    }
    
    // Set session context for row-level security
    await setSessionContext(tenantId);
    
    // Attach tenant to request
    req.tenant = tenant;
    
    next();
  } catch (error) {
    next(error);
  }
}

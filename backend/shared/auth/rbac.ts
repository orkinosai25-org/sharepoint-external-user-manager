import { TenantContext, UserRole } from '../models/types';

/**
 * Check if user has required permission
 */
export function hasPermission(
  context: TenantContext,
  permission: string
): boolean {
  const rolePermissions = getRolePermissions();
  
  for (const role of context.roles) {
    const permissions = rolePermissions[role as UserRole];
    if (permissions?.includes(permission)) {
      return true;
    }
  }
  
  return false;
}

/**
 * Get permissions for each role
 */
function getRolePermissions(): Record<UserRole, string[]> {
  return {
    [UserRole.TenantOwner]: [
      'tenants:read',
      'tenants:write',
      'tenants:delete',
      'subscription:read',
      'subscription:write',
      'admins:manage',
      'libraries:read',
      'libraries:write',
      'libraries:delete',
      'users:read',
      'users:write',
      'users:delete',
      'policies:read',
      'policies:write',
      'audit:read',
      'audit:export'
    ],
    [UserRole.TenantAdmin]: [
      'tenants:read',
      'tenants:write',
      'subscription:read',
      'libraries:read',
      'libraries:write',
      'libraries:delete',
      'users:read',
      'users:write',
      'users:delete',
      'policies:read',
      'policies:write',
      'audit:read',
      'audit:export'
    ],
    [UserRole.LibraryOwner]: [
      'libraries:read',
      'libraries:write',
      'users:read',
      'users:write',
      'users:delete'
    ],
    [UserRole.LibraryContributor]: [
      'libraries:read',
      'users:read',
      'users:write'
    ],
    [UserRole.LibraryReader]: [
      'libraries:read',
      'users:read'
    ]
  };
}

/**
 * Check if user is tenant admin or owner
 */
export function isTenantAdmin(context: TenantContext): boolean {
  return context.roles.includes(UserRole.TenantOwner) ||
         context.roles.includes(UserRole.TenantAdmin);
}

/**
 * Check if user is tenant owner
 */
export function isTenantOwner(context: TenantContext): boolean {
  return context.roles.includes(UserRole.TenantOwner);
}

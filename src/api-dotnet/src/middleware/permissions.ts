/**
 * Permission middleware for role-based access control
 */

import { TenantContext, UserRole, ForbiddenError } from '../models/common';

/**
 * Permission definitions for different operations
 */
export const Permissions = {
  CLIENTS_READ: 'clients:read',
  CLIENTS_WRITE: 'clients:write',
  CLIENTS_DELETE: 'clients:delete',
  EXTERNAL_USERS_READ: 'external-users:read',
  EXTERNAL_USERS_WRITE: 'external-users:write',
  EXTERNAL_USERS_DELETE: 'external-users:delete',
} as const;

export type Permission = typeof Permissions[keyof typeof Permissions];

/**
 * Role to permission mapping
 */
const rolePermissions: Record<UserRole, Permission[]> = {
  Owner: [
    Permissions.CLIENTS_READ,
    Permissions.CLIENTS_WRITE,
    Permissions.CLIENTS_DELETE,
    Permissions.EXTERNAL_USERS_READ,
    Permissions.EXTERNAL_USERS_WRITE,
    Permissions.EXTERNAL_USERS_DELETE,
  ],
  Admin: [
    Permissions.CLIENTS_READ,
    Permissions.CLIENTS_WRITE,
    Permissions.CLIENTS_DELETE,
    Permissions.EXTERNAL_USERS_READ,
    Permissions.EXTERNAL_USERS_WRITE,
    Permissions.EXTERNAL_USERS_DELETE,
  ],
  FirmAdmin: [
    Permissions.CLIENTS_READ,
    Permissions.CLIENTS_WRITE,
    Permissions.CLIENTS_DELETE,
    Permissions.EXTERNAL_USERS_READ,
    Permissions.EXTERNAL_USERS_WRITE,
    Permissions.EXTERNAL_USERS_DELETE,
  ],
  FirmUser: [
    Permissions.CLIENTS_READ,
    Permissions.EXTERNAL_USERS_READ,
  ],
  User: [
    Permissions.CLIENTS_READ,
    Permissions.EXTERNAL_USERS_READ,
  ],
  ReadOnly: [
    Permissions.CLIENTS_READ,
    Permissions.EXTERNAL_USERS_READ,
  ],
};

/**
 * Check if a user has a specific permission
 */
export function hasPermission(
  context: TenantContext,
  permission: Permission
): boolean {
  for (const role of context.roles) {
    const permissions = rolePermissions[role];
    if (permissions && permissions.includes(permission)) {
      return true;
    }
  }
  return false;
}

/**
 * Check if a user has a specific role
 */
export function hasRole(
  context: TenantContext,
  role: UserRole
): boolean {
  return context.roles.includes(role);
}

/**
 * Check if a user has any of the specified roles
 */
export function hasAnyRole(
  context: TenantContext,
  roles: UserRole[]
): boolean {
  return roles.some(role => context.roles.includes(role));
}

/**
 * Require a specific permission, throw ForbiddenError if not met
 */
export function requirePermission(
  context: TenantContext,
  permission: Permission,
  action?: string
): void {
  if (!hasPermission(context, permission)) {
    const actionDescription = action || permission;
    throw new ForbiddenError(
      `You do not have permission to ${actionDescription}`,
      `Required permission: ${permission}. Your roles: ${context.roles.join(', ')}`
    );
  }
}

/**
 * Require a specific role, throw ForbiddenError if not met
 */
export function requireRole(
  context: TenantContext,
  role: UserRole,
  action?: string
): void {
  if (!hasRole(context, role)) {
    const actionDescription = action || `perform this action`;
    throw new ForbiddenError(
      `You do not have permission to ${actionDescription}`,
      `Required role: ${role}. Your roles: ${context.roles.join(', ')}`
    );
  }
}

/**
 * Require any of the specified roles, throw ForbiddenError if not met
 */
export function requireAnyRole(
  context: TenantContext,
  roles: UserRole[],
  action?: string
): void {
  if (!hasAnyRole(context, roles)) {
    const actionDescription = action || `perform this action`;
    throw new ForbiddenError(
      `You do not have permission to ${actionDescription}`,
      `Required one of: ${roles.join(', ')}. Your roles: ${context.roles.join(', ')}`
    );
  }
}

/**
 * Tests for permission middleware
 */

import { 
  hasPermission, 
  hasRole, 
  hasAnyRole, 
  requirePermission, 
  requireRole,
  requireAnyRole,
  Permissions 
} from '../permissions';
import { TenantContext, ForbiddenError } from '../../models/common';

describe('Permission Middleware', () => {
  const createContext = (roles: string[]): TenantContext => ({
    tenantId: 1,
    entraIdTenantId: 'test-tenant-id',
    userId: 'user-123',
    userEmail: 'test@example.com',
    roles: roles as any[],
    subscriptionTier: 'Pro'
  });

  describe('hasPermission', () => {
    it('should return true for FirmAdmin with CLIENTS_WRITE permission', () => {
      const context = createContext(['FirmAdmin']);
      expect(hasPermission(context, Permissions.CLIENTS_WRITE)).toBe(true);
    });

    it('should return false for FirmUser with CLIENTS_WRITE permission', () => {
      const context = createContext(['FirmUser']);
      expect(hasPermission(context, Permissions.CLIENTS_WRITE)).toBe(false);
    });

    it('should return true for FirmUser with CLIENTS_READ permission', () => {
      const context = createContext(['FirmUser']);
      expect(hasPermission(context, Permissions.CLIENTS_READ)).toBe(true);
    });

    it('should return true for Owner with all permissions', () => {
      const context = createContext(['Owner']);
      expect(hasPermission(context, Permissions.CLIENTS_READ)).toBe(true);
      expect(hasPermission(context, Permissions.CLIENTS_WRITE)).toBe(true);
      expect(hasPermission(context, Permissions.CLIENTS_DELETE)).toBe(true);
    });

    it('should return true for user with multiple roles when any role has permission', () => {
      const context = createContext(['FirmUser', 'FirmAdmin']);
      expect(hasPermission(context, Permissions.CLIENTS_WRITE)).toBe(true);
    });
  });

  describe('hasRole', () => {
    it('should return true when user has the specified role', () => {
      const context = createContext(['FirmAdmin']);
      expect(hasRole(context, 'FirmAdmin')).toBe(true);
    });

    it('should return false when user does not have the specified role', () => {
      const context = createContext(['FirmUser']);
      expect(hasRole(context, 'FirmAdmin')).toBe(false);
    });

    it('should return true when user has multiple roles including the specified one', () => {
      const context = createContext(['FirmUser', 'FirmAdmin']);
      expect(hasRole(context, 'FirmAdmin')).toBe(true);
    });
  });

  describe('hasAnyRole', () => {
    it('should return true when user has any of the specified roles', () => {
      const context = createContext(['FirmUser']);
      expect(hasAnyRole(context, ['FirmAdmin', 'FirmUser'])).toBe(true);
    });

    it('should return false when user has none of the specified roles', () => {
      const context = createContext(['ReadOnly']);
      expect(hasAnyRole(context, ['FirmAdmin', 'Owner'])).toBe(false);
    });

    it('should return true when user has multiple roles', () => {
      const context = createContext(['FirmUser', 'Admin']);
      expect(hasAnyRole(context, ['FirmAdmin', 'Admin'])).toBe(true);
    });
  });

  describe('requirePermission', () => {
    it('should not throw error when user has required permission', () => {
      const context = createContext(['FirmAdmin']);
      expect(() => {
        requirePermission(context, Permissions.CLIENTS_WRITE, 'create clients');
      }).not.toThrow();
    });

    it('should throw ForbiddenError when user lacks required permission', () => {
      const context = createContext(['FirmUser']);
      expect(() => {
        requirePermission(context, Permissions.CLIENTS_WRITE, 'create clients');
      }).toThrow(ForbiddenError);
    });

    it('should include helpful error message', () => {
      const context = createContext(['FirmUser']);
      try {
        requirePermission(context, Permissions.CLIENTS_WRITE, 'create clients');
        fail('Should have thrown ForbiddenError');
      } catch (error: any) {
        expect(error).toBeInstanceOf(ForbiddenError);
        expect(error.message).toContain('create clients');
        expect(error.details).toContain('clients:write');
        expect(error.details).toContain('FirmUser');
      }
    });
  });

  describe('requireRole', () => {
    it('should not throw error when user has required role', () => {
      const context = createContext(['FirmAdmin']);
      expect(() => {
        requireRole(context, 'FirmAdmin', 'perform admin action');
      }).not.toThrow();
    });

    it('should throw ForbiddenError when user lacks required role', () => {
      const context = createContext(['FirmUser']);
      expect(() => {
        requireRole(context, 'FirmAdmin', 'perform admin action');
      }).toThrow(ForbiddenError);
    });

    it('should include helpful error message with role information', () => {
      const context = createContext(['FirmUser']);
      try {
        requireRole(context, 'FirmAdmin', 'perform admin action');
        fail('Should have thrown ForbiddenError');
      } catch (error: any) {
        expect(error).toBeInstanceOf(ForbiddenError);
        expect(error.message).toContain('perform admin action');
        expect(error.details).toContain('FirmAdmin');
        expect(error.details).toContain('FirmUser');
      }
    });
  });

  describe('requireAnyRole', () => {
    it('should not throw error when user has any of the required roles', () => {
      const context = createContext(['FirmUser']);
      expect(() => {
        requireAnyRole(context, ['FirmAdmin', 'FirmUser'], 'access resource');
      }).not.toThrow();
    });

    it('should throw ForbiddenError when user has none of the required roles', () => {
      const context = createContext(['ReadOnly']);
      expect(() => {
        requireAnyRole(context, ['FirmAdmin', 'FirmUser'], 'access resource');
      }).toThrow(ForbiddenError);
    });

    it('should include all required roles in error message', () => {
      const context = createContext(['ReadOnly']);
      try {
        requireAnyRole(context, ['FirmAdmin', 'Owner'], 'access resource');
        fail('Should have thrown ForbiddenError');
      } catch (error: any) {
        expect(error).toBeInstanceOf(ForbiddenError);
        expect(error.details).toContain('FirmAdmin');
        expect(error.details).toContain('Owner');
        expect(error.details).toContain('ReadOnly');
      }
    });
  });

  describe('Role-based scenarios', () => {
    it('FirmAdmin should be able to create, read, and delete clients', () => {
      const context = createContext(['FirmAdmin']);
      expect(hasPermission(context, Permissions.CLIENTS_READ)).toBe(true);
      expect(hasPermission(context, Permissions.CLIENTS_WRITE)).toBe(true);
      expect(hasPermission(context, Permissions.CLIENTS_DELETE)).toBe(true);
    });

    it('FirmUser should only be able to read clients', () => {
      const context = createContext(['FirmUser']);
      expect(hasPermission(context, Permissions.CLIENTS_READ)).toBe(true);
      expect(hasPermission(context, Permissions.CLIENTS_WRITE)).toBe(false);
      expect(hasPermission(context, Permissions.CLIENTS_DELETE)).toBe(false);
    });

    it('Owner should have all permissions', () => {
      const context = createContext(['Owner']);
      expect(hasPermission(context, Permissions.CLIENTS_READ)).toBe(true);
      expect(hasPermission(context, Permissions.CLIENTS_WRITE)).toBe(true);
      expect(hasPermission(context, Permissions.CLIENTS_DELETE)).toBe(true);
    });

    it('ReadOnly should only be able to read clients', () => {
      const context = createContext(['ReadOnly']);
      expect(hasPermission(context, Permissions.CLIENTS_READ)).toBe(true);
      expect(hasPermission(context, Permissions.CLIENTS_WRITE)).toBe(false);
      expect(hasPermission(context, Permissions.CLIENTS_DELETE)).toBe(false);
    });
  });
});

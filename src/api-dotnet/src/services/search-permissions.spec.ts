/**
 * Unit tests for search permissions service
 */

import { TenantContext } from '../models/common';
import { SearchScope } from '../models/search';
import {
  getSearchPermissions,
  canPerformSearch,
  canUseFullTextSearch,
  canUseAdvancedFilters,
  filterAccessibleClients,
  validateSearchScope,
  getAllowedSearchScope,
  shouldShowSearchUpgradePrompt
} from './search-permissions';

describe('Search Permissions Service', () => {
  describe('getSearchPermissions', () => {
    it('should allow current client search for Starter tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Free'
      };

      const permissions = getSearchPermissions(context, [1, 2, 3]);

      expect(permissions.canSearchCurrentClient).toBe(true);
      expect(permissions.canSearchAllClients).toBe(false);
      expect(permissions.accessibleClientIds).toEqual([1, 2, 3]);
      expect(permissions.deniedReason).toBeDefined();
      expect(permissions.requiredTier).toBe('Professional');
    });

    it('should allow global search for Professional tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Pro'
      };

      const permissions = getSearchPermissions(context, [1, 2, 3]);

      expect(permissions.canSearchCurrentClient).toBe(true);
      expect(permissions.canSearchAllClients).toBe(true);
      expect(permissions.accessibleClientIds).toEqual([1, 2, 3]);
      expect(permissions.deniedReason).toBeUndefined();
      expect(permissions.requiredTier).toBeUndefined();
    });

    it('should allow global search for Enterprise tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Enterprise'
      };

      const permissions = getSearchPermissions(context, [1, 2, 3, 4, 5]);

      expect(permissions.canSearchCurrentClient).toBe(true);
      expect(permissions.canSearchAllClients).toBe(true);
      expect(permissions.accessibleClientIds).toEqual([1, 2, 3, 4, 5]);
      expect(permissions.deniedReason).toBeUndefined();
    });
  });

  describe('canPerformSearch', () => {
    it('should always allow CurrentClient scope', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Free'
      };

      const result = canPerformSearch(context, SearchScope.CurrentClient);

      expect(result.allowed).toBe(true);
      expect(result.reason).toBeUndefined();
    });

    it('should deny AllClients scope for Starter tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Free'
      };

      const result = canPerformSearch(context, SearchScope.AllClients);

      expect(result.allowed).toBe(false);
      expect(result.reason).toBeDefined();
      expect(result.requiredTier).toBe('Professional');
    });

    it('should allow AllClients scope for Pro tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Pro'
      };

      const result = canPerformSearch(context, SearchScope.AllClients);

      expect(result.allowed).toBe(true);
    });
  });

  describe('canUseFullTextSearch', () => {
    it('should deny full-text search for Starter tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Free'
      };

      expect(canUseFullTextSearch(context)).toBe(false);
    });

    it('should allow full-text search for Pro tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Pro'
      };

      expect(canUseFullTextSearch(context)).toBe(true);
    });
  });

  describe('canUseAdvancedFilters', () => {
    it('should deny advanced filters for Starter tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Free'
      };

      expect(canUseAdvancedFilters(context)).toBe(false);
    });

    it('should allow advanced filters for Pro tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Pro'
      };

      expect(canUseAdvancedFilters(context)).toBe(true);
    });
  });

  describe('filterAccessibleClients', () => {
    it('should return all accessible clients when no specific clients requested', () => {
      const accessibleClientIds = [1, 2, 3, 4, 5];
      const result = filterAccessibleClients(undefined, accessibleClientIds);
      expect(result).toEqual([1, 2, 3, 4, 5]);
    });

    it('should return all accessible clients when empty array requested', () => {
      const accessibleClientIds = [1, 2, 3, 4, 5];
      const result = filterAccessibleClients([], accessibleClientIds);
      expect(result).toEqual([1, 2, 3, 4, 5]);
    });

    it('should return intersection of requested and accessible clients', () => {
      const requestedClientIds = [2, 3, 6, 7];
      const accessibleClientIds = [1, 2, 3, 4, 5];
      const result = filterAccessibleClients(requestedClientIds, accessibleClientIds);
      expect(result).toEqual([2, 3]);
    });

    it('should return empty array when no intersection', () => {
      const requestedClientIds = [6, 7, 8];
      const accessibleClientIds = [1, 2, 3, 4, 5];
      const result = filterAccessibleClients(requestedClientIds, accessibleClientIds);
      expect(result).toEqual([]);
    });
  });

  describe('validateSearchScope', () => {
    it('should require clientId for CurrentClient scope', () => {
      const result = validateSearchScope(SearchScope.CurrentClient, undefined, [1, 2, 3]);
      expect(result.valid).toBe(false);
      expect(result.error).toContain('Client ID is required');
    });

    it('should require user access to specified client', () => {
      const result = validateSearchScope(SearchScope.CurrentClient, 5, [1, 2, 3]);
      expect(result.valid).toBe(false);
      expect(result.error).toContain('does not have access');
    });

    it('should validate successfully when user has access', () => {
      const result = validateSearchScope(SearchScope.CurrentClient, 2, [1, 2, 3]);
      expect(result.valid).toBe(true);
      expect(result.error).toBeUndefined();
    });

    it('should validate AllClients scope without clientId', () => {
      const result = validateSearchScope(SearchScope.AllClients, undefined, [1, 2, 3]);
      expect(result.valid).toBe(true);
      expect(result.error).toBeUndefined();
    });
  });

  describe('getAllowedSearchScope', () => {
    it('should return CurrentClient for Starter tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Free'
      };

      expect(getAllowedSearchScope(context)).toBe(SearchScope.CurrentClient);
    });

    it('should return AllClients for Pro tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Pro'
      };

      expect(getAllowedSearchScope(context)).toBe(SearchScope.AllClients);
    });

    it('should return AllClients for Enterprise tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Enterprise'
      };

      expect(getAllowedSearchScope(context)).toBe(SearchScope.AllClients);
    });
  });

  describe('shouldShowSearchUpgradePrompt', () => {
    it('should show upgrade prompt for Starter tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Free'
      };

      const result = shouldShowSearchUpgradePrompt(context);

      expect(result.showPrompt).toBe(true);
      expect(result.feature).toContain('Global search');
      expect(result.requiredTier).toBe('Professional');
    });

    it('should not show upgrade prompt for Pro tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Pro'
      };

      const result = shouldShowSearchUpgradePrompt(context);

      expect(result.showPrompt).toBe(false);
      expect(result.feature).toBeUndefined();
    });

    it('should not show upgrade prompt for Enterprise tier', () => {
      const context: TenantContext = {
        tenantId: 1,
        entraIdTenantId: 'test-tenant',
        userId: 'user1',
        userEmail: 'user@test.com',
        roles: ['User'],
        subscriptionTier: 'Enterprise'
      };

      const result = shouldShowSearchUpgradePrompt(context);

      expect(result.showPrompt).toBe(false);
    });
  });
});

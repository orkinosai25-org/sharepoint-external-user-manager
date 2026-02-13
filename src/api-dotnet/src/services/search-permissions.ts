/**
 * Search permissions service
 * Determines what search capabilities a user has based on their plan and access
 */

import { TenantContext } from '../models/common';
import { SearchPermissions, SearchScope } from '../models/search';
import {
  checkGlobalSearchAccess,
  checkFullTextSearchAccess,
  checkAdvancedSearchFiltersAccess,
  getTenantPlan
} from './plan-enforcement';

/**
 * Get search permissions for a user
 */
export function getSearchPermissions(
  context: TenantContext,
  accessibleClientIds: number[]
): SearchPermissions {
  const globalSearchResult = checkGlobalSearchAccess(context);
  const planTier = getTenantPlan(context);

  return {
    canSearchCurrentClient: true, // All users can search within current client
    canSearchAllClients: globalSearchResult.allowed,
    accessibleClientIds,
    deniedReason: globalSearchResult.allowed ? undefined : globalSearchResult.reason,
    requiredTier: globalSearchResult.allowed ? undefined : (globalSearchResult.requiredTier || 'Professional')
  };
}

/**
 * Check if user can perform a search with the given scope
 */
export function canPerformSearch(
  context: TenantContext,
  scope: SearchScope
): { allowed: boolean; reason?: string; requiredTier?: string } {
  // Current client search is always allowed
  if (scope === SearchScope.CurrentClient) {
    return { allowed: true };
  }

  // Global search requires Pro tier or higher
  const result = checkGlobalSearchAccess(context);
  return result;
}

/**
 * Check if user can use full-text search
 */
export function canUseFullTextSearch(context: TenantContext): boolean {
  const result = checkFullTextSearchAccess(context);
  return result.allowed;
}

/**
 * Check if user can use advanced search filters
 */
export function canUseAdvancedFilters(context: TenantContext): boolean {
  const result = checkAdvancedSearchFiltersAccess(context);
  return result.allowed;
}

/**
 * Filter client IDs based on user permissions
 * Ensures user can only search within client spaces they have access to
 */
export function filterAccessibleClients(
  requestedClientIds: number[] | undefined,
  accessibleClientIds: number[]
): number[] {
  // If no specific clients requested, return all accessible
  if (!requestedClientIds || requestedClientIds.length === 0) {
    return accessibleClientIds;
  }

  // Return only the intersection of requested and accessible
  return requestedClientIds.filter(id => accessibleClientIds.includes(id));
}

/**
 * Validate search scope and client ID combination
 */
export function validateSearchScope(
  scope: SearchScope,
  clientId: number | undefined,
  accessibleClientIds: number[]
): { valid: boolean; error?: string } {
  // CurrentClient scope requires a client ID
  if (scope === SearchScope.CurrentClient) {
    if (!clientId) {
      return {
        valid: false,
        error: 'Client ID is required when search scope is CurrentClient'
      };
    }

    // Check if user has access to the specified client
    if (!accessibleClientIds.includes(clientId)) {
      return {
        valid: false,
        error: 'User does not have access to the specified client'
      };
    }
  }

  return { valid: true };
}

/**
 * Get allowed search scope for a user
 */
export function getAllowedSearchScope(context: TenantContext): SearchScope {
  const globalSearchResult = checkGlobalSearchAccess(context);
  
  // If user has global search access, default to all clients
  if (globalSearchResult.allowed) {
    return SearchScope.AllClients;
  }

  // Otherwise, default to current client only
  return SearchScope.CurrentClient;
}

/**
 * Check if user should see upgrade prompt for search features
 */
export function shouldShowSearchUpgradePrompt(context: TenantContext): {
  showPrompt: boolean;
  feature?: string;
  requiredTier?: string;
} {
  const planTier = getTenantPlan(context);
  
  // If user is on Starter, show prompt for global search
  if (planTier === 'Starter') {
    return {
      showPrompt: true,
      feature: 'Global search across all client spaces',
      requiredTier: 'Professional'
    };
  }

  return { showPrompt: false };
}

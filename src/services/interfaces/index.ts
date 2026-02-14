/**
 * Service interfaces for the shared services layer
 * These define the contracts that all implementations must follow
 */

import {
  ExternalUser,
  InvitationRequest,
  InvitationResult,
  BulkOperationResult,
  PermissionLevel,
  SharePointLibrary,
  SharePointSite,
  LibraryCreationRequest,
  PermissionCheckResult
} from '../models';

/**
 * External User Management Service Interface
 * 
 * Handles all operations related to external users in SharePoint
 */
export interface IExternalUserService {
  /**
   * List all external users for a given SharePoint resource
   * @param resourceUrl - SharePoint site or library URL
   * @returns Array of external users
   */
  listExternalUsers(resourceUrl: string): Promise<ExternalUser[]>;
  
  /**
   * Invite an external user to a SharePoint resource
   * @param request - Invitation request details
   * @returns Invitation result
   */
  inviteUser(request: InvitationRequest): Promise<InvitationResult>;
  
  /**
   * Remove an external user's access to a SharePoint resource
   * @param resourceUrl - SharePoint site or library URL
   * @param userId - User identifier
   */
  removeUser(resourceUrl: string, userId: string): Promise<void>;
  
  /**
   * Update an external user's permissions
   * @param resourceUrl - SharePoint site or library URL
   * @param userId - User identifier
   * @param newPermission - New permission level
   */
  updateUserPermission(
    resourceUrl: string,
    userId: string,
    newPermission: PermissionLevel
  ): Promise<void>;
  
  /**
   * Update user metadata (company, project, etc.)
   * @param resourceUrl - SharePoint site or library URL
   * @param userId - User identifier
   * @param metadata - Metadata to update
   */
  updateUserMetadata(
    resourceUrl: string,
    userId: string,
    metadata: Record<string, any>
  ): Promise<void>;
  
  /**
   * Bulk invite multiple users
   * @param requests - Array of invitation requests
   * @returns Bulk operation result
   */
  bulkInviteUsers(
    requests: InvitationRequest[]
  ): Promise<BulkOperationResult<InvitationRequest>>;
}

/**
 * SharePoint Library Management Service Interface
 * 
 * Handles operations related to SharePoint document libraries
 */
export interface ILibraryService {
  /**
   * List all document libraries in a site
   * @param siteId - SharePoint site identifier
   * @returns Array of libraries
   */
  listLibraries(siteId: string): Promise<SharePointLibrary[]>;
  
  /**
   * Get a specific library by ID
   * @param siteId - SharePoint site identifier
   * @param libraryId - Library identifier
   * @returns Library details
   */
  getLibrary(siteId: string, libraryId: string): Promise<SharePointLibrary>;
  
  /**
   * Create a new document library
   * @param request - Library creation request
   * @returns Created library
   */
  createLibrary(request: LibraryCreationRequest): Promise<SharePointLibrary>;
  
  /**
   * Delete a document library
   * @param siteId - SharePoint site identifier
   * @param libraryId - Library identifier
   */
  deleteLibrary(siteId: string, libraryId: string): Promise<void>;
  
  /**
   * Update library settings
   * @param siteId - SharePoint site identifier
   * @param libraryId - Library identifier
   * @param settings - Settings to update
   */
  updateLibrary(
    siteId: string,
    libraryId: string,
    settings: Partial<SharePointLibrary>
  ): Promise<SharePointLibrary>;
}

/**
 * SharePoint Site Provisioning Service Interface
 * 
 * Handles creation and management of SharePoint sites
 */
export interface ISiteProvisioningService {
  /**
   * Provision a new SharePoint site
   * @param name - Site name
   * @param description - Site description
   * @param template - Site template type
   * @returns Provisioned site details
   */
  provisionSite(
    name: string,
    description: string,
    template: 'Team' | 'Communication'
  ): Promise<SharePointSite>;
  
  /**
   * Get site details
   * @param siteId - Site identifier
   * @returns Site details
   */
  getSite(siteId: string): Promise<SharePointSite>;
  
  /**
   * List all sites
   * @returns Array of sites
   */
  listSites(): Promise<SharePointSite[]>;
  
  /**
   * Delete a site
   * @param siteId - Site identifier
   */
  deleteSite(siteId: string): Promise<void>;
  
  /**
   * Enable external sharing for a site
   * @param siteId - Site identifier
   */
  enableExternalSharing(siteId: string): Promise<void>;
  
  /**
   * Disable external sharing for a site
   * @param siteId - Site identifier
   */
  disableExternalSharing(siteId: string): Promise<void>;
}

/**
 * Permission Management Service Interface
 * 
 * Handles SharePoint permission operations
 */
export interface IPermissionService {
  /**
   * Grant permission to a user
   * @param resourceUrl - SharePoint resource URL
   * @param userEmail - User email address
   * @param permission - Permission level to grant
   */
  grantPermission(
    resourceUrl: string,
    userEmail: string,
    permission: PermissionLevel
  ): Promise<void>;
  
  /**
   * Revoke permission from a user
   * @param resourceUrl - SharePoint resource URL
   * @param userId - User identifier
   */
  revokePermission(resourceUrl: string, userId: string): Promise<void>;
  
  /**
   * Check if user has a specific permission
   * @param resourceUrl - SharePoint resource URL
   * @param userEmail - User email address
   * @param requiredPermission - Required permission level
   * @returns Permission check result
   */
  checkPermission(
    resourceUrl: string,
    userEmail: string,
    requiredPermission: PermissionLevel
  ): Promise<PermissionCheckResult>;
  
  /**
   * List all permissions for a resource
   * @param resourceUrl - SharePoint resource URL
   * @returns Array of user permissions
   */
  listPermissions(resourceUrl: string): Promise<Array<{
    userId: string;
    email: string;
    displayName: string;
    permission: PermissionLevel;
  }>>;
}

/**
 * Graph API Client Interface
 * 
 * Abstract interface for Microsoft Graph API operations
 * Implementations can use different authentication methods
 */
export interface IGraphClient {
  /**
   * Get authenticated access token
   * @returns Access token for Graph API
   */
  getAccessToken(): Promise<string>;
  
  /**
   * Make a Graph API request
   * @param endpoint - API endpoint (e.g., '/sites/{id}')
   * @param method - HTTP method
   * @param body - Request body
   * @returns Response data
   */
  request<T>(
    endpoint: string,
    method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE',
    body?: any
  ): Promise<T>;
}

/**
 * Audit Logging Service Interface
 * 
 * Handles audit logging for compliance and monitoring
 */
export interface IAuditService {
  /**
   * Log an informational message
   * @param operation - Operation name
   * @param message - Log message
   * @param data - Additional data
   */
  logInfo(operation: string, message: string, data?: any): void;
  
  /**
   * Log a warning message
   * @param operation - Operation name
   * @param message - Log message
   * @param data - Additional data
   */
  logWarning(operation: string, message: string, data?: any): void;
  
  /**
   * Log an error
   * @param operation - Operation name
   * @param message - Error message
   * @param error - Error object or data
   */
  logError(operation: string, message: string, error: any): void;
  
  /**
   * Generate a unique session ID for tracking related operations
   * @returns Session ID
   */
  generateSessionId(): string;
}

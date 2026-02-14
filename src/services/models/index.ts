/**
 * Common models and types used across the shared services layer
 * These are framework-agnostic and can be used by SPFx, Blazor, and API
 */

/**
 * Permission levels for SharePoint resources
 */
export type PermissionLevel = 'Read' | 'Contribute' | 'Edit' | 'FullControl';

/**
 * External user status
 */
export type ExternalUserStatus = 'Active' | 'Invited' | 'Inactive' | 'Blocked';

/**
 * Site template types for SharePoint
 */
export type SiteTemplate = 'Team' | 'Communication' | 'Custom';

/**
 * External user model
 */
export interface ExternalUser {
  /** Unique identifier */
  id: string;
  
  /** User email address */
  email: string;
  
  /** Display name */
  displayName: string;
  
  /** SharePoint library/site URL */
  libraryUrl: string;
  
  /** Permission level */
  permissions: PermissionLevel;
  
  /** Who invited this user */
  invitedBy: string;
  
  /** When the user was invited */
  invitedDate: Date;
  
  /** Last access date (nullable) */
  lastAccess: Date | null;
  
  /** User status */
  status: ExternalUserStatus;
  
  /** Additional metadata */
  metadata?: {
    company?: string;
    project?: string;
    [key: string]: any;
  };
}

/**
 * SharePoint library model
 */
export interface SharePointLibrary {
  /** Unique library identifier */
  id: string;
  
  /** Library name */
  name: string;
  
  /** Library description */
  description: string;
  
  /** SharePoint URL */
  url: string;
  
  /** Site ID this library belongs to */
  siteId: string;
  
  /** Number of external users with access */
  externalUserCount: number;
  
  /** Last modified date */
  lastModified: Date;
  
  /** Library owner */
  owner: string;
  
  /** Total item count */
  itemCount?: number;
}

/**
 * SharePoint site model
 */
export interface SharePointSite {
  /** Unique site identifier */
  id: string;
  
  /** Site name */
  name: string;
  
  /** Site description */
  description: string;
  
  /** Site URL */
  url: string;
  
  /** Site template type */
  template: SiteTemplate;
  
  /** Site created date */
  createdDate: Date;
  
  /** Site owner */
  owner: string;
  
  /** External sharing enabled */
  externalSharingEnabled: boolean;
}

/**
 * Invitation request model
 */
export interface InvitationRequest {
  /** User email to invite */
  email: string;
  
  /** Display name for the user */
  displayName?: string;
  
  /** SharePoint library/site URL */
  resourceUrl: string;
  
  /** Permission level to grant */
  permission: PermissionLevel;
  
  /** Custom invitation message */
  message?: string;
  
  /** Additional metadata */
  metadata?: {
    company?: string;
    project?: string;
    [key: string]: any;
  };
}

/**
 * Invitation result model
 */
export interface InvitationResult {
  /** Whether invitation was successful */
  success: boolean;
  
  /** User ID (if successful) */
  userId?: string;
  
  /** Error message (if failed) */
  error?: string;
  
  /** External user object (if successful) */
  user?: ExternalUser;
}

/**
 * Bulk operation result
 */
export interface BulkOperationResult<T> {
  /** Total operations requested */
  total: number;
  
  /** Number of successful operations */
  successCount: number;
  
  /** Number of failed operations */
  failedCount: number;
  
  /** Individual results */
  results: Array<{
    item: T;
    success: boolean;
    error?: string;
  }>;
}

/**
 * Library creation request
 */
export interface LibraryCreationRequest {
  /** Library name */
  name: string;
  
  /** Library description */
  description?: string;
  
  /** Site ID where library should be created */
  siteId: string;
  
  /** Enable external sharing */
  enableExternalSharing?: boolean;
  
  /** Library template (optional) */
  template?: string;
}

/**
 * Permission check result
 */
export interface PermissionCheckResult {
  /** Whether user has the requested permission */
  hasPermission: boolean;
  
  /** Actual permission level */
  actualPermission: PermissionLevel;
  
  /** User is site owner */
  isOwner: boolean;
}

/**
 * Service operation result
 */
export interface ServiceResult<T> {
  /** Whether operation was successful */
  success: boolean;
  
  /** Result data */
  data?: T;
  
  /** Error message if failed */
  error?: string;
  
  /** Error code if failed */
  errorCode?: string;
}

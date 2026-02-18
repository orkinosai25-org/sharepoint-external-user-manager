/**
 * External user model and interfaces
 * 
 * Represents external users (guests) who are granted access to SharePoint
 * libraries within a tenant's client spaces. These users are outside the
 * organization's Entra ID tenant.
 */

export type UserStatus = 'Active' | 'Invited' | 'Expired' | 'Removed';

export type PermissionLevel = 'Read' | 'Contribute' | 'Edit' | 'FullControl';

/**
 * Core external user model
 * Represents a guest user with access to SharePoint resources
 */
export interface ExternalUser {
  /** Unique user identifier */
  id: string;
  /** User email address (used for invitations) */
  email: string;
  /** User display name */
  displayName: string;
  /** Library/document library the user has access to */
  library: string;
  /** Permission level granted to the user */
  permissions: PermissionLevel;
  /** Email of the user who invited this external user */
  invitedBy: string;
  /** Date when the invitation was sent */
  invitedDate: Date;
  /** Date of last access (null if never accessed) */
  lastAccess?: Date | null;
  /** Current invitation/access status */
  status: UserStatus;
  /** Additional metadata about the user */
  metadata?: UserMetadata;
}

/**
 * Optional metadata for external users
 * Used to store additional contextual information
 */
export interface UserMetadata {
  /** Company/organization name */
  company?: string;
  /** Project or matter reference */
  project?: string;
  /** Department within the company */
  department?: string;
  /** Additional notes or context */
  notes?: string;
  /** Custom fields */
  [key: string]: any;
}

/**
 * Request parameters for listing external users
 */
export interface ListUsersRequest {
  /** Filter by library name */
  library?: string;
  /** Filter by user status */
  status?: UserStatus;
  /** Filter by email address (partial match) */
  email?: string;
  /** Filter by company name */
  company?: string;
  /** Filter by project reference */
  project?: string;
  /** Page number (1-based) */
  page?: number;
  /** Number of results per page */
  pageSize?: number;
}

/**
 * Request payload for inviting an external user
 */
export interface InviteUserRequest {
  /** User email address */
  email: string;
  /** User display name */
  displayName: string;
  /** Library to grant access to */
  library: string;
  /** Permission level to grant */
  permissions: PermissionLevel;
  /** Optional invitation message */
  message?: string;
  /** Optional user metadata */
  metadata?: UserMetadata;
}

/**
 * Request payload for removing an external user
 */
export interface RemoveUserRequest {
  /** User email address */
  email: string;
  /** Library to remove access from */
  library: string;
}

/**
 * Response format for external user data
 */
export interface ExternalUserResponse {
  /** User identifier */
  id: string;
  /** Email address */
  email: string;
  /** Display name */
  displayName: string;
  /** Library name */
  library: string;
  /** Permission level */
  permissions: PermissionLevel;
  /** Invited by email */
  invitedBy: string;
  /** Invitation date (ISO 8601 string) */
  invitedDate: string;
  /** Last access date (ISO 8601 string or null) */
  lastAccess: string | null;
  /** Current status */
  status: UserStatus;
  /** Optional metadata */
  metadata?: UserMetadata;
}

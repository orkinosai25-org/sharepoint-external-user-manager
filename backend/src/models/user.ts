/**
 * External user model and interfaces
 */

export type UserStatus = 'Active' | 'Invited' | 'Expired' | 'Removed';

export type PermissionLevel = 'Read' | 'Contribute' | 'Edit' | 'FullControl';

export interface ExternalUser {
  id: string;
  email: string;
  displayName: string;
  library: string;
  permissions: PermissionLevel;
  invitedBy: string;
  invitedDate: Date;
  lastAccess?: Date | null;
  status: UserStatus;
  metadata?: UserMetadata;
}

export interface UserMetadata {
  company?: string;
  project?: string;
  department?: string;
  notes?: string;
  [key: string]: any;
}

export interface ListUsersRequest {
  library?: string;
  status?: UserStatus;
  email?: string;
  company?: string;
  project?: string;
  page?: number;
  pageSize?: number;
}

export interface InviteUserRequest {
  email: string;
  displayName: string;
  library: string;
  permissions: PermissionLevel;
  message?: string;
  metadata?: UserMetadata;
}

export interface RemoveUserRequest {
  email: string;
  library: string;
}

export interface ExternalUserResponse {
  id: string;
  email: string;
  displayName: string;
  library: string;
  permissions: PermissionLevel;
  invitedBy: string;
  invitedDate: string;
  lastAccess: string | null;
  status: UserStatus;
  metadata?: UserMetadata;
}

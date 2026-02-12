/**
 * Extended client detail interfaces including libraries, lists, and external users
 */

import { IClient } from './IClient';

/**
 * Document Library interface
 */
export interface ILibrary {
  id: string;
  name: string;
  displayName: string;
  description: string;
  webUrl: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  itemCount: number;
}

/**
 * SharePoint List interface
 */
export interface IList {
  id: string;
  name: string;
  displayName: string;
  description: string;
  webUrl: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  itemCount: number;
  listTemplate: string;
}

/**
 * External User interface
 */
export type ExternalUserPermission = 'Read' | 'Contribute' | 'Edit' | 'FullControl';
export type ExternalUserStatus = 'Active' | 'Invited' | 'Expired' | 'Removed';

export interface IExternalUser {
  id: string;
  email: string;
  displayName: string;
  library: string;
  permissions: ExternalUserPermission;
  invitedBy: string;
  invitedDate: string;
  lastAccess: string | null;
  status: ExternalUserStatus;
  metadata?: {
    company?: string;
    project?: string;
    department?: string;
    notes?: string;
    [key: string]: string | undefined;
  };
}

/**
 * Extended client details with all associated data
 */
export interface IClientDetail extends IClient {
  libraries?: ILibrary[];
  lists?: IList[];
  externalUsers?: IExternalUser[];
}

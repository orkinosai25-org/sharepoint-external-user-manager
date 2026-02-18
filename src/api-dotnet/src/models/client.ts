/**
 * Client model and interfaces
 * Represents a solicitor's customer mapped 1:1 to a SharePoint site
 * 
 * Also known as ClientSpace - a dedicated workspace for a specific client
 * with isolated document libraries, lists, and external user access.
 */

import { SiteInfo } from './site';

export type ClientStatus = 'Provisioning' | 'Active' | 'Error';
export type SiteTemplateType = 'Team' | 'Communication';

/**
 * Core client/client space model
 * Each client represents a dedicated SharePoint site with tenant isolation
 */
export interface Client {
  /** Unique client identifier */
  id: number;
  /** Tenant ID for multi-tenant isolation (required) */
  tenantId: number;
  /** Client display name */
  clientName: string;
  /** SharePoint site URL */
  siteUrl: string;
  /** SharePoint site ID */
  siteId: string;
  /** User who created the client space */
  createdBy: string;
  /** Creation timestamp */
  createdAt: Date;
  /** Current provisioning/operational status */
  status: ClientStatus;
  /** Error message if status is Error */
  errorMessage?: string;
}

/**
 * Type alias for clarity - Client and ClientSpace refer to the same concept
 */
export type ClientSpace = Client;

/**
 * Extended client information with site details
 */
export interface ClientWithSiteInfo extends Client {
  siteInfo?: SiteInfo;
}

export interface CreateClientRequest {
  clientName: string;
  siteTemplate?: SiteTemplateType;
}

export interface ClientResponse {
  id: number;
  tenantId: number;
  clientName: string;
  siteUrl: string;
  siteId: string;
  createdBy: string;
  createdAt: string;
  status: ClientStatus;
  errorMessage?: string;
}

export interface LibraryResponse {
  id: string;
  name: string;
  displayName: string;
  description: string;
  webUrl: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  itemCount: number;
}

export interface ListResponse {
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

export interface CreateLibraryRequest {
  name: string;
  description?: string;
}

export interface CreateListRequest {
  name: string;
  description?: string;
  template?: string;
}

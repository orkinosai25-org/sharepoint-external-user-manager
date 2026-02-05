/**
 * Client model and interfaces
 * Represents a solicitor's customer mapped 1:1 to a SharePoint site
 */

export type ClientStatus = 'Provisioning' | 'Active' | 'Error';
export type SiteTemplateType = 'Team' | 'Communication';

export interface Client {
  id: number;
  tenantId: number;
  clientName: string;
  siteUrl: string;
  siteId: string;
  createdBy: string;
  createdAt: Date;
  status: ClientStatus;
  errorMessage?: string;
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

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

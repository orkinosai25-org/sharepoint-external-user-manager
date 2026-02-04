/**
 * Client model and interfaces
 * Represents a solicitor's customer mapped 1:1 to a SharePoint site
 */

export type ClientStatus = 'Provisioning' | 'Active' | 'Error';

export interface Client {
  id: number;
  tenantId: number;
  clientName: string;
  siteUrl: string;
  siteId: string;
  createdBy: string;
  createdAt: Date;
  status: ClientStatus;
}

export interface CreateClientRequest {
  clientName: string;
  siteUrl: string;
  siteId: string;
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
}

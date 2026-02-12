import { WebPartContext } from '@microsoft/sp-webpart-base';

export interface IExternalUserManagerProps {
  description: string;
  context: WebPartContext;
  backendApiUrl: string;
  portalUrl?: string; // URL to the SaaS portal for upgrade CTAs
}
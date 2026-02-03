/**
 * Tenant model and interfaces
 */

import { TenantStatus } from './common';

export interface Tenant {
  id: number;
  entraIdTenantId: string;
  organizationName: string;
  primaryAdminEmail: string;
  onboardedDate: Date;
  status: TenantStatus;
  settings?: TenantSettings;
  createdDate: Date;
  modifiedDate: Date;
}

export interface TenantSettings {
  timezone?: string;
  locale?: string;
  region?: string;
  customDomain?: string;
  [key: string]: any;
}

export interface OnboardTenantRequest {
  organizationName: string;
  primaryAdminEmail: string;
  settings?: TenantSettings;
}

export interface TenantResponse {
  tenantId: number;
  entraIdTenantId: string;
  organizationName: string;
  primaryAdminEmail: string;
  onboardedDate: string;
  status: TenantStatus;
  settings?: TenantSettings;
}

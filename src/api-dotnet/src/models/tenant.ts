/**
 * Tenant model and interfaces
 * 
 * Represents a top-level organizational entity in the multi-tenant system.
 * Each tenant has complete data isolation and its own subscription/configuration.
 */

import { TenantStatus } from './common';

/**
 * Core tenant model
 * Represents an organization using the SharePoint External User Manager
 */
export interface Tenant {
  /** Internal tenant identifier (database primary key) */
  id: number;
  /** Entra ID (Azure AD) tenant ID for authentication */
  entraIdTenantId: string;
  /** Organization display name */
  organizationName: string;
  /** Primary admin email address */
  primaryAdminEmail: string;
  /** Date when tenant was onboarded */
  onboardedDate: Date;
  /** Current tenant status */
  status: TenantStatus;
  /** Optional tenant configuration settings */
  settings?: TenantSettings;
  /** Record creation timestamp */
  createdDate: Date;
  /** Record last modified timestamp */
  modifiedDate: Date;
}

/**
 * Tenant configuration settings
 * Customizable per-tenant options
 */
export interface TenantSettings {
  /** Timezone for the tenant (IANA format, e.g., 'America/New_York') */
  timezone?: string;
  /** Locale/language code (e.g., 'en-US', 'fr-FR') */
  locale?: string;
  /** Geographic region for data residency */
  region?: string;
  /** Custom domain for the tenant (if applicable) */
  customDomain?: string;
  /** Additional custom settings */
  [key: string]: any;
}

/**
 * Request payload for tenant onboarding
 */
export interface OnboardTenantRequest {
  /** Organization display name */
  organizationName: string;
  /** Primary administrator email */
  primaryAdminEmail: string;
  /** Optional initial settings */
  settings?: TenantSettings;
}

/**
 * Response format for tenant data
 * Used for API responses with date strings instead of Date objects
 */
export interface TenantResponse {
  /** Internal tenant identifier */
  tenantId: number;
  /** Entra ID tenant ID */
  entraIdTenantId: string;
  /** Organization name */
  organizationName: string;
  /** Primary admin email */
  primaryAdminEmail: string;
  /** Onboarding date (ISO 8601 string) */
  onboardedDate: string;
  /** Current status */
  status: TenantStatus;
  /** Optional settings */
  settings?: TenantSettings;
}

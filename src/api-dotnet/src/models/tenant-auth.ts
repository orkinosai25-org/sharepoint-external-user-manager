/**
 * Tenant Authentication and OAuth Token models
 */

export interface TenantAuth {
  id: number;
  tenantId: number;
  accessToken?: string;
  refreshToken?: string;
  tokenExpiresAt?: Date;
  scope?: string;
  consentGrantedBy?: string;
  consentGrantedAt?: Date;
  lastTokenRefresh?: Date;
  createdDate: Date;
  modifiedDate: Date;
}

export interface OAuthTokenResponse {
  access_token: string;
  refresh_token?: string;
  expires_in: number;
  token_type: string;
  scope?: string;
}

export interface AdminConsentRequest {
  tenantId: string;
  adminConsent: string;
  state: string;
  error?: string;
  error_description?: string;
}

export interface ConnectTenantRequest {
  redirectUri: string;
}

export interface ConnectTenantResponse {
  authorizationUrl: string;
  state: string;
}

export interface ValidatePermissionsResponse {
  hasRequiredPermissions: boolean;
  grantedPermissions: string[];
  missingPermissions: string[];
}

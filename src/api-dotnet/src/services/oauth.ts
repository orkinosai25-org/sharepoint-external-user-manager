/**
 * OAuth Service for Microsoft Graph Admin Consent Flow
 * Handles OAuth authorization code flow for tenant onboarding
 */

import axios from 'axios';
import { config } from '../utils/config';
import { OAuthTokenResponse, ValidatePermissionsResponse } from '../models/tenant-auth';

export class OAuthService {
  private readonly authority = 'https://login.microsoftonline.com';
  private readonly graphScope = 'https://graph.microsoft.com/.default';
  
  /**
   * Required Microsoft Graph API permissions for the application
   */
  private readonly requiredPermissions = [
    'User.Read.All',
    'Sites.ReadWrite.All',
    'Sites.FullControl.All',
    'Directory.Read.All'
  ];

  /**
   * Generate admin consent URL for tenant onboarding
   * Uses tenant-specific endpoint for better compatibility
   */
  generateAdminConsentUrl(state: string, redirectUri: string, tenantId: string = 'common'): string {
    const params = new URLSearchParams({
      client_id: config.auth.clientId,
      redirect_uri: redirectUri,
      state: state,
      scope: this.graphScope,
      response_type: 'code',
      response_mode: 'query',
      prompt: 'admin_consent'
    });

    // Use tenant-specific endpoint for better compatibility
    // 'common' works for most cases but tenant-specific is more reliable
    return `${this.authority}/${tenantId}/v2.0/adminconsent?${params.toString()}`;
  }

  /**
   * Exchange authorization code for access token
   */
  async exchangeAuthorizationCode(
    code: string,
    redirectUri: string,
    tenantId: string
  ): Promise<OAuthTokenResponse> {
    try {
      const tokenEndpoint = `${this.authority}/${tenantId}/oauth2/v2.0/token`;
      
      const params = new URLSearchParams({
        client_id: config.auth.clientId,
        client_secret: config.auth.clientSecret,
        code: code,
        redirect_uri: redirectUri,
        grant_type: 'authorization_code',
        scope: this.graphScope
      });

      const response = await axios.post<OAuthTokenResponse>(
        tokenEndpoint,
        params.toString(),
        {
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
          }
        }
      );

      return response.data;
    } catch (error: any) {
      throw new Error(`Failed to exchange authorization code: ${error.message}`);
    }
  }

  /**
   * Refresh access token using refresh token
   */
  async refreshAccessToken(
    refreshToken: string,
    tenantId: string
  ): Promise<OAuthTokenResponse> {
    try {
      const tokenEndpoint = `${this.authority}/${tenantId}/oauth2/v2.0/token`;
      
      const params = new URLSearchParams({
        client_id: config.auth.clientId,
        client_secret: config.auth.clientSecret,
        refresh_token: refreshToken,
        grant_type: 'refresh_token',
        scope: this.graphScope
      });

      const response = await axios.post<OAuthTokenResponse>(
        tokenEndpoint,
        params.toString(),
        {
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
          }
        }
      );

      return response.data;
    } catch (error: any) {
      throw new Error(`Failed to refresh access token: ${error.message}`);
    }
  }

  /**
   * Validate that required Graph API permissions are granted
   * Checks appRoleAssignments to verify actual granted permissions
   */
  async validatePermissions(accessToken: string): Promise<ValidatePermissionsResponse> {
    try {
      // Get application service principal
      const spResponse = await axios.get(
        `https://graph.microsoft.com/v1.0/servicePrincipals?$filter=appId eq '${config.auth.clientId}'`,
        {
          headers: {
            'Authorization': `Bearer ${accessToken}`,
            'Content-Type': 'application/json'
          }
        }
      );

      if (spResponse.data.value.length === 0) {
        return {
          hasRequiredPermissions: false,
          grantedPermissions: [],
          missingPermissions: this.requiredPermissions
        };
      }

      const servicePrincipal = spResponse.data.value[0];
      
      // Get Microsoft Graph service principal to map role IDs to names
      const graphSpResponse = await axios.get(
        `https://graph.microsoft.com/v1.0/servicePrincipals?$filter=appId eq '00000003-0000-0000-c000-000000000000'`,
        {
          headers: {
            'Authorization': `Bearer ${accessToken}`,
            'Content-Type': 'application/json'
          }
        }
      );

      if (graphSpResponse.data.value.length === 0) {
        throw new Error('Microsoft Graph service principal not found');
      }

      const graphSp = graphSpResponse.data.value[0];
      
      // Get app role assignments to see what permissions are actually granted
      const assignmentsResponse = await axios.get(
        `https://graph.microsoft.com/v1.0/servicePrincipals/${servicePrincipal.id}/appRoleAssignments`,
        {
          headers: {
            'Authorization': `Bearer ${accessToken}`,
            'Content-Type': 'application/json'
          }
        }
      );

      // Map role IDs to permission names
      const grantedRoleIds = assignmentsResponse.data.value
        .filter((assignment: any) => assignment.resourceId === graphSp.id)
        .map((assignment: any) => assignment.appRoleId);

      const grantedPermissions = graphSp.appRoles
        .filter((role: any) => grantedRoleIds.includes(role.id))
        .map((role: any) => role.value);

      // Check which required permissions are missing
      const missingPermissions = this.requiredPermissions.filter(
        perm => !grantedPermissions.includes(perm)
      );

      return {
        hasRequiredPermissions: missingPermissions.length === 0,
        grantedPermissions,
        missingPermissions
      };
    } catch (error: any) {
      console.error('Failed to validate permissions:', error.message);
      
      // If we can't validate, return a conservative response
      return {
        hasRequiredPermissions: false,
        grantedPermissions: [],
        missingPermissions: this.requiredPermissions
      };
    }
  }

  /**
   * Get app-only access token using client credentials
   * This is for the backend to call Graph API on behalf of the app
   */
  async getAppAccessToken(tenantId: string): Promise<string> {
    try {
      const tokenEndpoint = `${this.authority}/${tenantId}/oauth2/v2.0/token`;
      
      const params = new URLSearchParams({
        client_id: config.auth.clientId,
        client_secret: config.auth.clientSecret,
        grant_type: 'client_credentials',
        scope: this.graphScope
      });

      const response = await axios.post<OAuthTokenResponse>(
        tokenEndpoint,
        params.toString(),
        {
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
          }
        }
      );

      return response.data.access_token;
    } catch (error: any) {
      throw new Error(`Failed to get app access token: ${error.message}`);
    }
  }
}

export const oauthService = new OAuthService();

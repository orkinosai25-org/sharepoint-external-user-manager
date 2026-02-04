/**
 * SharePoint site provisioning service
 * Handles creation and management of SharePoint sites via Microsoft Graph API
 */

import { Client } from '@microsoft/microsoft-graph-client';
import { ClientSecretCredential } from '@azure/identity';
import { config } from '../utils/config';
import { SiteTemplateType } from '../models/client';

export interface ProvisionSiteResult {
  siteId: string;
  siteUrl: string;
  success: boolean;
  error?: string;
}

class SharePointService {
  /**
   * Get authenticated Graph client
   */
  private getGraphClient(): Client {
    const credential = new ClientSecretCredential(
      config.auth.tenantId,
      config.auth.clientId,
      config.auth.clientSecret
    );

    return Client.initWithMiddleware({
      authProvider: {
        getAccessToken: async () => {
          const token = await credential.getToken('https://graph.microsoft.com/.default');
          return token.token;
        }
      }
    });
  }

  /**
   * Provision a new SharePoint site for a client
   * @param clientName - Name of the client (used for site title and URL)
   * @param siteTemplate - Type of site template (Team or Communication)
   * @param tenantDomain - SharePoint tenant domain (e.g., contoso.sharepoint.com)
   * @returns Provisioning result with site ID and URL
   */
  async provisionSite(
    clientName: string,
    siteTemplate: SiteTemplateType = 'Team',
    tenantDomain?: string
  ): Promise<ProvisionSiteResult> {
    // If Graph integration is disabled, return mock data
    if (!config.features.enableGraphIntegration) {
      console.log('Graph integration disabled, returning mock site provisioning');
      return this.getMockProvisionResult(clientName, siteTemplate);
    }

    try {
      const client = this.getGraphClient();
      
      // Generate a URL-friendly alias from client name
      const alias = this.generateSiteAlias(clientName);
      
      // Determine the site design based on template type
      const siteDesignId = siteTemplate === 'Communication' 
        ? '6142d2a0-63a5-4ba0-aede-d9fefca2c767' // Communication site
        : '64c5a1b4-0f5f-4c1c-9c7f-3b6c9d3f4a3a'; // Team site
      
      // Create the site using Graph API
      // For Team sites, we create a Microsoft 365 Group which automatically provisions a SharePoint site
      // For Communication sites, we create a standalone SharePoint site
      
      let siteResult;
      
      if (siteTemplate === 'Team') {
        // Create Microsoft 365 Group with Team site
        const groupResult = await client.api('/groups').post({
          displayName: clientName,
          mailNickname: alias,
          mailEnabled: true,
          securityEnabled: false,
          groupTypes: ['Unified'],
          visibility: 'Private',
          description: `Client space for ${clientName}`
        });
        
        // Wait a moment for site provisioning
        await this.delay(2000);
        
        // Get the SharePoint site associated with the group
        siteResult = await client.api(`/groups/${groupResult.id}/sites/root`).get();
      } else {
        // Create Communication site
        siteResult = await client.api('/sites').post({
          displayName: clientName,
          name: alias,
          siteDesignId: siteDesignId,
          description: `Client space for ${clientName}`
        });
      }
      
      return {
        siteId: siteResult.id,
        siteUrl: siteResult.webUrl,
        success: true
      };
    } catch (error: any) {
      console.error('Error provisioning SharePoint site:', error);
      
      // Extract meaningful error message
      const errorMessage = error.message || 'Failed to provision SharePoint site';
      
      return {
        siteId: '',
        siteUrl: '',
        success: false,
        error: errorMessage
      };
    }
  }

  /**
   * Check if a site exists and is accessible
   * @param siteId - SharePoint site ID
   */
  async verifySite(siteId: string): Promise<boolean> {
    if (!config.features.enableGraphIntegration) {
      return true; // Assume success in mock mode
    }

    try {
      const client = this.getGraphClient();
      await client.api(`/sites/${siteId}`).get();
      return true;
    } catch (error) {
      console.error('Error verifying site:', error);
      return false;
    }
  }

  /**
   * Generate a URL-friendly alias from client name
   * @param clientName - Original client name
   */
  private generateSiteAlias(clientName: string): string {
    return clientName
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-') // Replace non-alphanumeric with hyphens
      .replace(/^-+|-+$/g, '') // Remove leading/trailing hyphens
      .substring(0, 64); // Limit length
  }

  /**
   * Helper to add delay
   */
  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * Mock provisioning for development/testing
   */
  private getMockProvisionResult(
    clientName: string,
    siteTemplate: SiteTemplateType
  ): ProvisionSiteResult {
    const alias = this.generateSiteAlias(clientName);
    const mockTenantDomain = 'contoso.sharepoint.com';
    
    return {
      siteId: `mock-site-${Date.now()}`,
      siteUrl: `https://${mockTenantDomain}/sites/${alias}`,
      success: true
    };
  }
}

export const sharePointService = new SharePointService();

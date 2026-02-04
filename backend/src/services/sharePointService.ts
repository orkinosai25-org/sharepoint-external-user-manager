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
   * 
   * IMPORTANT NOTES:
   * - The Graph API endpoint and parameters used here are simplified for MVP
   * - In production, use proper SharePoint site creation APIs with full configuration
   * - The 2-second delay is insufficient for real provisioning (can take 30+ seconds)
   * - Implement proper polling with retry logic or use webhooks in production
   * 
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
   * Get document libraries for a SharePoint site
   * @param siteId - SharePoint site ID
   */
  async getLibraries(siteId: string): Promise<any[]> {
    if (!config.features.enableGraphIntegration) {
      console.log('Graph integration disabled, returning mock libraries');
      return this.getMockLibraries();
    }

    try {
      const client = this.getGraphClient();
      const response = await client
        .api(`/sites/${siteId}/drives`)
        .select('id,name,description,webUrl,createdDateTime,lastModifiedDateTime')
        .get();

      if (!response.value) {
        console.warn('Graph API response missing "value" property for drives');
        return [];
      }
      
      return response.value;
    } catch (error: any) {
      console.error('Error fetching libraries:', error);
      throw new Error(`Failed to fetch libraries: ${error.message}`);
    }
  }

  /**
   * Get lists for a SharePoint site
   * @param siteId - SharePoint site ID
   */
  async getLists(siteId: string): Promise<any[]> {
    if (!config.features.enableGraphIntegration) {
      console.log('Graph integration disabled, returning mock lists');
      return this.getMockLists();
    }

    try {
      const client = this.getGraphClient();
      const response = await client
        .api(`/sites/${siteId}/lists`)
        .select('id,name,displayName,description,webUrl,createdDateTime,lastModifiedDateTime,list')
        .get();

      if (!response.value) {
        console.warn('Graph API response missing "value" property for lists');
        return [];
      }
      
      return response.value;
    } catch (error: any) {
      console.error('Error fetching lists:', error);
      throw new Error(`Failed to fetch lists: ${error.message}`);
    }
  }

  /**
   * Create a new document library in a SharePoint site
   * @param siteId - SharePoint site ID
   * @param name - Library name
   * @param description - Library description (optional)
   */
  async createLibrary(siteId: string, name: string, description?: string): Promise<any> {
    if (!config.features.enableGraphIntegration) {
      console.log('Graph integration disabled, returning mock library creation');
      return this.getMockLibraryCreation(name, description);
    }

    try {
      const client = this.getGraphClient();
      
      // Create a new document library (drive) using Microsoft Graph API
      // Note: The Graph API creates document libraries via the drives endpoint
      const libraryData = {
        name: name,
        description: description || '',
        '@microsoft.graph.conflictBehavior': 'rename'
      };

      const response = await client
        .api(`/sites/${siteId}/lists`)
        .post({
          displayName: name,
          description: description || '',
          list: {
            template: 'documentLibrary'
          }
        });

      return response;
    } catch (error: any) {
      console.error('Error creating library:', error);
      throw new Error(`Failed to create library: ${error.message}`);
    }
  }

  /**
   * Create a new list in a SharePoint site
   * @param siteId - SharePoint site ID
   * @param name - List name
   * @param description - List description (optional)
   * @param template - List template type (optional, defaults to 'genericList')
   */
  async createList(siteId: string, name: string, description?: string, template: string = 'genericList'): Promise<any> {
    if (!config.features.enableGraphIntegration) {
      console.log('Graph integration disabled, returning mock list creation');
      return this.getMockListCreation(name, description, template);
    }

    try {
      const client = this.getGraphClient();
      
      // Create a new list using Microsoft Graph API
      const response = await client
        .api(`/sites/${siteId}/lists`)
        .post({
          displayName: name,
          description: description || '',
          list: {
            template: template
          }
        });

      return response;
    } catch (error: any) {
      console.error('Error creating list:', error);
      throw new Error(`Failed to create list: ${error.message}`);
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

  /**
   * Mock libraries for development/testing
   */
  private getMockLibraries(): any[] {
    return [
      {
        id: 'mock-library-1',
        name: 'Documents',
        description: 'Default document library',
        webUrl: 'https://contoso.sharepoint.com/sites/client/Shared%20Documents',
        createdDateTime: '2024-01-01T10:00:00Z',
        lastModifiedDateTime: '2024-01-15T14:30:00Z'
      },
      {
        id: 'mock-library-2',
        name: 'Contracts',
        description: 'Client contracts and agreements',
        webUrl: 'https://contoso.sharepoint.com/sites/client/Contracts',
        createdDateTime: '2024-01-05T09:00:00Z',
        lastModifiedDateTime: '2024-01-14T11:20:00Z'
      }
    ];
  }

  /**
   * Mock lists for development/testing
   */
  private getMockLists(): any[] {
    return [
      {
        id: 'mock-list-1',
        name: 'Tasks',
        displayName: 'Tasks',
        description: 'Task tracking list',
        webUrl: 'https://contoso.sharepoint.com/sites/client/Lists/Tasks',
        createdDateTime: '2024-01-01T10:00:00Z',
        lastModifiedDateTime: '2024-01-15T16:45:00Z',
        list: {
          template: 'genericList'
        }
      },
      {
        id: 'mock-list-2',
        name: 'Issues',
        displayName: 'Issues',
        description: 'Issue tracking list',
        webUrl: 'https://contoso.sharepoint.com/sites/client/Lists/Issues',
        createdDateTime: '2024-01-03T11:30:00Z',
        lastModifiedDateTime: '2024-01-14T09:15:00Z',
        list: {
          template: 'issueTracking'
        }
      }
    ];
  }

  /**
   * Mock library creation for development/testing
   */
  private getMockLibraryCreation(name: string, description?: string): any {
    const now = new Date().toISOString();
    return {
      id: `mock-library-${Date.now()}`,
      name: name,
      displayName: name,
      description: description || '',
      webUrl: `https://contoso.sharepoint.com/sites/client/${name.replace(/\s+/g, '%20')}`,
      createdDateTime: now,
      lastModifiedDateTime: now,
      list: {
        template: 'documentLibrary'
      }
    };
  }

  /**
   * Mock list creation for development/testing
   */
  private getMockListCreation(name: string, description?: string, template: string = 'genericList'): any {
    const now = new Date().toISOString();
    return {
      id: `mock-list-${Date.now()}`,
      name: name,
      displayName: name,
      description: description || '',
      webUrl: `https://contoso.sharepoint.com/sites/client/Lists/${name.replace(/\s+/g, '%20')}`,
      createdDateTime: now,
      lastModifiedDateTime: now,
      list: {
        template: template
      }
    };
  }
}

export const sharePointService = new SharePointService();

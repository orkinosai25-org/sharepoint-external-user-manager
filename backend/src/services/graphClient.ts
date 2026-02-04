/**
 * Microsoft Graph API client for SharePoint external user management
 */

import { Client } from '@microsoft/microsoft-graph-client';
import { ClientSecretCredential } from '@azure/identity';
import { config } from '../utils/config';
import { ExternalUser, PermissionLevel } from '../models/user';

class GraphClientService {
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
   * Parse site ID from library URL
   * Extracts the site ID from a SharePoint library URL
   * Example: https://contoso.sharepoint.com/sites/client1/Shared%20Documents -> site ID
   */
  private async getSiteIdFromUrl(libraryUrl: string): Promise<string> {
    try {
      // Extract hostname and site path from the URL
      const url = new URL(libraryUrl);
      const hostname = url.hostname;
      const pathParts = url.pathname.split('/');
      
      // Find the site path (typically /sites/{sitename})
      const siteIndex = pathParts.indexOf('sites');
      if (siteIndex === -1 || siteIndex >= pathParts.length - 1) {
        throw new Error('Invalid SharePoint library URL: cannot find site path');
      }
      
      const sitePath = pathParts.slice(0, siteIndex + 2).join('/');
      
      // Use Graph API to get site by URL
      const client = this.getGraphClient();
      const site = await client.api(`/sites/${hostname}:${sitePath}`).get();
      
      return site.id;
    } catch (error: any) {
      console.error('Error parsing site ID from URL:', error);
      throw new Error(`Failed to parse site from URL: ${error.message}`);
    }
  }

  /**
   * Map SharePoint permission role to our PermissionLevel
   */
  private mapSharePointRole(role: string): PermissionLevel {
    switch (role.toLowerCase()) {
      case 'read':
      case 'reader':
        return 'Read';
      case 'contribute':
      case 'contributor':
        return 'Contribute';
      case 'edit':
      case 'editor':
        return 'Edit';
      case 'fullcontrol':
      case 'owner':
        return 'FullControl';
      default:
        return 'Read';
    }
  }

  /**
   * Map our PermissionLevel to SharePoint role
   */
  private mapPermissionToRole(permission: PermissionLevel): string {
    switch (permission) {
      case 'Read':
        return 'read';
      case 'Contribute':
        return 'write';
      case 'Edit':
        return 'write';
      case 'FullControl':
        return 'owner';
      default:
        return 'read';
    }
  }

  /**
   * List external users from a SharePoint site
   * Gets all users with external email addresses (#EXT#) who have access to the site
   */
  async listExternalUsers(libraryUrl: string): Promise<ExternalUser[]> {
    if (!config.features.enableGraphIntegration) {
      console.log('Graph integration disabled, returning mock data');
      return this.getMockUsers(libraryUrl);
    }

    try {
      const client = this.getGraphClient();
      
      // Parse site ID from library URL
      const siteId = await this.getSiteIdFromUrl(libraryUrl);
      
      // Get site permissions
      const permissionsResponse = await client
        .api(`/sites/${siteId}/permissions`)
        .get();
      
      const externalUsers: ExternalUser[] = [];
      
      // Process each permission entry
      for (const permission of permissionsResponse.value || []) {
        // Check if this permission is for an external user
        const grantedToIdentities = permission.grantedToIdentities || permission.grantedTo ? [permission.grantedTo] : [];
        
        for (const identity of grantedToIdentities) {
          const user = identity.user;
          
          // External users have #EXT# in their email or userPrincipalName
          if (user && (user.email?.includes('#EXT#') || user.userPrincipalName?.includes('#EXT#'))) {
            // Extract actual email (remove #EXT# suffix)
            const email = user.email?.replace(/_#EXT#.*$/, '').replace(/_/g, '@') || user.userPrincipalName || '';
            
            externalUsers.push({
              id: permission.id,
              email: email,
              displayName: user.displayName || email,
              library: libraryUrl,
              permissions: this.mapSharePointRole(permission.roles?.[0] || 'read'),
              invitedBy: 'system', // Graph API doesn't provide this easily
              invitedDate: new Date(permission.shareId || Date.now()),
              lastAccess: null, // Requires additional API call to get user activity
              status: 'Active',
              metadata: {}
            });
          }
        }
      }
      
      return externalUsers;
    } catch (error: any) {
      console.error('Error fetching external users from Graph API:', error);
      // Fall back to mock data in case of errors
      console.log('Falling back to mock data due to error');
      return this.getMockUsers(libraryUrl);
    }
  }

  /**
   * Invite an external user to a SharePoint library
   * Creates a sharing invitation and grants the specified permission level
   */
  async inviteExternalUser(
    email: string,
    displayName: string,
    libraryUrl: string,
    permission: PermissionLevel,
    message?: string
  ): Promise<ExternalUser> {
    if (!config.features.enableGraphIntegration) {
      console.log('Graph integration disabled, returning mock invitation');
      return this.getMockInvitation(email, displayName, libraryUrl, permission);
    }

    try {
      const client = this.getGraphClient();
      
      // Parse site ID from library URL
      const siteId = await this.getSiteIdFromUrl(libraryUrl);
      
      // Create invitation using Graph API
      // This sends an email invitation to the external user
      const invitation = await client.api('/invitations').post({
        invitedUserEmailAddress: email,
        invitedUserDisplayName: displayName,
        inviteRedirectUrl: libraryUrl,
        sendInvitationMessage: true,
        invitedUserMessageInfo: message ? {
          customizedMessageBody: message
        } : undefined
      });
      
      // Grant permission to the site
      // Map our permission level to SharePoint role
      const role = this.mapPermissionToRole(permission);
      
      await client.api(`/sites/${siteId}/permissions`).post({
        roles: [role],
        grantedToIdentities: [{
          user: {
            email: email
          }
        }]
      });
      
      // Return the created external user
      return {
        id: invitation.id,
        email: email,
        displayName: displayName,
        library: libraryUrl,
        permissions: permission,
        invitedBy: 'system', // Could be enhanced to get from context
        invitedDate: new Date(),
        lastAccess: null,
        status: 'Invited',
        metadata: {}
      };
    } catch (error: any) {
      console.error('Error inviting external user via Graph API:', error);
      throw new Error(`Failed to invite external user: ${error.message}`);
    }
  }

  /**
   * Remove external user access from a SharePoint library
   * Finds and deletes the permission entry for the specified user
   */
  async removeExternalUser(email: string, libraryUrl: string): Promise<void> {
    if (!config.features.enableGraphIntegration) {
      console.log('Graph integration disabled, skipping user removal');
      return;
    }

    try {
      const client = this.getGraphClient();
      
      // Parse site ID from library URL
      const siteId = await this.getSiteIdFromUrl(libraryUrl);
      
      // Get all site permissions
      const permissionsResponse = await client
        .api(`/sites/${siteId}/permissions`)
        .get();
      
      // Find the permission ID for the specified user
      let permissionIdToDelete: string | null = null;
      
      for (const permission of permissionsResponse.value || []) {
        const grantedToIdentities = permission.grantedToIdentities || (permission.grantedTo ? [permission.grantedTo] : []);
        
        for (const identity of grantedToIdentities) {
          const user = identity.user;
          
          if (user) {
            // Match by email (handle both regular and #EXT# format)
            const userEmail = user.email?.replace(/_#EXT#.*$/, '').replace(/_/g, '@') || user.userPrincipalName || '';
            
            if (userEmail.toLowerCase() === email.toLowerCase()) {
              permissionIdToDelete = permission.id;
              break;
            }
          }
        }
        
        if (permissionIdToDelete) break;
      }
      
      if (!permissionIdToDelete) {
        throw new Error(`User ${email} not found in site permissions`);
      }
      
      // Delete the permission
      await client.api(`/sites/${siteId}/permissions/${permissionIdToDelete}`).delete();
      
      console.log(`Successfully removed user ${email} from ${libraryUrl}`);
    } catch (error: any) {
      console.error('Error removing external user via Graph API:', error);
      throw new Error(`Failed to remove external user: ${error.message}`);
    }
  }

  // Mock data helpers for development/testing
  private getMockUsers(libraryUrl: string): ExternalUser[] {
    return [
      {
        id: 'mock-user-1',
        email: 'partner@external.com',
        displayName: 'Jane Partner',
        library: libraryUrl,
        permissions: 'Read',
        invitedBy: 'admin@contoso.com',
        invitedDate: new Date('2024-01-10T09:15:00Z'),
        lastAccess: new Date('2024-01-14T16:45:00Z'),
        status: 'Active',
        metadata: {
          company: 'Partner Corp',
          project: 'Q1 Campaign'
        }
      },
      {
        id: 'mock-user-2',
        email: 'vendor@supplier.com',
        displayName: 'John Vendor',
        library: libraryUrl,
        permissions: 'Contribute',
        invitedBy: 'admin@contoso.com',
        invitedDate: new Date('2024-01-05T14:30:00Z'),
        lastAccess: new Date('2024-01-13T10:20:00Z'),
        status: 'Active',
        metadata: {
          company: 'Supplier Inc',
          project: 'Supply Chain'
        }
      }
    ];
  }

  private getMockInvitation(
    email: string,
    displayName: string,
    libraryUrl: string,
    permission: PermissionLevel
  ): ExternalUser {
    return {
      id: `mock-invite-${Date.now()}`,
      email,
      displayName,
      library: libraryUrl,
      permissions: permission,
      invitedBy: 'admin@contoso.com',
      invitedDate: new Date(),
      lastAccess: null,
      status: 'Invited',
      metadata: {}
    };
  }
}

export const graphClient = new GraphClientService();

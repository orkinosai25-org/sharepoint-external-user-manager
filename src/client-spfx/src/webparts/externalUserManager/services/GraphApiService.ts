import { WebPartContext } from '@microsoft/sp-webpart-base';
import { MSGraphClientV3 } from '@microsoft/sp-http';
import { IExternalLibrary } from '../models/IExternalLibrary';
import { AuditLogger } from './AuditLogger';

/**
 * Microsoft Graph API Service for SharePoint library management
 * 
 * Used as fallback when PnPjs doesn't support specific operations:
 * - Tenant-level operations
 * - Cross-site operations requiring elevated permissions
 * - Advanced user management scenarios
 * - External sharing configuration
 */
export class GraphApiService {
  private context: WebPartContext;
  private auditLogger: AuditLogger;

  constructor(context: WebPartContext) {
    this.context = context;
    this.auditLogger = new AuditLogger(context);
  }

  /**
   * Get Microsoft Graph client
   */
  private async getGraphClient(): Promise<MSGraphClientV3> {
    return await this.context.msGraphClientFactory.getClient('3');
  }

  /**
   * Enable external sharing for a site using Graph API
   * This requires tenant admin permissions
   */
  public async enableExternalSharingForSite(siteId: string): Promise<void> {
    try {
      this.auditLogger.logInfo('enableExternalSharingForSite', `Enabling external sharing for site: ${siteId}`);

      const graphClient = await this.getGraphClient();

      // Update site sharing capabilities
      const siteUpdateRequest = {
        sharingCapabilities: 'ExternalUserAndGuestSharing',
        allowDownload: true,
        allowSharing: true
      };

      await graphClient
        .api(`/sites/${siteId}`)
        .patch(siteUpdateRequest);

      this.auditLogger.logInfo('enableExternalSharingForSite', `Successfully enabled external sharing for site: ${siteId}`);

    } catch (error) {
      this.auditLogger.logError('enableExternalSharingForSite', `Failed to enable external sharing for site: ${siteId}`, error);
      throw new Error(`Failed to enable external sharing: ${error.message}`);
    }
  }

  /**
   * Get external users for a specific site
   * Uses Graph API to get more detailed user information
   */
  public async getExternalUsersForSite(siteId: string): Promise<any[]> {
    try {
      this.auditLogger.logInfo('getExternalUsersForSite', `Fetching external users for site: ${siteId}`);

      const graphClient = await this.getGraphClient();

      // Get site members with external user information
      const response = await graphClient
        .api(`/sites/${siteId}/permissions`)
        .get();

      const externalUsers = response.value.filter((permission: any) => 
        permission.grantedToIdentities && 
        permission.grantedToIdentities.some((identity: any) => 
          identity.user && identity.user.email && 
          (identity.user.email.includes('#ext#') || !identity.user.email.endsWith(this.context.pageContext.aadInfo.tenantId))
        )
      );

      this.auditLogger.logInfo('getExternalUsersForSite', `Found ${externalUsers.length} external users`);
      return externalUsers;

    } catch (error) {
      this.auditLogger.logError('getExternalUsersForSite', `Failed to get external users for site: ${siteId}`, error);
      throw new Error(`Failed to get external users: ${error.message}`);
    }
  }

  /**
   * Create sharing link for a library
   * Uses Graph API for advanced sharing configuration
   */
  public async createSharingLink(siteId: string, listId: string, options: {
    type: 'view' | 'edit';
    scope: 'anonymous' | 'organization' | 'users';
    expirationDateTime?: Date;
    password?: string;
  }): Promise<string> {
    try {
      this.auditLogger.logInfo('createSharingLink', `Creating sharing link for list: ${listId}`);

      const graphClient = await this.getGraphClient();

      const sharingRequest = {
        type: options.type,
        scope: options.scope,
        ...(options.expirationDateTime && { expirationDateTime: options.expirationDateTime.toISOString() }),
        ...(options.password && { password: options.password })
      };

      const response = await graphClient
        .api(`/sites/${siteId}/lists/${listId}/createLink`)
        .post(sharingRequest);

      this.auditLogger.logInfo('createSharingLink', `Successfully created sharing link for list: ${listId}`);
      return response.link.webUrl;

    } catch (error) {
      this.auditLogger.logError('createSharingLink', `Failed to create sharing link for list: ${listId}`, error);
      throw new Error(`Failed to create sharing link: ${error.message}`);
    }
  }

  /**
   * Invite external users to a library
   * Uses Graph API for advanced invitation features
   */
  public async inviteExternalUsers(siteId: string, listId: string, invitations: {
    email: string;
    displayName?: string;
    role: 'read' | 'write' | 'owner';
    message?: string;
  }[]): Promise<void> {
    try {
      this.auditLogger.logInfo('inviteExternalUsers', `Inviting ${invitations.length} external users to list: ${listId}`);

      const graphClient = await this.getGraphClient();

      for (const invitation of invitations) {
        try {
          const inviteRequest = {
            recipients: [{
              email: invitation.email,
              ...(invitation.displayName && { displayName: invitation.displayName })
            }],
            message: invitation.message || 'You have been invited to access a SharePoint library.',
            requireSignIn: true,
            sendInvitation: true,
            roles: [invitation.role]
          };

          await graphClient
            .api(`/sites/${siteId}/lists/${listId}/invite`)
            .post(inviteRequest);

          this.auditLogger.logInfo('inviteExternalUsers', `Successfully invited user: ${invitation.email}`);
        } catch (error) {
          this.auditLogger.logError('inviteExternalUsers', `Failed to invite user: ${invitation.email}`, error);
          // Continue with other invitations
        }
      }

    } catch (error) {
      this.auditLogger.logError('inviteExternalUsers', `Failed to invite external users to list: ${listId}`, error);
      throw new Error(`Failed to invite external users: ${error.message}`);
    }
  }

  /**
   * Get site information including sharing settings
   */
  public async getSiteInfo(siteId: string): Promise<any> {
    try {
      this.auditLogger.logInfo('getSiteInfo', `Fetching site information for: ${siteId}`);

      const graphClient = await this.getGraphClient();

      const response = await graphClient
        .api(`/sites/${siteId}`)
        .select('id,name,webUrl,sharingCapabilities,allowDownload,allowSharing')
        .get();

      this.auditLogger.logInfo('getSiteInfo', `Successfully retrieved site information for: ${siteId}`);
      return response;

    } catch (error) {
      this.auditLogger.logError('getSiteInfo', `Failed to get site information for: ${siteId}`, error);
      throw new Error(`Failed to get site information: ${error.message}`);
    }
  }

  /**
   * Revoke external user access
   * Uses Graph API for advanced permission management
   */
  public async revokeExternalUserAccess(siteId: string, permissionId: string): Promise<void> {
    try {
      this.auditLogger.logInfo('revokeExternalUserAccess', `Revoking access for permission: ${permissionId}`);

      const graphClient = await this.getGraphClient();

      await graphClient
        .api(`/sites/${siteId}/permissions/${permissionId}`)
        .delete();

      this.auditLogger.logInfo('revokeExternalUserAccess', `Successfully revoked access for permission: ${permissionId}`);

    } catch (error) {
      this.auditLogger.logError('revokeExternalUserAccess', `Failed to revoke access for permission: ${permissionId}`, error);
      throw new Error(`Failed to revoke access: ${error.message}`);
    }
  }

  /**
   * Get sharing analytics for a site
   * Provides insights into external sharing patterns
   */
  public async getSharingAnalytics(siteId: string, days: number = 30): Promise<any> {
    try {
      this.auditLogger.logInfo('getSharingAnalytics', `Fetching sharing analytics for site: ${siteId}`);

      const graphClient = await this.getGraphClient();

      const endDate = new Date();
      const startDate = new Date();
      startDate.setDate(endDate.getDate() - days);

      // Note: This is a placeholder for actual Graph API analytics endpoints
      // Real implementation would use specific analytics APIs when available
      const response = await graphClient
        .api(`/sites/${siteId}/analytics/allTime`)
        .get();

      this.auditLogger.logInfo('getSharingAnalytics', `Successfully retrieved sharing analytics for site: ${siteId}`);
      return response;

    } catch (error) {
      this.auditLogger.logError('getSharingAnalytics', `Failed to get sharing analytics for site: ${siteId}`, error);
      throw new Error(`Failed to get sharing analytics: ${error.message}`);
    }
  }
}
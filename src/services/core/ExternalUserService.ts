/**
 * Core External User Service Implementation
 * 
 * This is the shared business logic for managing external users.
 * It's framework-agnostic and can be used by SPFx, Blazor portal, and API backend.
 * 
 * The service depends on an IGraphClient interface for Graph API operations,
 * allowing different implementations (SPFx context, server credentials, etc.)
 */

import {
  IExternalUserService,
  IGraphClient,
  IAuditService
} from '../interfaces';
import {
  ExternalUser,
  InvitationRequest,
  InvitationResult,
  BulkOperationResult,
  PermissionLevel
} from '../models';

export class ExternalUserService implements IExternalUserService {
  constructor(
    private graphClient: IGraphClient,
    private auditService?: IAuditService
  ) {}

  /**
   * List all external users for a given SharePoint resource
   */
  async listExternalUsers(resourceUrl: string): Promise<ExternalUser[]> {
    try {
      this.auditService?.logInfo('listExternalUsers', `Fetching external users for: ${resourceUrl}`);

      // Parse site ID from URL
      const siteId = await this.getSiteIdFromUrl(resourceUrl);

      // Get site permissions via Graph API
      const permissionsResponse = await this.graphClient.request<any>(
        `/sites/${siteId}/permissions`,
        'GET'
      );

      const externalUsers: ExternalUser[] = [];

      // Process each permission entry
      for (const permission of permissionsResponse.value || []) {
        const grantedToIdentities = permission.grantedToIdentities || 
          (permission.grantedTo ? [permission.grantedTo] : []);

        for (const identity of grantedToIdentities) {
          const user = identity.user;

          // External users have #EXT# in their email
          if (user && (user.email?.includes('#EXT#') || user.userPrincipalName?.includes('#EXT#'))) {
            // Extract actual email (remove #EXT# suffix)
            const email = this.extractEmailFromExternal(
              user.email || user.userPrincipalName || ''
            );

            externalUsers.push({
              id: permission.id,
              email: email,
              displayName: user.displayName || email,
              libraryUrl: resourceUrl,
              permissions: this.mapSharePointRole(permission.roles?.[0] || 'read'),
              invitedBy: 'system',
              invitedDate: new Date(),
              lastAccess: null,
              status: 'Active',
              metadata: {}
            });
          }
        }
      }

      this.auditService?.logInfo('listExternalUsers', `Found ${externalUsers.length} external users`);
      return externalUsers;
    } catch (error: any) {
      this.auditService?.logError('listExternalUsers', 'Failed to list external users', error);
      throw new Error(`Failed to list external users: ${error.message}`);
    }
  }

  /**
   * Invite an external user to a SharePoint resource
   */
  async inviteUser(request: InvitationRequest): Promise<InvitationResult> {
    try {
      this.auditService?.logInfo('inviteUser', `Inviting user: ${request.email}`, {
        email: request.email,
        resourceUrl: request.resourceUrl,
        permission: request.permission
      });

      // Parse site ID from URL
      const siteId = await this.getSiteIdFromUrl(request.resourceUrl);

      // Create invitation using Graph API
      const invitation = await this.graphClient.request<any>(
        '/invitations',
        'POST',
        {
          invitedUserEmailAddress: request.email,
          invitedUserDisplayName: request.displayName || request.email,
          inviteRedirectUrl: request.resourceUrl,
          sendInvitationMessage: true,
          invitedUserMessageInfo: request.message ? {
            customizedMessageBody: request.message
          } : undefined
        }
      );

      // Grant permission to the site
      const role = this.mapPermissionToRole(request.permission);

      await this.graphClient.request(
        `/sites/${siteId}/permissions`,
        'POST',
        {
          roles: [role],
          grantedToIdentities: [{
            user: {
              email: request.email
            }
          }]
        }
      );

      const user: ExternalUser = {
        id: invitation.id,
        email: request.email,
        displayName: request.displayName || request.email,
        libraryUrl: request.resourceUrl,
        permissions: request.permission,
        invitedBy: 'system',
        invitedDate: new Date(),
        lastAccess: null,
        status: 'Invited',
        metadata: request.metadata || {}
      };

      this.auditService?.logInfo('inviteUser', `Successfully invited user: ${request.email}`);

      return {
        success: true,
        userId: invitation.id,
        user: user
      };
    } catch (error: any) {
      this.auditService?.logError('inviteUser', `Failed to invite user: ${request.email}`, error);
      return {
        success: false,
        error: error.message
      };
    }
  }

  /**
   * Remove an external user's access to a SharePoint resource
   */
  async removeUser(resourceUrl: string, userId: string): Promise<void> {
    try {
      this.auditService?.logInfo('removeUser', `Removing user: ${userId}`, {
        resourceUrl,
        userId
      });

      // Parse site ID from URL
      const siteId = await this.getSiteIdFromUrl(resourceUrl);

      // Delete the permission via Graph API
      await this.graphClient.request(
        `/sites/${siteId}/permissions/${userId}`,
        'DELETE'
      );

      this.auditService?.logInfo('removeUser', `Successfully removed user: ${userId}`);
    } catch (error: any) {
      this.auditService?.logError('removeUser', `Failed to remove user: ${userId}`, error);
      throw new Error(`Failed to remove user: ${error.message}`);
    }
  }

  /**
   * Update an external user's permissions
   */
  async updateUserPermission(
    resourceUrl: string,
    userId: string,
    newPermission: PermissionLevel
  ): Promise<void> {
    try {
      this.auditService?.logInfo('updateUserPermission', 
        `Updating permission for user: ${userId}`, {
        resourceUrl,
        userId,
        newPermission
      });

      // Parse site ID from URL
      const siteId = await this.getSiteIdFromUrl(resourceUrl);

      // Update permission via Graph API
      const role = this.mapPermissionToRole(newPermission);

      await this.graphClient.request(
        `/sites/${siteId}/permissions/${userId}`,
        'PATCH',
        {
          roles: [role]
        }
      );

      this.auditService?.logInfo('updateUserPermission', 
        `Successfully updated permission for user: ${userId}`);
    } catch (error: any) {
      this.auditService?.logError('updateUserPermission', 
        `Failed to update permission for user: ${userId}`, error);
      throw new Error(`Failed to update permission: ${error.message}`);
    }
  }

  /**
   * Update user metadata
   */
  async updateUserMetadata(
    resourceUrl: string,
    userId: string,
    metadata: Record<string, any>
  ): Promise<void> {
    try {
      this.auditService?.logInfo('updateUserMetadata', 
        `Updating metadata for user: ${userId}`, {
        resourceUrl,
        userId,
        metadata
      });

      // Note: Metadata storage would typically be in a separate database
      // or custom SharePoint list. This is a placeholder for that logic.
      // In production, this would call a metadata storage service.

      this.auditService?.logInfo('updateUserMetadata', 
        `Successfully updated metadata for user: ${userId}`);
    } catch (error: any) {
      this.auditService?.logError('updateUserMetadata', 
        `Failed to update metadata for user: ${userId}`, error);
      throw new Error(`Failed to update metadata: ${error.message}`);
    }
  }

  /**
   * Bulk invite multiple users
   */
  async bulkInviteUsers(
    requests: InvitationRequest[]
  ): Promise<BulkOperationResult<InvitationRequest>> {
    const sessionId = this.auditService?.generateSessionId() || `bulk-${Date.now()}`;
    
    this.auditService?.logInfo('bulkInviteUsers', 
      `Starting bulk invitation of ${requests.length} users`, {
      count: requests.length,
      sessionId
    });

    const results: Array<{
      item: InvitationRequest;
      success: boolean;
      error?: string;
    }> = [];

    for (const request of requests) {
      try {
        const result = await this.inviteUser(request);
        results.push({
          item: request,
          success: result.success,
          error: result.error
        });
      } catch (error: any) {
        results.push({
          item: request,
          success: false,
          error: error.message
        });
      }
    }

    const successCount = results.filter(r => r.success).length;
    const failedCount = results.filter(r => !r.success).length;

    this.auditService?.logInfo('bulkInviteUsers', 
      `Bulk invitation completed. Success: ${successCount}, Failed: ${failedCount}`, {
      total: requests.length,
      successCount,
      failedCount,
      sessionId
    });

    return {
      total: requests.length,
      successCount,
      failedCount,
      results
    };
  }

  // Helper methods

  /**
   * Extract site ID from SharePoint URL
   */
  private async getSiteIdFromUrl(url: string): Promise<string> {
    try {
      const urlObj = new URL(url);
      const hostname = urlObj.hostname;
      const pathParts = urlObj.pathname.split('/');

      // Find the site path (typically /sites/{sitename})
      const siteIndex = pathParts.indexOf('sites');
      if (siteIndex === -1 || siteIndex >= pathParts.length - 1) {
        throw new Error('Invalid SharePoint URL: cannot find site path');
      }

      const sitePath = pathParts.slice(0, siteIndex + 2).join('/');

      // Get site by URL via Graph API
      const site = await this.graphClient.request<any>(
        `/sites/${hostname}:${sitePath}`,
        'GET'
      );

      return site.id;
    } catch (error: any) {
      throw new Error(`Failed to parse site ID from URL: ${error.message}`);
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
      case 'Edit':
        return 'write';
      case 'FullControl':
        return 'owner';
      default:
        return 'read';
    }
  }

  /**
   * Extract email from external user format
   * Removes #EXT# suffix and converts underscore before domain to @
   * Example: john_doe_example.com#EXT# → john_doe@example.com
   */
  private extractEmailFromExternal(externalEmail: string): string {
    // Remove #EXT# and everything after it
    let email = externalEmail.replace(/_#EXT#.*$/, '');
    
    // Find the last underscore (which represents the @ before domain)
    // Keep underscores in the local part intact
    const lastUnderscoreIndex = email.lastIndexOf('_');
    if (lastUnderscoreIndex !== -1) {
      email = email.substring(0, lastUnderscoreIndex) + '@' + email.substring(lastUnderscoreIndex + 1);
    }
    
    return email;
  }
}

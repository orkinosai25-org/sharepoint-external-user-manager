/**
 * SharePoint Data Service - REFACTORED VERSION
 * 
 * This demonstrates the new pattern using shared services for CS-SAAS-REF-01
 * This service is now a thin wrapper that delegates to shared services
 */

import { WebPartContext } from '@microsoft/sp-webpart-base';
import { IExternalLibrary, IExternalUser } from '../models/IExternalLibrary';

// NEW: Import shared services and adapters
import { ExternalUserService, LibraryService } from '../../../../../services/core';
import { InvitationRequest, PermissionLevel } from '../../../../../services/models';
import { SPFxGraphClient, SPFxAuditService } from '../../../../shared/adapters';

/**
 * Refactored SharePoint Data Service
 * Acts as a thin adapter between SPFx models and shared services
 */
export class SharePointDataServiceRefactored {
  private userService: ExternalUserService;
  private libraryService: LibraryService;
  private context: WebPartContext;

  constructor(context: WebPartContext) {
    this.context = context;

    // Create adapters
    const graphClient = new SPFxGraphClient(context);
    const auditService = new SPFxAuditService(context);

    // Create shared service instances
    this.userService = new ExternalUserService(graphClient, auditService);
    this.libraryService = new LibraryService(graphClient, auditService);
  }

  /**
   * Get external users for a library
   * Delegates to shared service and converts models
   */
  public async getExternalUsersForLibrary(libraryUrl: string): Promise<IExternalUser[]> {
    try {
      // Delegate to shared service
      const users = await this.userService.listExternalUsers(libraryUrl);

      // Convert shared model to SPFx model
      return users.map(user => ({
        id: user.id,
        email: user.email,
        displayName: user.displayName,
        invitedBy: user.invitedBy,
        invitedDate: user.invitedDate,
        lastAccess: user.lastAccess,
        permissions: this.mapPermissionLevel(user.permissions),
        company: user.metadata?.company,
        project: user.metadata?.project
      }));
    } catch (error: any) {
      throw new Error(`Failed to get external users: ${error.message}`);
    }
  }

  /**
   * Add external user to a library
   * Delegates to shared service
   */
  public async addExternalUserToLibrary(
    libraryUrl: string,
    email: string,
    permission: 'Read' | 'Contribute' | 'Full Control',
    company?: string,
    project?: string
  ): Promise<void> {
    try {
      // Create invitation request
      const request: InvitationRequest = {
        email,
        displayName: displayName || email.split('@')[0], // Use email prefix as fallback
        resourceUrl: libraryUrl,
        permission: this.mapToSharedPermission(permission),
        metadata: {
          company,
          project
        }
      };

      // Delegate to shared service
      const result = await this.userService.inviteUser(request);

      if (!result.success) {
        throw new Error(result.error || 'Failed to invite user');
      }
    } catch (error: any) {
      throw new Error(`Failed to add user: ${error.message}`);
    }
  }

  /**
   * Remove external user from a library
   * Delegates to shared service
   */
  public async removeExternalUserFromLibrary(libraryUrl: string, userId: string): Promise<void> {
    try {
      await this.userService.removeUser(libraryUrl, userId);
    } catch (error: any) {
      throw new Error(`Failed to remove user: ${error.message}`);
    }
  }

  /**
   * Update user metadata
   * Delegates to shared service
   */
  public async updateUserMetadata(
    libraryUrl: string,
    userId: string,
    company: string,
    project: string
  ): Promise<void> {
    try {
      await this.userService.updateUserMetadata(libraryUrl, userId, {
        company,
        project
      });
    } catch (error: any) {
      throw new Error(`Failed to update user metadata: ${error.message}`);
    }
  }

  /**
   * Bulk add external users to a library
   * Delegates to shared service
   */
  public async bulkAddExternalUsersToLibrary(
    libraryUrl: string,
    request: {
      emails: string[];
      permission: 'Read' | 'Contribute' | 'Full Control';
      company?: string;
      project?: string;
    }
  ): Promise<Array<{
    email: string;
    status: 'success' | 'already_member' | 'invitation_sent' | 'failed';
    message: string;
    error?: string;
  }>> {
    try {
      // Convert to invitation requests
      const invitationRequests: InvitationRequest[] = request.emails.map(email => ({
        email,
        displayName: email.split('@')[0],
        resourceUrl: libraryUrl,
        permission: this.mapToSharedPermission(request.permission),
        metadata: {
          company: request.company,
          project: request.project
        }
      }));

      // Delegate to shared service
      const bulkResult = await this.userService.bulkInviteUsers(invitationRequests);

      // Convert results back to SPFx format
      return bulkResult.results.map(result => ({
        email: result.item.email,
        status: result.success ? 'invitation_sent' : 'failed',
        message: result.success ? 'User invited successfully' : 'Failed to invite user',
        error: result.error
      }));
    } catch (error: any) {
      throw new Error(`Bulk add failed: ${error.message}`);
    }
  }

  /**
   * Search for users
   * Note: This is a simplified mock implementation for demonstration
   * In production, this should use Microsoft Graph People Picker API or similar
   */
  public async searchUsers(query: string): Promise<IExternalUser[]> {
    // Simple email validation regex
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    
    if (!query || !emailRegex.test(query)) {
      return [];
    }

    // Mock implementation - in production, this would query Graph API
    // TODO: Replace with actual Graph API people picker when migrating
    return [{
      id: 'search-result',
      email: query,
      displayName: query.split('@')[0],
      invitedBy: this.context.pageContext.user.displayName,
      invitedDate: new Date(),
      lastAccess: new Date(),
      permissions: 'Read'
    }];
  }

  // Helper methods for model conversion

  /**
   * Map SPFx permission to shared permission level
   */
  private mapToSharedPermission(permission: 'Read' | 'Contribute' | 'Full Control'): PermissionLevel {
    switch (permission) {
      case 'Read':
        return 'Read';
      case 'Contribute':
        return 'Contribute';
      case 'Full Control':
        return 'FullControl';
      default:
        return 'Read';
    }
  }

  /**
   * Map shared permission level to SPFx permission
   */
  private mapPermissionLevel(permission: PermissionLevel): 'Read' | 'Contribute' | 'Full Control' {
    switch (permission) {
      case 'Read':
        return 'Read';
      case 'Contribute':
      case 'Edit':
        return 'Contribute';
      case 'FullControl':
        return 'Full Control';
      default:
        return 'Read';
    }
  }
}

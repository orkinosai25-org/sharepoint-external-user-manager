import { WebPartContext } from '@microsoft/sp-webpart-base';
import { IExternalUser } from '../models/IExternalLibrary';
import { AuditLogger } from './AuditLogger';
import { SaaSApiClient } from '../../../shared/services/SaaSApiClient';

/**
 * Backend API Service for External User Management
 * 
 * This service connects the SPFx UI to the SaaS backend API.
 * All external user operations go through the backend API endpoints.
 * 
 * Note: Now uses the shared SaaSApiClient for consistent authentication
 * and error handling across all webparts.
 */
export class BackendApiService {
  private context: WebPartContext;
  private auditLogger: AuditLogger;
  private apiClient: SaaSApiClient;

  constructor(context: WebPartContext, backendUrl?: string) {
    this.context = context;
    this.auditLogger = new AuditLogger(context);
    this.apiClient = new SaaSApiClient(context, backendUrl);
  }

  /**
   * List external users for a specific library
   * Maps to: GET /api/external-users?library={libraryUrl}
   */
  public async listExternalUsers(libraryUrl: string): Promise<IExternalUser[]> {
    try {
      this.auditLogger.logInfo('listExternalUsers', `Fetching external users for library: ${libraryUrl}`);

      const params = new URLSearchParams({
        library: libraryUrl
      });

      const response = await this.apiClient.request<any>(
        `/external-users?${params.toString()}`,
        'GET'
      );

      // Transform backend response to UI model
      const users: IExternalUser[] = (response.items || response || []).map((user: any) => ({
        id: user.id,
        email: user.email,
        displayName: user.displayName || user.email,
        invitedBy: user.invitedBy || 'System',
        invitedDate: new Date(user.invitedDate),
        lastAccess: user.lastAccess ? new Date(user.lastAccess) : new Date(),
        permissions: this.mapBackendPermissionToUI(user.permissions),
        company: user.metadata?.company,
        project: user.metadata?.project
      }));

      this.auditLogger.logInfo('listExternalUsers', `Retrieved ${users.length} external users`);
      return users;
    } catch (error) {
      this.auditLogger.logError('listExternalUsers', 'Failed to list external users', error);
      throw new Error(`Failed to load external users: ${error.message}`);
    }
  }

  /**
   * Add external user to a library
   * Maps to: POST /api/external-users
   */
  public async addExternalUser(
    libraryUrl: string,
    email: string,
    permission: 'Read' | 'Edit',
    company?: string,
    project?: string
  ): Promise<void> {
    try {
      this.auditLogger.logInfo('addExternalUser', `Adding user ${email} to library ${libraryUrl}`, {
        email,
        permission,
        company,
        project
      });

      const requestBody = {
        email,
        displayName: email.split('@')[0], // Use email prefix as display name
        library: libraryUrl,
        permissions: this.mapUIPermissionToBackend(permission),
        message: `You have been invited to access the SharePoint library.`,
        metadata: {
          company,
          project
        }
      };

      await this.apiClient.request(
        '/external-users',
        'POST',
        requestBody
      );

      this.auditLogger.logInfo('addExternalUser', `Successfully added user ${email}`);
    } catch (error) {
      this.auditLogger.logError('addExternalUser', `Failed to add user ${email}`, error);
      throw new Error(`Failed to add user: ${error.message}`);
    }
  }

  /**
   * Remove external user from a library
   * Maps to: DELETE /api/external-users
   */
  public async removeExternalUser(
    libraryUrl: string,
    email: string
  ): Promise<void> {
    try {
      this.auditLogger.logInfo('removeExternalUser', `Removing user ${email} from library ${libraryUrl}`);

      const requestBody = {
        email,
        library: libraryUrl
      };

      await this.apiClient.request(
        '/external-users',
        'DELETE',
        requestBody
      );

      this.auditLogger.logInfo('removeExternalUser', `Successfully removed user ${email}`);
    } catch (error) {
      this.auditLogger.logError('removeExternalUser', `Failed to remove user ${email}`, error);
      throw new Error(`Failed to remove user: ${error.message}`);
    }
  }

  /**
   * Map UI permission (Read/Edit) to backend permission level
   */
  private mapUIPermissionToBackend(permission: 'Read' | 'Edit'): 'Read' | 'Contribute' {
    switch (permission) {
      case 'Read':
        return 'Read';
      case 'Edit':
        return 'Contribute';
      default:
        return 'Read';
    }
  }

  /**
   * Map backend permission level to UI permission (Read/Edit)
   * Backend uses: Read, Contribute, Edit, FullControl
   * UI simplified to: Read, Edit
   */
  private mapBackendPermissionToUI(permission: string): 'Read' | 'Contribute' | 'Full Control' {
    // Note: Returning old model format for compatibility with existing IExternalUser interface
    // which is used throughout the codebase. Display layer handles Read/Edit only.
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

  /**
   * Bulk add external users to a library
   */
  public async bulkAddExternalUsers(
    libraryUrl: string,
    emails: string[],
    permission: 'Read' | 'Edit',
    company?: string,
    project?: string
  ): Promise<any[]> {
    try {
      this.auditLogger.logInfo('bulkAddExternalUsers', `Bulk adding ${emails.length} users to library ${libraryUrl}`);

      // Backend doesn't have a dedicated bulk endpoint, so we'll call add user for each email
      const results = await Promise.all(
        emails.map(async (email) => {
          try {
            await this.addExternalUser(libraryUrl, email, permission, company, project);
            return {
              email,
              status: 'success',
              message: 'User added successfully'
            };
          } catch (error) {
            return {
              email,
              status: 'failed',
              message: error.message
            };
          }
        })
      );

      this.auditLogger.logInfo('bulkAddExternalUsers', `Bulk operation completed. Success: ${results.filter(r => r.status === 'success').length}/${emails.length}`);
      
      return results;
    } catch (error) {
      this.auditLogger.logError('bulkAddExternalUsers', 'Bulk add operation failed', error);
      throw new Error(`Bulk add operation failed: ${error.message}`);
    }
  }
}

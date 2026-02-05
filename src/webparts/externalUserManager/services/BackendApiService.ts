import { WebPartContext } from '@microsoft/sp-webpart-base';
import { IExternalUser } from '../models/IExternalLibrary';
import { AuditLogger } from './AuditLogger';

/**
 * Backend API Service for External User Management
 * 
 * This service connects the SPFx UI to the SaaS backend API.
 * All external user operations go through the backend API endpoints.
 */
export class BackendApiService {
  private context: WebPartContext;
  private auditLogger: AuditLogger;
  private backendUrl: string;

  constructor(context: WebPartContext, backendUrl?: string) {
    this.context = context;
    this.auditLogger = new AuditLogger(context);
    
    // Use provided backend URL or default to localhost for development
    this.backendUrl = backendUrl || 'http://localhost:7071/api';
  }

  /**
   * Get access token for backend API authentication
   */
  private async getAccessToken(): Promise<string> {
    try {
      // Use the SPFx context to get an access token for the backend API
      // This assumes the backend is registered as an Azure AD application
      const tokenProvider = await this.context.aadTokenProviderFactory.getTokenProvider();
      
      // For now, use a placeholder token. In production, this should be configured
      // with the actual backend API application ID
      const token = await tokenProvider.getToken('https://graph.microsoft.com');
      
      return token;
    } catch (error) {
      this.auditLogger.logError('getAccessToken', 'Failed to get access token', error);
      throw new Error('Authentication failed. Please ensure you are signed in.');
    }
  }

  /**
   * Make authenticated API request to backend
   */
  private async apiRequest<T>(
    endpoint: string,
    method: 'GET' | 'POST' | 'DELETE' = 'GET',
    body?: any
  ): Promise<T> {
    try {
      const token = await this.getAccessToken();
      
      const headers: HeadersInit = {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      };

      const requestInit: RequestInit = {
        method,
        headers
      };

      if (body) {
        requestInit.body = JSON.stringify(body);
      }

      const url = `${this.backendUrl}${endpoint}`;
      this.auditLogger.logInfo('apiRequest', `Making ${method} request to ${url}`);

      const response = await fetch(url, requestInit);

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: response.statusText }));
        throw new Error(errorData.message || `API request failed with status ${response.status}`);
      }

      const data = await response.json();
      
      // Backend returns responses in format: { success: true, data: {...}, meta: {...} }
      if (data.success && data.data) {
        return data.data;
      }
      
      return data;
    } catch (error) {
      this.auditLogger.logError('apiRequest', `API request failed: ${endpoint}`, error);
      
      // Provide user-friendly error messages
      if (error.message.includes('Failed to fetch')) {
        throw new Error('Unable to connect to the backend service. Please check your network connection or contact your administrator.');
      }
      
      throw error;
    }
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

      const response = await this.apiRequest<any>(
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

      await this.apiRequest(
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

      await this.apiRequest(
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

/**
 * Core Library Service Implementation
 * 
 * Handles SharePoint document library management operations
 */

import {
  ILibraryService,
  IGraphClient,
  IAuditService
} from '../interfaces';
import {
  SharePointLibrary,
  LibraryCreationRequest
} from '../models';

export class LibraryService implements ILibraryService {
  constructor(
    private graphClient: IGraphClient,
    private auditService?: IAuditService
  ) {}

  /**
   * List all document libraries in a site
   */
  async listLibraries(siteId: string): Promise<SharePointLibrary[]> {
    try {
      this.auditService?.logInfo('listLibraries', `Fetching libraries for site: ${siteId}`);

      // Get drives (document libraries) from the site
      const response = await this.graphClient.request<any>(
        `/sites/${siteId}/drives`,
        'GET'
      );

      const libraries: SharePointLibrary[] = (response.value || []).map((drive: any) => ({
        id: drive.id,
        name: drive.name,
        description: drive.description || '',
        url: drive.webUrl,
        siteId: siteId,
        externalUserCount: 0, // Would require additional API call to calculate
        lastModified: new Date(drive.lastModifiedDateTime),
        owner: drive.owner?.user?.displayName || 'Unknown',
        itemCount: drive.quota?.total || 0
      }));

      this.auditService?.logInfo('listLibraries', `Found ${libraries.length} libraries`);
      return libraries;
    } catch (error: any) {
      this.auditService?.logError('listLibraries', `Failed to list libraries for site: ${siteId}`, error);
      throw new Error(`Failed to list libraries: ${error.message}`);
    }
  }

  /**
   * Get a specific library by ID
   */
  async getLibrary(siteId: string, libraryId: string): Promise<SharePointLibrary> {
    try {
      this.auditService?.logInfo('getLibrary', `Fetching library: ${libraryId}`);

      const drive = await this.graphClient.request<any>(
        `/sites/${siteId}/drives/${libraryId}`,
        'GET'
      );

      const library: SharePointLibrary = {
        id: drive.id,
        name: drive.name,
        description: drive.description || '',
        url: drive.webUrl,
        siteId: siteId,
        externalUserCount: 0,
        lastModified: new Date(drive.lastModifiedDateTime),
        owner: drive.owner?.user?.displayName || 'Unknown',
        itemCount: drive.quota?.total || 0
      };

      this.auditService?.logInfo('getLibrary', `Successfully retrieved library: ${libraryId}`);
      return library;
    } catch (error: any) {
      this.auditService?.logError('getLibrary', `Failed to get library: ${libraryId}`, error);
      throw new Error(`Failed to get library: ${error.message}`);
    }
  }

  /**
   * Create a new document library
   */
  async createLibrary(request: LibraryCreationRequest): Promise<SharePointLibrary> {
    try {
      this.auditService?.logInfo('createLibrary', `Creating library: ${request.name}`, request);

      // Create library using Graph API
      const response = await this.graphClient.request<any>(
        `/sites/${request.siteId}/lists`,
        'POST',
        {
          displayName: request.name,
          description: request.description || '',
          list: {
            template: request.template || 'documentLibrary'
          }
        }
      );

      const library: SharePointLibrary = {
        id: response.id,
        name: response.displayName || request.name,
        description: response.description || '',
        url: response.webUrl,
        siteId: request.siteId,
        externalUserCount: 0,
        lastModified: new Date(response.lastModifiedDateTime),
        owner: 'Current User',
        itemCount: 0
      };

      // Enable external sharing if requested
      if (request.enableExternalSharing) {
        await this.enableExternalSharing(request.siteId, library.id);
      }

      this.auditService?.logInfo('createLibrary', `Successfully created library: ${request.name}`);
      return library;
    } catch (error: any) {
      this.auditService?.logError('createLibrary', `Failed to create library: ${request.name}`, error);
      throw new Error(`Failed to create library: ${error.message}`);
    }
  }

  /**
   * Delete a document library
   */
  async deleteLibrary(siteId: string, libraryId: string): Promise<void> {
    try {
      this.auditService?.logInfo('deleteLibrary', `Deleting library: ${libraryId}`);

      await this.graphClient.request(
        `/sites/${siteId}/lists/${libraryId}`,
        'DELETE'
      );

      this.auditService?.logInfo('deleteLibrary', `Successfully deleted library: ${libraryId}`);
    } catch (error: any) {
      this.auditService?.logError('deleteLibrary', `Failed to delete library: ${libraryId}`, error);
      throw new Error(`Failed to delete library: ${error.message}`);
    }
  }

  /**
   * Update library settings
   */
  async updateLibrary(
    siteId: string,
    libraryId: string,
    settings: Partial<SharePointLibrary>
  ): Promise<SharePointLibrary> {
    try {
      this.auditService?.logInfo('updateLibrary', `Updating library: ${libraryId}`, settings);

      const updateData: any = {};
      if (settings.name) updateData.displayName = settings.name;
      if (settings.description !== undefined) updateData.description = settings.description;

      const response = await this.graphClient.request<any>(
        `/sites/${siteId}/lists/${libraryId}`,
        'PATCH',
        updateData
      );

      const library: SharePointLibrary = {
        id: response.id,
        name: response.displayName,
        description: response.description || '',
        url: response.webUrl,
        siteId: siteId,
        externalUserCount: settings.externalUserCount || 0,
        lastModified: new Date(response.lastModifiedDateTime),
        owner: settings.owner || 'Unknown',
        itemCount: settings.itemCount || 0
      };

      this.auditService?.logInfo('updateLibrary', `Successfully updated library: ${libraryId}`);
      return library;
    } catch (error: any) {
      this.auditService?.logError('updateLibrary', `Failed to update library: ${libraryId}`, error);
      throw new Error(`Failed to update library: ${error.message}`);
    }
  }

  // Helper methods

  /**
   * Enable external sharing for a library
   */
  private async enableExternalSharing(_siteId: string, libraryId: string): Promise<void> {
    try {
      this.auditService?.logInfo('enableExternalSharing', 
        `Enabling external sharing for library: ${libraryId}`);

      // Note: External sharing configuration typically requires tenant admin permissions
      // This is a placeholder for the actual implementation
      // In production, this would use SharePoint Admin APIs

      this.auditService?.logInfo('enableExternalSharing', 
        `External sharing enablement for library ${libraryId} requires tenant admin configuration`);
    } catch (error: any) {
      this.auditService?.logError('enableExternalSharing', 
        `Failed to enable external sharing for library: ${libraryId}`, error);
      // Don't throw - this is a non-critical operation
    }
  }
}

import { WebPartContext } from '@microsoft/sp-webpart-base';
import "@pnp/sp/webs";
import "@pnp/sp/lists";
import "@pnp/sp/items";
import "@pnp/sp/security";
import { IExternalLibrary, IExternalUser } from '../models/IExternalLibrary';
/**
 * SharePoint Data Service using PnPjs for library management
 *
 * Technical Decisions:
 * - PnPjs chosen for primary integration due to:
 *   * Better TypeScript support and intellisense
 *   * Simplified API compared to raw REST calls
 *   * Built-in error handling and retry logic
 *   * Better caching and performance optimizations
 *
 * - Microsoft Graph API used as fallback for:
 *   * Tenant-level operations not supported by PnPjs
 *   * Cross-site operations requiring elevated permissions
 *   * Advanced user management scenarios
 */
export declare class SharePointDataService {
    private context;
    private auditLogger;
    private sp;
    constructor(context: WebPartContext);
    /**
     * Get all external libraries (document libraries with external sharing enabled)
     */
    getExternalLibraries(): Promise<IExternalLibrary[]>;
    /**
     * Create a new document library with specified configuration
     */
    createLibrary(libraryConfig: {
        title: string;
        description?: string;
        enableExternalSharing?: boolean;
        template?: number;
    }): Promise<IExternalLibrary>;
    /**
     * Delete a document library by ID
     */
    deleteLibrary(libraryId: string): Promise<void>;
    /**
     * Get external users for a specific library
     */
    getExternalUsersForLibrary(libraryId: string): Promise<IExternalUser[]>;
    /**
     * Add external user to a library with specified permissions
     */
    addExternalUserToLibrary(libraryId: string, email: string, permission: 'Read' | 'Contribute' | 'Full Control', company?: string, project?: string): Promise<void>;
    /**
  
     * Remove external user from a library
     */
    removeExternalUserFromLibrary(libraryId: string, userId: string): Promise<void>;
    /**
  
     * Update user metadata (company and project) - public method
     */
    updateUserMetadata(libraryId: string, userId: string, company: string, project: string): Promise<void>;
    /**
     * Store user metadata (company and project) in a SharePoint list
     */
    private storeUserMetadata;
    /**
     * Retrieve user metadata (company and project)
     */
    private getUserMetadata;
    /**
     * Bulk add external users to a library with specified permissions
     */
    bulkAddExternalUsersToLibrary(libraryId: string, request: {
        emails: string[];
        permission: 'Read' | 'Contribute' | 'Full Control';
        company?: string;
        project?: string;
    }): Promise<{
        email: string;
        status: 'success' | 'already_member' | 'invitation_sent' | 'failed';
        message: string;
        error?: string;
    }[]>;
    /**
     * Search for users in the tenant (for adding external users)
     */
    searchUsers(query: string): Promise<IExternalUser[]>;
    /**
     * Get role definition ID for a permission level
     */
    private getRoleDefinitionId;
    /**
     * Check if a user is external (not from the same tenant)
     */
    private isExternalUser;
    private hasExternalUsers;
    private getLibraryOwner;
    private getLibraryPermissionLevel;
    private enableExternalSharing;
    private mapPermissionLevel;
}
//# sourceMappingURL=SharePointDataService.d.ts.map
import { WebPartContext } from '@microsoft/sp-webpart-base';
import { IExternalUser } from '../models/IExternalLibrary';
/**
 * Backend API Service for External User Management
 *
 * This service connects the SPFx UI to the SaaS backend API.
 * All external user operations go through the backend API endpoints.
 *
 * Note: Now uses the shared SaaSApiClient for consistent authentication
 * and error handling across all webparts.
 */
export declare class BackendApiService {
    private context;
    private auditLogger;
    private apiClient;
    constructor(context: WebPartContext, backendUrl?: string);
    /**
     * List external users for a specific library
     * Maps to: GET /api/external-users?library={libraryUrl}
     */
    listExternalUsers(libraryUrl: string): Promise<IExternalUser[]>;
    /**
     * Add external user to a library
     * Maps to: POST /api/external-users
     */
    addExternalUser(libraryUrl: string, email: string, permission: 'Read' | 'Edit', company?: string, project?: string): Promise<void>;
    /**
     * Remove external user from a library
     * Maps to: DELETE /api/external-users
     */
    removeExternalUser(libraryUrl: string, email: string): Promise<void>;
    /**
     * Map UI permission (Read/Edit) to backend permission level
     */
    private mapUIPermissionToBackend;
    /**
     * Map backend permission level to UI permission (Read/Edit)
     * Backend uses: Read, Contribute, Edit, FullControl
     * UI simplified to: Read, Edit
     */
    private mapBackendPermissionToUI;
    /**
     * Bulk add external users to a library
     */
    bulkAddExternalUsers(libraryUrl: string, emails: string[], permission: 'Read' | 'Edit', company?: string, project?: string): Promise<any[]>;
}
//# sourceMappingURL=BackendApiService.d.ts.map
import { WebPartContext } from '@microsoft/sp-webpart-base';
/**
 * Microsoft Graph API Service for SharePoint library management
 *
 * Used as fallback when PnPjs doesn't support specific operations:
 * - Tenant-level operations
 * - Cross-site operations requiring elevated permissions
 * - Advanced user management scenarios
 * - External sharing configuration
 */
export declare class GraphApiService {
    private context;
    private auditLogger;
    constructor(context: WebPartContext);
    /**
     * Get Microsoft Graph client
     */
    private getGraphClient;
    /**
     * Enable external sharing for a site using Graph API
     * This requires tenant admin permissions
     */
    enableExternalSharingForSite(siteId: string): Promise<void>;
    /**
     * Get external users for a specific site
     * Uses Graph API to get more detailed user information
     */
    getExternalUsersForSite(siteId: string): Promise<any[]>;
    /**
     * Create sharing link for a library
     * Uses Graph API for advanced sharing configuration
     */
    createSharingLink(siteId: string, listId: string, options: {
        type: 'view' | 'edit';
        scope: 'anonymous' | 'organization' | 'users';
        expirationDateTime?: Date;
        password?: string;
    }): Promise<string>;
    /**
     * Invite external users to a library
     * Uses Graph API for advanced invitation features
     */
    inviteExternalUsers(siteId: string, listId: string, invitations: {
        email: string;
        displayName?: string;
        role: 'read' | 'write' | 'owner';
        message?: string;
    }[]): Promise<void>;
    /**
     * Get site information including sharing settings
     */
    getSiteInfo(siteId: string): Promise<any>;
    /**
     * Revoke external user access
     * Uses Graph API for advanced permission management
     */
    revokeExternalUserAccess(siteId: string, permissionId: string): Promise<void>;
    /**
     * Get sharing analytics for a site
     * Provides insights into external sharing patterns
     */
    getSharingAnalytics(siteId: string, days?: number): Promise<any>;
}
//# sourceMappingURL=GraphApiService.d.ts.map
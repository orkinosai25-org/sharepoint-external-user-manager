import { WebPartContext } from '@microsoft/sp-webpart-base';
/**
 * Tenant status information from the SaaS backend
 */
export interface ITenantStatus {
    tenantId: string;
    isActive: boolean;
    subscriptionTier: string;
}
/**
 * Subscription status information
 */
export interface ISubscriptionStatus {
    tier: string;
    status: string;
    isActive: boolean;
    startDate?: Date;
    endDate?: Date;
    trialExpiry?: Date;
    limits: {
        maxClients: number;
        maxExternalUsersPerClient: number;
        maxLibrariesPerClient: number;
        maxListsPerClient: number;
    };
    features: string[];
}
/**
 * Shared SaaS API Client for all SPFx webparts
 *
 * This service provides:
 * - Centralised authentication with the SaaS backend
 * - Tenant and subscription status checks
 * - User-friendly error handling
 * - Consistent API request pattern
 *
 * All SPFx webparts should use this client instead of direct Graph API calls.
 */
export declare class SaaSApiClient {
    private context;
    private backendUrl;
    private tenantStatusCache;
    private subscriptionStatusCache;
    private cacheTTL;
    private lastCacheTime;
    constructor(context: WebPartContext, backendUrl?: string);
    /**
     * Get access token for backend API authentication
     *
     * In production, this should use the backend API's Azure AD application ID
     * For now, it uses the Graph API token as a placeholder
     */
    private getAccessToken;
    /**
     * Make authenticated API request to the SaaS backend
     *
     * @param endpoint - API endpoint path (e.g., '/clients')
     * @param method - HTTP method
     * @param body - Request body for POST/PUT/PATCH
     * @returns Response data
     */
    request<T>(endpoint: string, method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE', body?: any): Promise<T>;
    /**
     * Check if the tenant is connected and active
     *
     * This should be called when a webpart loads to verify the tenant
     * has been onboarded to the SaaS platform.
     *
     * @returns Tenant status information
     */
    checkTenantStatus(): Promise<ITenantStatus>;
    /**
     * Get the current subscription status
     *
     * This includes the subscription tier, limits, and features available.
     * Use this to implement feature gating in the UI.
     *
     * @returns Subscription status with limits and features
     */
    getSubscriptionStatus(): Promise<ISubscriptionStatus>;
    /**
     * Clear the cache (useful when subscription changes)
     */
    clearCache(): void;
    /**
     * Get the backend URL being used
     */
    getBackendUrl(): string;
}
//# sourceMappingURL=SaaSApiClient.d.ts.map
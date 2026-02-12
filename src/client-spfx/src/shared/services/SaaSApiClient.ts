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
export class SaaSApiClient {
  private context: WebPartContext;
  private backendUrl: string;
  private tenantStatusCache: ITenantStatus | null = null;
  private subscriptionStatusCache: ISubscriptionStatus | null = null;
  private cacheTTL: number = 5 * 60 * 1000; // 5 minutes
  private lastCacheTime: number = 0;

  constructor(context: WebPartContext, backendUrl?: string) {
    this.context = context;
    // Use provided backend URL or default to localhost for development
    this.backendUrl = backendUrl || 'http://localhost:5000/api';
  }

  /**
   * Get access token for backend API authentication
   * 
   * In production, this should use the backend API's Azure AD application ID
   * For now, it uses the Graph API token as a placeholder
   */
  private async getAccessToken(): Promise<string> {
    try {
      const tokenProvider = await this.context.aadTokenProviderFactory.getTokenProvider();
      
      // TODO: Replace with actual backend API scope/resource ID
      // Example: const token = await tokenProvider.getToken('api://your-backend-app-id');
      const token = await tokenProvider.getToken('https://graph.microsoft.com');
      
      return token;
    } catch (error) {
      throw new Error('Authentication failed. Please ensure you are signed in.');
    }
  }

  /**
   * Make authenticated API request to the SaaS backend
   * 
   * @param endpoint - API endpoint path (e.g., '/clients')
   * @param method - HTTP method
   * @param body - Request body for POST/PUT/PATCH
   * @returns Response data
   */
  public async request<T>(
    endpoint: string,
    method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE' = 'GET',
    body?: any
  ): Promise<T> {
    try {
      const token = await this.getAccessToken();
      
      const headers: HeadersInit = {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      };

      const requestInit: RequestInit = {
        method,
        headers
      };

      if (body && method !== 'GET') {
        requestInit.body = JSON.stringify(body);
      }

      const url = `${this.backendUrl}${endpoint}`;
      const response = await fetch(url, requestInit);

      if (!response.ok) {
        // Handle specific error status codes
        if (response.status === 401) {
          throw new Error('Authentication failed. Please sign in again.');
        }
        if (response.status === 403) {
          throw new Error('Access denied. You do not have permission to perform this action.');
        }
        if (response.status === 404) {
          throw new Error('Resource not found. Please check the request and try again.');
        }
        if (response.status === 429) {
          throw new Error('Too many requests. Please wait a moment and try again.');
        }
        if (response.status >= 500) {
          throw new Error('Service temporarily unavailable. Please try again later.');
        }

        // Try to get error message from response
        const errorData = await response.json().catch(() => ({ message: response.statusText }));
        throw new Error(errorData.message || `Request failed with status ${response.status}`);
      }

      const data = await response.json();
      
      // Backend returns responses in format: { success: true, data: {...} }
      if (data.success !== undefined && data.data !== undefined) {
        return data.data;
      }
      
      return data;
    } catch (error) {
      // Provide user-friendly error messages
      if (error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
        throw new Error('Unable to connect to the service. Please check your network connection or contact your administrator.');
      }
      
      throw error;
    }
  }

  /**
   * Check if the tenant is connected and active
   * 
   * This should be called when a webpart loads to verify the tenant
   * has been onboarded to the SaaS platform.
   * 
   * @returns Tenant status information
   */
  public async checkTenantStatus(): Promise<ITenantStatus> {
    // Check cache first
    const now = Date.now();
    if (this.tenantStatusCache && (now - this.lastCacheTime) < this.cacheTTL) {
      return this.tenantStatusCache;
    }

    try {
      const response = await this.request<any>('/tenants/me', 'GET');
      
      const status: ITenantStatus = {
        tenantId: response.tenantId,
        isActive: response.isActive || false,
        subscriptionTier: response.subscriptionTier || 'Free'
      };

      // Update cache
      this.tenantStatusCache = status;
      this.lastCacheTime = now;

      return status;
    } catch (error) {
      throw new Error(`Failed to check tenant status: ${error.message}`);
    }
  }

  /**
   * Get the current subscription status
   * 
   * This includes the subscription tier, limits, and features available.
   * Use this to implement feature gating in the UI.
   * 
   * @returns Subscription status with limits and features
   */
  public async getSubscriptionStatus(): Promise<ISubscriptionStatus> {
    // Check cache first
    const now = Date.now();
    if (this.subscriptionStatusCache && (now - this.lastCacheTime) < this.cacheTTL) {
      return this.subscriptionStatusCache;
    }

    try {
      const response = await this.request<any>('/billing/subscription/status', 'GET');
      
      const status: ISubscriptionStatus = {
        tier: response.tier || 'Starter',
        status: response.status || 'None',
        isActive: response.isActive || false,
        startDate: response.startDate ? new Date(response.startDate) : undefined,
        endDate: response.endDate ? new Date(response.endDate) : undefined,
        trialExpiry: response.trialExpiry ? new Date(response.trialExpiry) : undefined,
        limits: response.limits || {
          maxClients: 1,
          maxExternalUsersPerClient: 5,
          maxLibrariesPerClient: 2,
          maxListsPerClient: 2
        },
        features: response.features || []
      };

      // Update cache
      this.subscriptionStatusCache = status;
      this.lastCacheTime = now;

      return status;
    } catch (error) {
      throw new Error(`Failed to get subscription status: ${error.message}`);
    }
  }

  /**
   * Clear the cache (useful when subscription changes)
   */
  public clearCache(): void {
    this.tenantStatusCache = null;
    this.subscriptionStatusCache = null;
    this.lastCacheTime = 0;
  }

  /**
   * Get the backend URL being used
   */
  public getBackendUrl(): string {
    return this.backendUrl;
  }
}

/**
 * Graph API Client Interface
 * 
 * Abstract interface for Microsoft Graph API operations
 * Implementations can use different authentication methods
 */
export interface IGraphClient {
  /**
   * Get authenticated access token
   * @returns Access token for Graph API
   */
  getAccessToken(): Promise<string>;
  
  /**
   * Make a Graph API request
   * @param endpoint - API endpoint (e.g., '/sites/{id}')
   * @param method - HTTP method
   * @param body - Request body
   * @returns Response data
   */
  request<T>(
    endpoint: string,
    method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE',
    body?: any
  ): Promise<T>;
}

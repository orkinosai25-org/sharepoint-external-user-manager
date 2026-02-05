/**
 * Service to fetch client data from the SaaS backend API
 */

import { WebPartContext } from '@microsoft/sp-webpart-base';
import { IClient } from '../models/IClient';

export class ClientDataService {
  private context: WebPartContext;
  private baseUrl: string;

  constructor(context: WebPartContext) {
    this.context = context;
    // In production, this should come from environment config or web part properties
    this.baseUrl = process.env.BACKEND_API_URL || 'https://your-backend-api.azurewebsites.net/api';
  }

  /**
   * Get all clients for the current tenant
   */
  public async getClients(): Promise<IClient[]> {
    try {
      // Get the current user's access token
      const token = await this.getAccessToken();
      
      const response = await fetch(`${this.baseUrl}/clients`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch clients: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      return data as IClient[];
    } catch (error) {
      console.error('Error fetching clients:', error);
      throw error;
    }
  }

  /**
   * Get access token for API authentication
   * In a real implementation, this would use MSAL or AAD authentication
   */
  private async getAccessToken(): Promise<string> {
    try {
      // For MVP, try to get from AAD token provider
      // In production, implement proper MSAL authentication
      const tokenProvider = await this.context.aadTokenProviderFactory.getTokenProvider();
      const token = await tokenProvider.getToken('api://your-api-client-id');
      return token;
    } catch (error) {
      console.warn('Unable to get AAD token, API calls may fail:', error);
      // Return empty string for now, will be handled by mock service fallback
      return '';
    }
  }
}

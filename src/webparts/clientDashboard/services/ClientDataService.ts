/**
 * Service to fetch client data from the SaaS backend API
 */

import { WebPartContext } from '@microsoft/sp-webpart-base';
import { IClient } from '../models/IClient';
import { ILibrary, IList, IExternalUser } from '../models/IClientDetail';

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
   * Create a new client
   */
  public async createClient(clientName: string): Promise<IClient> {
    try {
      // Get the current user's access token
      const token = await this.getAccessToken();
      
      const response = await fetch(`${this.baseUrl}/clients`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify({
          clientName: clientName
        })
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to create client: ${response.status} ${response.statusText}. ${errorText}`);
      }

      const data = await response.json();
      return data as IClient;
    } catch (error) {
      console.error('Error creating client:', error);
      throw error;
    }
  }

  /**
   * Get a single client by ID
   */
  public async getClient(clientId: number): Promise<IClient> {
    try {
      // Get the current user's access token
      const token = await this.getAccessToken();
      
      const response = await fetch(`${this.baseUrl}/clients/${clientId}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch client: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      return data as IClient;
    } catch (error) {
      console.error('Error fetching client:', error);
      throw error;
    }
  }

  /**
   * Get libraries for a specific client
   */
  public async getClientLibraries(clientId: number): Promise<ILibrary[]> {
    try {
      const token = await this.getAccessToken();
      
      const response = await fetch(`${this.baseUrl}/clients/${clientId}/libraries`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch libraries: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      return data as ILibrary[];
    } catch (error) {
      console.error('Error fetching client libraries:', error);
      throw error;
    }
  }

  /**
   * Get lists for a specific client
   */
  public async getClientLists(clientId: number): Promise<IList[]> {
    try {
      const token = await this.getAccessToken();
      
      const response = await fetch(`${this.baseUrl}/clients/${clientId}/lists`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch lists: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      return data as IList[];
    } catch (error) {
      console.error('Error fetching client lists:', error);
      throw error;
    }
  }

  /**
   * Get external users for a specific client
   */
  public async getClientExternalUsers(clientId: number): Promise<IExternalUser[]> {
    try {
      const token = await this.getAccessToken();
      
      // Note: The backend endpoint is /external-users with filtering
      // In a real implementation, we would filter by client's libraries
      const response = await fetch(`${this.baseUrl}/external-users?clientId=${clientId}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch external users: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      return data as IExternalUser[];
    } catch (error) {
      console.error('Error fetching external users:', error);
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

import { WebPartContext } from '@microsoft/sp-webpart-base';
import { IClient } from '../models/IClient';

/**
 * Service to interact with the SaaS backend API for client management
 */
export class ClientApiService {
  private context: WebPartContext;
  private apiBaseUrl: string;

  constructor(context: WebPartContext) {
    this.context = context;
    
    // In production, this should come from web part properties or environment config
    // For now, we'll use a default that can be overridden
    this.apiBaseUrl = window.location.hostname === 'localhost' 
      ? 'http://localhost:7071/api'  // Local development
      : '/api';  // Production - assumes API is on same domain or proxied
  }

  /**
   * Get all clients for the current tenant
   */
  public async getClients(): Promise<IClient[]> {
    try {
      const response = await fetch(`${this.apiBaseUrl}/clients`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          // In production, add authentication token here
          // 'Authorization': `Bearer ${await this.getAccessToken()}`
        }
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch clients: ${response.statusText}`);
      }

      const clients: IClient[] = await response.json();
      return clients;
    } catch (error) {
      console.error('Error fetching clients:', error);
      throw error;
    }
  }

  /**
   * Get a specific client by ID
   */
  public async getClient(clientId: number): Promise<IClient> {
    try {
      const response = await fetch(`${this.apiBaseUrl}/clients/${clientId}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        }
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch client: ${response.statusText}`);
      }

      const client: IClient = await response.json();
      return client;
    } catch (error) {
      console.error('Error fetching client:', error);
      throw error;
    }
  }

  /**
   * Create a new client
   */
  public async createClient(clientName: string): Promise<IClient> {
    try {
      const response = await fetch(`${this.apiBaseUrl}/clients`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ clientName })
      });

      if (!response.ok) {
        throw new Error(`Failed to create client: ${response.statusText}`);
      }

      const client: IClient = await response.json();
      return client;
    } catch (error) {
      console.error('Error creating client:', error);
      throw error;
    }
  }

  // Helper method to get access token (placeholder for production)
  private async getAccessToken(): Promise<string> {
    // In production, use context.aadTokenProviderFactory to get token
    // For now, return empty string
    return '';
  }
}

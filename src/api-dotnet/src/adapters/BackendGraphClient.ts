/**
 * Graph Client Adapter for Backend API
 * 
 * This adapter implements the IGraphClient interface from the shared services layer
 * and bridges it to the backend's Microsoft Graph authentication
 */

import { Client } from '@microsoft/microsoft-graph-client';
import { ClientSecretCredential } from '@azure/identity';
import { config } from '../utils/config';
import { IGraphClient } from '../../../services/interfaces';

export class BackendGraphClient implements IGraphClient {
  private graphClient: Client | null = null;

  /**
   * Get authenticated access token
   */
  async getAccessToken(): Promise<string> {
    try {
      const credential = new ClientSecretCredential(
        config.auth.tenantId,
        config.auth.clientId,
        config.auth.clientSecret
      );

      const token = await credential.getToken('https://graph.microsoft.com/.default');
      return token.token;
    } catch (error: any) {
      throw new Error(`Failed to get access token: ${error.message}`);
    }
  }

  /**
   * Get initialized Graph client
   */
  private async getClient(): Promise<Client> {
    if (this.graphClient) {
      return this.graphClient;
    }

    const credential = new ClientSecretCredential(
      config.auth.tenantId,
      config.auth.clientId,
      config.auth.clientSecret
    );

    this.graphClient = Client.initWithMiddleware({
      authProvider: {
        getAccessToken: async () => {
          const token = await credential.getToken('https://graph.microsoft.com/.default');
          return token.token;
        }
      }
    });

    return this.graphClient;
  }

  /**
   * Make a Graph API request
   */
  async request<T>(
    endpoint: string,
    method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE',
    body?: any
  ): Promise<T> {
    try {
      const client = await this.getClient();
      const api = client.api(endpoint);

      switch (method) {
        case 'GET':
          return await api.get();
        case 'POST':
          return await api.post(body);
        case 'PUT':
          return await api.put(body);
        case 'PATCH':
          return await api.patch(body);
        case 'DELETE':
          return await api.delete();
        default:
          throw new Error(`Unsupported HTTP method: ${method}`);
      }
    } catch (error: any) {
      throw new Error(`Graph API request failed: ${error.message}`);
    }
  }
}

/**
 * SPFx Graph Client Adapter
 * 
 * This adapter implements the IGraphClient interface from the shared services layer
 * and bridges it to SPFx's MSGraphClientV3
 */

import { WebPartContext } from '@microsoft/sp-webpart-base';
import { MSGraphClientV3 } from '@microsoft/sp-http';
import { IGraphClient } from '../../../../services/interfaces';

export class SPFxGraphClient implements IGraphClient {
  private graphClient: MSGraphClientV3 | null = null;

  constructor(private context: WebPartContext) {}

  /**
   * Get authenticated access token
   */
  async getAccessToken(): Promise<string> {
    try {
      const tokenProvider = await this.context.aadTokenProviderFactory.getTokenProvider();
      const token = await tokenProvider.getToken('https://graph.microsoft.com');
      return token;
    } catch (error: any) {
      throw new Error(`Failed to get access token: ${error.message}`);
    }
  }

  /**
   * Get initialized Graph client
   */
  private async getClient(): Promise<MSGraphClientV3> {
    if (this.graphClient) {
      return this.graphClient;
    }

    this.graphClient = await this.context.msGraphClientFactory.getClient('3');
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

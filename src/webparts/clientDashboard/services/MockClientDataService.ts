/**
 * Mock data service for development and testing
 */

import { IClient } from '../models/IClient';

export class MockClientDataService {
  /**
   * Get mock client data
   */
  public static getClients(): IClient[] {
    return [
      {
        id: 1,
        tenantId: 1,
        clientName: 'Acme Corporation',
        siteUrl: 'https://contoso.sharepoint.com/sites/acme-corp',
        siteId: 'acme-corp-site-123',
        createdBy: 'john.doe@lawfirm.com',
        createdAt: '2024-01-15T10:30:00Z',
        status: 'Active'
      },
      {
        id: 2,
        tenantId: 1,
        clientName: 'Global Industries Ltd',
        siteUrl: 'https://contoso.sharepoint.com/sites/global-industries',
        siteId: 'global-ind-site-456',
        createdBy: 'jane.smith@lawfirm.com',
        createdAt: '2024-01-20T14:15:00Z',
        status: 'Active'
      },
      {
        id: 3,
        tenantId: 1,
        clientName: 'Tech Innovations Inc',
        siteUrl: 'https://contoso.sharepoint.com/sites/tech-innovations',
        siteId: 'tech-inn-site-789',
        createdBy: 'john.doe@lawfirm.com',
        createdAt: '2024-02-01T09:45:00Z',
        status: 'Active'
      },
      {
        id: 4,
        tenantId: 1,
        clientName: 'Metro Properties Group',
        siteUrl: 'https://contoso.sharepoint.com/sites/metro-properties',
        siteId: 'metro-prop-site-012',
        createdBy: 'sarah.jones@lawfirm.com',
        createdAt: '2024-02-05T11:20:00Z',
        status: 'Provisioning'
      },
      {
        id: 5,
        tenantId: 1,
        clientName: 'Healthcare Solutions Partners',
        siteUrl: 'https://contoso.sharepoint.com/sites/healthcare-solutions',
        siteId: 'healthcare-site-345',
        createdBy: 'michael.brown@lawfirm.com',
        createdAt: '2024-01-10T16:00:00Z',
        status: 'Active'
      }
    ];
  }
}

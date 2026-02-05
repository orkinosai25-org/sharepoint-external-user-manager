import { IClient } from '../models/IClient';

/**
 * Mock data service for development and testing
 */
export class MockClientDataService {
  /**
   * Get mock clients data
   */
  public static getClients(): IClient[] {
    return [
      {
        id: 1,
        tenantId: 1,
        clientName: 'Acme Corporation',
        siteUrl: 'https://contoso.sharepoint.com/sites/acme-corp',
        siteId: 'abc123-def456-ghi789',
        createdBy: 'admin@contoso.com',
        createdAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 days ago
        status: 'Active'
      },
      {
        id: 2,
        tenantId: 1,
        clientName: 'Smith & Associates',
        siteUrl: 'https://contoso.sharepoint.com/sites/smith-associates',
        siteId: 'xyz789-uvw456-rst123',
        createdBy: 'admin@contoso.com',
        createdAt: new Date(Date.now() - 15 * 24 * 60 * 60 * 1000).toISOString(), // 15 days ago
        status: 'Active'
      },
      {
        id: 3,
        tenantId: 1,
        clientName: 'Johnson Enterprises',
        siteUrl: 'https://contoso.sharepoint.com/sites/johnson-ent',
        siteId: 'mno345-pqr678-stu901',
        createdBy: 'admin@contoso.com',
        createdAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(), // 7 days ago
        status: 'Active'
      },
      {
        id: 4,
        tenantId: 1,
        clientName: 'Global Tech Solutions',
        siteUrl: 'https://contoso.sharepoint.com/sites/globaltech',
        siteId: 'abc987-def654-ghi321',
        createdBy: 'admin@contoso.com',
        createdAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(), // 2 days ago
        status: 'Provisioning'
      },
      {
        id: 5,
        tenantId: 1,
        clientName: 'Metro Properties Inc',
        siteUrl: 'https://contoso.sharepoint.com/sites/metro-properties',
        siteId: 'jkl234-mno567-pqr890',
        createdBy: 'admin@contoso.com',
        createdAt: new Date(Date.now() - 60 * 24 * 60 * 60 * 1000).toISOString(), // 60 days ago
        status: 'Active'
      },
      {
        id: 6,
        tenantId: 1,
        clientName: 'Williams & Co',
        siteUrl: 'https://contoso.sharepoint.com/sites/williams-co',
        siteId: 'error-site-123',
        createdBy: 'admin@contoso.com',
        createdAt: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString(), // 1 day ago
        status: 'Error',
        errorMessage: 'Site creation failed: insufficient permissions'
      }
    ];
  }
}

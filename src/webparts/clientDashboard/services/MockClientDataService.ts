/**
 * Mock data service for development and testing
 */

import { IClient } from '../models/IClient';
import { ILibrary, IList, IExternalUser } from '../models/IClientDetail';

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

  /**
   * Get mock libraries for a client
   */
  public static getClientLibraries(clientId: number): ILibrary[] {
    // Return sample libraries based on client
    return [
      {
        id: `lib-${clientId}-1`,
        name: 'Documents',
        displayName: 'Client Documents',
        description: 'General documents and files for this client',
        webUrl: `https://contoso.sharepoint.com/sites/client-${clientId}/Documents`,
        createdDateTime: '2024-01-15T10:30:00Z',
        lastModifiedDateTime: '2024-02-01T14:20:00Z',
        itemCount: 127
      },
      {
        id: `lib-${clientId}-2`,
        name: 'Contracts',
        displayName: 'Contracts & Agreements',
        description: 'Legal contracts and agreements',
        webUrl: `https://contoso.sharepoint.com/sites/client-${clientId}/Contracts`,
        createdDateTime: '2024-01-15T10:35:00Z',
        lastModifiedDateTime: '2024-02-03T09:15:00Z',
        itemCount: 45
      },
      {
        id: `lib-${clientId}-3`,
        name: 'Evidence',
        displayName: 'Case Evidence',
        description: 'Evidence and supporting documents',
        webUrl: `https://contoso.sharepoint.com/sites/client-${clientId}/Evidence`,
        createdDateTime: '2024-01-16T11:00:00Z',
        lastModifiedDateTime: '2024-02-04T16:45:00Z',
        itemCount: 89
      }
    ];
  }

  /**
   * Get mock lists for a client
   */
  public static getClientLists(clientId: number): IList[] {
    return [
      {
        id: `list-${clientId}-1`,
        name: 'Tasks',
        displayName: 'Project Tasks',
        description: 'Track tasks and deliverables',
        webUrl: `https://contoso.sharepoint.com/sites/client-${clientId}/Lists/Tasks`,
        createdDateTime: '2024-01-15T10:40:00Z',
        lastModifiedDateTime: '2024-02-04T15:30:00Z',
        itemCount: 34,
        listTemplate: 'Tasks'
      },
      {
        id: `list-${clientId}-2`,
        name: 'Contacts',
        displayName: 'Client Contacts',
        description: 'Key contacts for this client',
        webUrl: `https://contoso.sharepoint.com/sites/client-${clientId}/Lists/Contacts`,
        createdDateTime: '2024-01-15T10:45:00Z',
        lastModifiedDateTime: '2024-01-28T11:20:00Z',
        itemCount: 12,
        listTemplate: 'Contacts'
      },
      {
        id: `list-${clientId}-3`,
        name: 'Deadlines',
        displayName: 'Important Deadlines',
        description: 'Track important dates and deadlines',
        webUrl: `https://contoso.sharepoint.com/sites/client-${clientId}/Lists/Deadlines`,
        createdDateTime: '2024-01-16T09:00:00Z',
        lastModifiedDateTime: '2024-02-02T14:00:00Z',
        itemCount: 23,
        listTemplate: 'Events'
      }
    ];
  }

  /**
   * Create a new library (mock implementation)
   */
  public static createLibrary(clientId: number, libraryName: string, description: string): ILibrary {
    return {
      id: `lib-${clientId}-${Date.now()}`,
      name: libraryName.replace(/\s+/g, ''),
      displayName: libraryName,
      description: description,
      webUrl: `https://contoso.sharepoint.com/sites/client-${clientId}/${libraryName.replace(/\s+/g, '')}`,
      createdDateTime: new Date().toISOString(),
      lastModifiedDateTime: new Date().toISOString(),
      itemCount: 0
    };
  }

  /**
   * Create a new list (mock implementation)
   */
  public static createList(clientId: number, listName: string, listType: string, description: string): IList {
    return {
      id: `list-${clientId}-${Date.now()}`,
      name: listName.replace(/\s+/g, ''),
      displayName: listName,
      description: description,
      webUrl: `https://contoso.sharepoint.com/sites/client-${clientId}/Lists/${listName.replace(/\s+/g, '')}`,
      createdDateTime: new Date().toISOString(),
      lastModifiedDateTime: new Date().toISOString(),
      itemCount: 0,
      listTemplate: listType
    };
  }

  /**
   * Get mock external users for a client
   */
  public static getClientExternalUsers(clientId: number): IExternalUser[] {
    return [
      {
        id: `user-${clientId}-1`,
        email: 'john.client@acme.com',
        displayName: 'John Client',
        library: 'Client Documents',
        permissions: 'Read',
        invitedBy: 'attorney@lawfirm.com',
        invitedDate: '2024-01-20T10:00:00Z',
        lastAccess: '2024-02-04T14:30:00Z',
        status: 'Active',
        metadata: {
          company: 'Acme Corporation',
          project: 'General Access',
          department: 'Legal'
        }
      },
      {
        id: `user-${clientId}-2`,
        email: 'jane.external@partner.com',
        displayName: 'Jane External',
        library: 'Contracts & Agreements',
        permissions: 'Contribute',
        invitedBy: 'partner@lawfirm.com',
        invitedDate: '2024-01-22T11:30:00Z',
        lastAccess: '2024-02-03T16:45:00Z',
        status: 'Active',
        metadata: {
          company: 'Partner Firm',
          project: 'Contract Review',
          notes: 'Co-counsel on merger case'
        }
      },
      {
        id: `user-${clientId}-3`,
        email: 'bob.consultant@expert.com',
        displayName: 'Bob Consultant',
        library: 'Case Evidence',
        permissions: 'Read',
        invitedBy: 'attorney@lawfirm.com',
        invitedDate: '2024-01-25T09:00:00Z',
        lastAccess: null,
        status: 'Invited',
        metadata: {
          company: 'Expert Consultants',
          project: 'Technical Analysis',
          department: 'Engineering'
        }
      }
    ];
  }
}

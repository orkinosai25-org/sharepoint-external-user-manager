import { IExternalLibrary, IExternalUser } from '../models/IExternalLibrary';

export class MockDataService {
  
  public static getExternalLibraries(): IExternalLibrary[] {
    return [
      {
        id: '1',
        name: 'Marketing Documents',
        description: 'Marketing materials and campaigns shared with external partners',
        siteUrl: '/sites/marketing/documents',
        externalUsersCount: 5,
        lastModified: new Date('2024-01-15'),
        owner: 'John Smith',
        permissions: 'Contribute'
      },
      {
        id: '2',
        name: 'Project Alpha Resources',
        description: 'Confidential project documents for Project Alpha stakeholders',
        siteUrl: '/sites/projectalpha/shared',
        externalUsersCount: 3,
        lastModified: new Date('2024-01-10'),
        owner: 'Sarah Johnson',
        permissions: 'Read'
      },
      {
        id: '3',
        name: 'Vendor Collaboration Hub',
        description: 'Document sharing space for vendor partnerships',
        siteUrl: '/sites/vendors/collaboration',
        externalUsersCount: 8,
        lastModified: new Date('2024-01-08'),
        owner: 'Mike Davis',
        permissions: 'Full Control'
      },
      {
        id: '4',
        name: 'Customer Support Files',
        description: 'Documentation and resources for customer support team',
        siteUrl: '/sites/support/resources',
        externalUsersCount: 2,
        lastModified: new Date('2024-01-05'),
        owner: 'Lisa Wilson',
        permissions: 'Read'
      },
      {
        id: '5',
        name: 'Training Materials',
        description: 'Training content shared with external training providers',
        siteUrl: '/sites/training/materials',
        externalUsersCount: 4,
        lastModified: new Date('2024-01-03'),
        owner: 'Robert Brown',
        permissions: 'Contribute'
      }
    ];
  }

  public static getExternalUsersForLibrary(libraryId: string): IExternalUser[] {
    // Mock data for external users - in real implementation, this would come from SharePoint API
    const allUsers: IExternalUser[] = [
      {
        id: 'user1',
        email: 'partner1@external.com',
        displayName: 'External Partner 1',
        invitedBy: 'john.smith@company.com',
        invitedDate: new Date('2023-12-01'),
        lastAccess: new Date('2024-01-14'),
        permissions: 'Read',
        company: 'Acme Corp',
        project: 'Project Alpha'
      },
      {
        id: 'user2',
        email: 'vendor@supplier.com',
        displayName: 'Vendor User',
        invitedBy: 'sarah.johnson@company.com',
        invitedDate: new Date('2023-11-15'),
        lastAccess: new Date('2024-01-12'),
        permissions: 'Contribute',
        company: 'Beta Solutions',
        project: 'Implementation Phase 1'
      },
      {
        id: 'user3',
        email: 'consultant@agency.com',
        displayName: 'External Consultant',
        invitedBy: 'mike.davis@company.com',
        invitedDate: new Date('2023-10-20'),
        lastAccess: new Date('2024-01-10'),
        permissions: 'Contribute',
        company: 'Gamma Consulting',
        project: 'Strategic Review'
      }
    ];

    return allUsers;
  }
}
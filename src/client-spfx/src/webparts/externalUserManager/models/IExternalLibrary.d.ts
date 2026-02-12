export interface IExternalLibrary {
    id: string;
    name: string;
    description: string;
    siteUrl: string;
    externalUsersCount: number;
    lastModified: Date;
    owner: string;
    permissions: 'Read' | 'Contribute' | 'Full Control';
}
export interface IExternalUser {
    id: string;
    email: string;
    displayName: string;
    invitedBy: string;
    invitedDate: Date;
    lastAccess: Date;
    permissions: 'Read' | 'Contribute' | 'Full Control';
    company?: string;
    project?: string;
}
export interface IBulkUserAdditionRequest {
    emails: string[];
    permission: 'Read' | 'Contribute' | 'Full Control';
    message?: string;
    company?: string;
    project?: string;
}
export interface IBulkUserAdditionResult {
    email: string;
    status: 'success' | 'already_member' | 'invitation_sent' | 'failed';
    message: string;
    error?: string;
}
//# sourceMappingURL=IExternalLibrary.d.ts.map
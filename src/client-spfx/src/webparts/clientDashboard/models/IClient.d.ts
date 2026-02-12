/**
 * Client model interfaces for the dashboard
 */
export declare type ClientStatus = 'Provisioning' | 'Active' | 'Error';
export interface IClient {
    id: number;
    tenantId: number;
    clientName: string;
    siteUrl: string;
    siteId: string;
    createdBy: string;
    createdAt: string;
    status: ClientStatus;
    errorMessage?: string;
}
//# sourceMappingURL=IClient.d.ts.map
/**
 * Service to fetch client data from the SaaS backend API
 */
import { WebPartContext } from '@microsoft/sp-webpart-base';
import { IClient } from '../models/IClient';
import { ILibrary, IList, IExternalUser } from '../models/IClientDetail';
export declare class ClientDataService {
    private context;
    private baseUrl;
    constructor(context: WebPartContext);
    /**
     * Get all clients for the current tenant
     */
    getClients(): Promise<IClient[]>;
    /**
     * Create a new client
     */
    createClient(clientName: string): Promise<IClient>;
    /**
     * Get a single client by ID
     */
    getClient(clientId: number): Promise<IClient>;
    /**
     * Get libraries for a specific client
     */
    getClientLibraries(clientId: number): Promise<ILibrary[]>;
    /**
     * Get lists for a specific client
     */
    getClientLists(clientId: number): Promise<IList[]>;
    /**
     * Get external users for a specific client
     */
    getClientExternalUsers(clientId: number): Promise<IExternalUser[]>;
    /**
     * Create a new library for a client
     */
    createLibrary(clientId: number, libraryName: string, description: string): Promise<ILibrary>;
    /**
     * Create a new list for a client
     */
    createList(clientId: number, listName: string, listType: string, description: string): Promise<IList>;
    /**
     * Get access token for API authentication
     * In a real implementation, this would use MSAL or AAD authentication
     */
    private getAccessToken;
}
//# sourceMappingURL=ClientDataService.d.ts.map
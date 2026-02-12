/**
 * Mock data service for development and testing
 */
import { IClient } from '../models/IClient';
import { ILibrary, IList, IExternalUser } from '../models/IClientDetail';
export declare class MockClientDataService {
    /**
     * Get mock client data
     */
    static getClients(): IClient[];
    /**
     * Get mock libraries for a client
     */
    static getClientLibraries(clientId: number): ILibrary[];
    /**
     * Get mock lists for a client
     */
    static getClientLists(clientId: number): IList[];
    /**
     * Create a new library (mock implementation)
     */
    static createLibrary(clientId: number, libraryName: string, description: string): ILibrary;
    /**
     * Create a new list (mock implementation)
     */
    static createList(clientId: number, listName: string, listType: string, description: string): IList;
    /**
     * Get mock external users for a client
     */
    static getClientExternalUsers(clientId: number): IExternalUser[];
}
//# sourceMappingURL=MockClientDataService.d.ts.map
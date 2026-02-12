import { IProduct, IStockAlert, IInventoryTransaction } from '../models/IProduct';
export declare class MockInventoryDataService {
    static getProducts(): IProduct[];
    static getStockAlerts(): IStockAlert[];
    static getInventoryTransactions(): IInventoryTransaction[];
    static createProduct(product: Partial<IProduct>): Promise<IProduct>;
    static updateStock(productId: string, quantity: number, type: 'Stock In' | 'Stock Out'): Promise<void>;
    static generateInventoryReport(): Promise<string>;
}
//# sourceMappingURL=MockInventoryDataService.d.ts.map
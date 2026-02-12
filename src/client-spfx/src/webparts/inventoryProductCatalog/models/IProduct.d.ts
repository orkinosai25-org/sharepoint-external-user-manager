export interface IProduct {
    id: string;
    name: string;
    sku: string;
    category: string;
    description: string;
    price: number;
    currency: string;
    currentStock: number;
    minimumStock: number;
    maximumStock: number;
    supplier: string;
    lastRestocked: Date;
    imageUrl?: string;
    status: ProductStatus;
    location: string;
}
export interface IInventoryTransaction {
    id: string;
    productId: string;
    type: TransactionType;
    quantity: number;
    date: Date;
    reference: string;
    notes?: string;
    performedBy: string;
}
export interface IStockAlert {
    id: string;
    productId: string;
    productName: string;
    alertType: AlertType;
    currentStock: number;
    thresholdStock: number;
    dateGenerated: Date;
    acknowledged: boolean;
}
export declare type ProductStatus = 'Active' | 'Discontinued' | 'Out of Stock' | 'Low Stock';
export declare type TransactionType = 'Stock In' | 'Stock Out' | 'Adjustment' | 'Transfer';
export declare type AlertType = 'Low Stock' | 'Out of Stock' | 'Overstock';
//# sourceMappingURL=IProduct.d.ts.map
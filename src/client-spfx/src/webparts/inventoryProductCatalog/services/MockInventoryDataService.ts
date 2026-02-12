import { IProduct, IStockAlert, IInventoryTransaction } from '../models/IProduct';

export class MockInventoryDataService {
  public static getProducts(): IProduct[] {
    return [
      {
        id: '1',
        name: 'Wireless Bluetooth Headphones',
        sku: 'WBH-001',
        category: 'Electronics',
        description: 'High-quality wireless headphones with noise cancellation',
        price: 199.99,
        currency: '$',
        currentStock: 25,
        minimumStock: 10,
        maximumStock: 100,
        supplier: 'TechSupplier Inc.',
        lastRestocked: new Date('2024-01-10'),
        status: 'Active',
        location: 'Warehouse A - Shelf 1'
      },
      {
        id: '2',
        name: 'Ergonomic Office Chair',
        sku: 'EOC-002',
        category: 'Furniture',
        description: 'Adjustable ergonomic chair with lumbar support',
        price: 299.99,
        currency: '$',
        currentStock: 5,
        minimumStock: 8,
        maximumStock: 50,
        supplier: 'Office Solutions Ltd.',
        lastRestocked: new Date('2024-01-05'),
        status: 'Low Stock',
        location: 'Warehouse B - Section 2'
      },
      {
        id: '3',
        name: 'Laptop Stand Aluminum',
        sku: 'LSA-003',
        category: 'Accessories',
        description: 'Adjustable aluminum laptop stand for better ergonomics',
        price: 49.99,
        currency: '$',
        currentStock: 0,
        minimumStock: 15,
        maximumStock: 75,
        supplier: 'Accessories Plus',
        lastRestocked: new Date('2023-12-20'),
        status: 'Out of Stock',
        location: 'Warehouse A - Shelf 3'
      },
      {
        id: '4',
        name: 'Smart Water Bottle',
        sku: 'SWB-004',
        category: 'Health & Wellness',
        description: 'Smart water bottle with hydration tracking',
        price: 79.99,
        currency: '$',
        currentStock: 45,
        minimumStock: 20,
        maximumStock: 80,
        supplier: 'Health Tech Co.',
        lastRestocked: new Date('2024-01-15'),
        status: 'Active',
        location: 'Warehouse C - Zone 1'
      }
    ];
  }

  public static getStockAlerts(): IStockAlert[] {
    return [
      {
        id: '1',
        productId: '2',
        productName: 'Ergonomic Office Chair',
        alertType: 'Low Stock',
        currentStock: 5,
        thresholdStock: 8,
        dateGenerated: new Date('2024-01-16'),
        acknowledged: false
      },
      {
        id: '2',
        productId: '3',
        productName: 'Laptop Stand Aluminum',
        alertType: 'Out of Stock',
        currentStock: 0,
        thresholdStock: 15,
        dateGenerated: new Date('2024-01-14'),
        acknowledged: false
      }
    ];
  }

  public static getInventoryTransactions(): IInventoryTransaction[] {
    return [
      {
        id: '1',
        productId: '1',
        type: 'Stock In',
        quantity: 50,
        date: new Date('2024-01-10'),
        reference: 'PO-2024-001',
        notes: 'Monthly restock from supplier',
        performedBy: 'warehouse@company.com'
      },
      {
        id: '2',
        productId: '1',
        type: 'Stock Out',
        quantity: 25,
        date: new Date('2024-01-15'),
        reference: 'SO-2024-005',
        notes: 'Sales order fulfillment',
        performedBy: 'sales@company.com'
      }
    ];
  }

  public static async createProduct(product: Partial<IProduct>): Promise<IProduct> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    return {
      id: Math.random().toString(36).substr(2, 9),
      name: product.name || '',
      sku: product.sku || '',
      category: product.category || '',
      description: product.description || '',
      price: product.price || 0,
      currency: product.currency || '$',
      currentStock: product.currentStock || 0,
      minimumStock: product.minimumStock || 0,
      maximumStock: product.maximumStock || 0,
      supplier: product.supplier || '',
      lastRestocked: new Date(),
      status: 'Active',
      location: product.location || ''
    };
  }

  public static async updateStock(productId: string, quantity: number, type: 'Stock In' | 'Stock Out'): Promise<void> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    console.log(`Updating stock for product ${productId}: ${type} ${quantity} units`);
  }

  public static async generateInventoryReport(): Promise<string> {
    // Mock implementation - would generate actual report
    return 'inventory-report-' + new Date().toISOString().split('T')[0] + '.xlsx';
  }
}
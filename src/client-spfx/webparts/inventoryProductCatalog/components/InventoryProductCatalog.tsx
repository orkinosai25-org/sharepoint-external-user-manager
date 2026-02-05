import * as React from 'react';
import { useState, useEffect } from 'react';
import { Stack, CommandBar, DetailsList, IColumn, SelectionMode, Spinner, SpinnerSize, MessageBar, MessageBarType } from '@fluentui/react';
import { IInventoryProductCatalogProps } from './IInventoryProductCatalogProps';
import { IProduct, IStockAlert } from '../models/IProduct';
import { MockInventoryDataService } from '../services/MockInventoryDataService';
import styles from './InventoryProductCatalog.module.scss';

const InventoryProductCatalog: React.FC<IInventoryProductCatalogProps> = (props) => {
  const [products, setProducts] = useState<IProduct[]>([]);
  const [stockAlerts, setStockAlerts] = useState<IStockAlert[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    loadInventoryData();
  }, []);

  const loadInventoryData = async (): Promise<void> => {
    setLoading(true);
    try {
      // Simulate API call
      setTimeout(() => {
        const mockProducts = MockInventoryDataService.getProducts();
        const mockAlerts = MockInventoryDataService.getStockAlerts();
        setProducts(mockProducts);
        setStockAlerts(mockAlerts);
        setLoading(false);
      }, 1000);
    } catch (error) {
      console.error('Error loading inventory data:', error);
      setLoading(false);
    }
  };

  const columns: IColumn[] = [
    {
      key: 'name',
      name: 'Product Name',
      fieldName: 'name',
      minWidth: 150,
      maxWidth: 200,
      isResizable: true
    },
    {
      key: 'sku',
      name: 'SKU',
      fieldName: 'sku',
      minWidth: 100,
      maxWidth: 120,
      isResizable: true
    },
    {
      key: 'category',
      name: 'Category',
      fieldName: 'category',
      minWidth: 120,
      maxWidth: 150,
      isResizable: true
    },
    {
      key: 'currentStock',
      name: 'Current Stock',
      fieldName: 'currentStock',
      minWidth: 100,
      maxWidth: 120,
      isResizable: true
    },
    {
      key: 'price',
      name: 'Price',
      fieldName: 'price',
      minWidth: 80,
      maxWidth: 100,
      isResizable: true,
      onRender: (item: IProduct) => `${item.currency}${item.price.toFixed(2)}`
    },
    {
      key: 'status',
      name: 'Status',
      fieldName: 'status',
      minWidth: 100,
      maxWidth: 120,
      isResizable: true
    }
  ];

  const commandBarItems = [
    {
      key: 'addProduct',
      text: 'Add Product',
      iconProps: { iconName: 'Add' },
      onClick: () => alert('Add Product functionality will be implemented')
    },
    {
      key: 'stockIn',
      text: 'Stock In',
      iconProps: { iconName: 'Upload' },
      onClick: () => alert('Stock In functionality will be implemented')
    },
    {
      key: 'stockOut',
      text: 'Stock Out',
      iconProps: { iconName: 'Download' },
      onClick: () => alert('Stock Out functionality will be implemented')
    },
    {
      key: 'generateReport',
      text: 'Generate Report',
      iconProps: { iconName: 'BarChart4' },
      onClick: () => alert('Generate Report functionality will be implemented')
    },
    {
      key: 'refresh',
      text: 'Refresh',
      iconProps: { iconName: 'Refresh' },
      onClick: loadInventoryData
    }
  ];

  const renderStockAlerts = () => {
    if (stockAlerts.length === 0) return null;

    return (
      <Stack tokens={{ childrenGap: 10 }}>
        <h3>Stock Alerts</h3>
        {stockAlerts.map((alert) => (
          <MessageBar
            key={alert.id}
            messageBarType={alert.alertType === 'Out of Stock' ? MessageBarType.severeWarning : MessageBarType.warning}
            isMultiline={false}
          >
            {alert.productName}: {alert.alertType} - Current: {alert.currentStock}, Threshold: {alert.thresholdStock}
          </MessageBar>
        ))}
      </Stack>
    );
  };

  return (
    <div className={styles.inventoryProductCatalog}>
      <Stack tokens={{ childrenGap: 20 }}>
        <h2>Inventory & Product Catalog</h2>
        
        {renderStockAlerts()}
        
        <CommandBar items={commandBarItems} />
        
        {loading ? (
          <Spinner size={SpinnerSize.large} label="Loading inventory data..." />
        ) : (
          <DetailsList
            items={products}
            columns={columns}
            selectionMode={SelectionMode.multiple}
            setKey="set"
            layoutMode={0}
            isHeaderVisible={true}
          />
        )}
      </Stack>
    </div>
  );
};

export default InventoryProductCatalog;
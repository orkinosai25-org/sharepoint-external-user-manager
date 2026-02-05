import * as React from 'react';
import { useState, useEffect, useCallback } from 'react';
import {
  Stack,
  Text,
  DetailsList,
  DetailsListLayoutMode,
  IColumn,
  SelectionMode,
  CommandBar,
  ICommandBarItemProps,
  MessageBar,
  MessageBarType,
  Spinner,
  SpinnerSize,
  IconButton,
  Link
} from '@fluentui/react';
import { IClientDashboardProps } from './IClientDashboardProps';
import { IClient, ClientStatus } from '../models/IClient';
import { ClientDataService } from '../services/ClientDataService';
import { MockClientDataService } from '../services/MockClientDataService';
import styles from './ClientDashboard.module.scss';

const ClientDashboard: React.FC<IClientDashboardProps> = (props) => {
  const [clients, setClients] = useState<IClient[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [operationMessage, setOperationMessage] = useState<{ message: string; type: MessageBarType } | null>(null);
  const [dataService] = useState(() => new ClientDataService(props.context));

  const loadClients = useCallback(async (useMockData: boolean = false) => {
    setLoading(true);
    setOperationMessage(null);
    
    try {
      let clientsData: IClient[];
      
      if (useMockData) {
        // For development/fallback, use mock data
        clientsData = MockClientDataService.getClients();
      } else {
        // Use real backend API
        clientsData = await dataService.getClients();
      }
      
      setClients(clientsData);
      
      if (!useMockData && clientsData.length === 0) {
        setOperationMessage({
          message: 'No clients found. Clients will appear here once they are added to the system.',
          type: MessageBarType.info
        });
      }
    } catch (error) {
      console.error('Error loading clients:', error);
      setOperationMessage({
        message: `Unable to connect to the backend service. Showing sample data for demonstration.`,
        type: MessageBarType.warning
      });
      
      // Fallback to mock data on error
      setClients(MockClientDataService.getClients());
    } finally {
      setLoading(false);
    }
  }, [dataService]);

  useEffect(() => {
    // Try to load real data first, fallback to mock data if needed
    loadClients(false);
  }, [loadClients]);

  const getStatusColor = (status: ClientStatus): string => {
    switch (status) {
      case 'Active':
        return '#107c10'; // Green
      case 'Provisioning':
        return '#ffaa44'; // Orange
      case 'Error':
        return '#d13438'; // Red
      default:
        return '#605e5c'; // Gray
    }
  };

  const handleOpen = (client: IClient): void => {
    // Open the client's SharePoint site in a new tab
    window.open(client.siteUrl, '_blank');
  };

  const handleManage = (client: IClient): void => {
    // Navigate to the client management page
    // In a real implementation, this could open a panel or navigate to a details page
    alert(`Manage functionality for "${client.clientName}" will be implemented. This will allow you to manage users, libraries, and settings for this client.`);
  };

  const columns: IColumn[] = [
    {
      key: 'clientName',
      name: 'Client Name',
      fieldName: 'clientName',
      minWidth: 150,
      maxWidth: 250,
      isResizable: true,
      onRender: (item: IClient) => (
        <Stack horizontal tokens={{ childrenGap: 8 }} verticalAlign="center">
          <Text style={{ fontWeight: 600 }}>{item.clientName}</Text>
        </Stack>
      )
    },
    {
      key: 'siteUrl',
      name: 'Site URL',
      fieldName: 'siteUrl',
      minWidth: 250,
      maxWidth: 350,
      isResizable: true,
      onRender: (item: IClient) => (
        <Link href={item.siteUrl} target="_blank" underline>
          {item.siteUrl}
        </Link>
      )
    },
    {
      key: 'status',
      name: 'Status',
      fieldName: 'status',
      minWidth: 100,
      maxWidth: 120,
      isResizable: true,
      onRender: (item: IClient) => (
        <Stack horizontal tokens={{ childrenGap: 8 }} verticalAlign="center">
          <div
            style={{
              width: 8,
              height: 8,
              borderRadius: '50%',
              backgroundColor: getStatusColor(item.status)
            }}
          />
          <Text>{item.status}</Text>
        </Stack>
      )
    },
    {
      key: 'actions',
      name: 'Actions',
      minWidth: 120,
      maxWidth: 150,
      isResizable: false,
      onRender: (item: IClient) => (
        <Stack horizontal tokens={{ childrenGap: 4 }}>
          <IconButton
            iconProps={{ iconName: 'OpenInNewWindow' }}
            title="Open Client Site"
            ariaLabel="Open Client Site"
            onClick={() => handleOpen(item)}
            disabled={item.status !== 'Active'}
          />
          <IconButton
            iconProps={{ iconName: 'Settings' }}
            title="Manage Client"
            ariaLabel="Manage Client"
            onClick={() => handleManage(item)}
          />
        </Stack>
      )
    }
  ];

  const commandBarItems: ICommandBarItemProps[] = [
    {
      key: 'refresh',
      text: 'Refresh',
      iconProps: { iconName: 'Refresh' },
      onClick: () => loadClients(false)
    },
    {
      key: 'info',
      text: 'Help',
      iconProps: { iconName: 'Info' },
      onClick: () => {
        alert('Client Dashboard Help:\n\n' +
          '• View all your firm\'s clients in one place\n' +
          '• Click "Open" to visit the client\'s site\n' +
          '• Click "Manage" to configure client settings\n' +
          '• Status shows whether the client site is ready to use\n\n' +
          'For support, contact your system administrator.');
      }
    }
  ];

  return (
    <div className={styles.clientDashboard}>
      <Stack tokens={{ childrenGap: 20 }}>
        <Stack horizontal horizontalAlign="space-between" verticalAlign="center">
          <Stack tokens={{ childrenGap: 5 }}>
            <Text variant="xxLarge" style={{ fontWeight: 600 }}>
              Client Dashboard
            </Text>
            <Text variant="medium" style={{ color: '#605e5c' }}>
              View and manage all your firm's clients
            </Text>
          </Stack>
        </Stack>
        
        {operationMessage && (
          <MessageBar
            messageBarType={operationMessage.type}
            isMultiline={false}
            onDismiss={() => setOperationMessage(null)}
            dismissButtonAriaLabel="Close"
          >
            {operationMessage.message}
          </MessageBar>
        )}
        
        <CommandBar items={commandBarItems} />
        
        {loading ? (
          <Stack horizontalAlign="center" tokens={{ padding: 40 }}>
            <Spinner size={SpinnerSize.large} label="Loading clients..." />
          </Stack>
        ) : (
          <>
            {clients.length === 0 ? (
              <MessageBar messageBarType={MessageBarType.info}>
                No clients found. Clients will appear here once they are added to the system.
              </MessageBar>
            ) : (
              <Stack tokens={{ childrenGap: 10 }}>
                <Text variant="medium" style={{ color: '#605e5c' }}>
                  Showing {clients.length} client{clients.length !== 1 ? 's' : ''}
                </Text>
                <DetailsList
                  items={clients}
                  columns={columns}
                  selectionMode={SelectionMode.none}
                  setKey="set"
                  layoutMode={DetailsListLayoutMode.justified}
                  isHeaderVisible={true}
                  className={styles.clientList}
                />
              </Stack>
            )}
          </>
        )}
      </Stack>
    </div>
  );
};

export default ClientDashboard;

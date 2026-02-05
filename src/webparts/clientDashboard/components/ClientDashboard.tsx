import * as React from 'react';
import { useState, useEffect, useCallback } from 'react';
import {
  Stack,
  Text,
  PrimaryButton,
  DefaultButton,
  DetailsList,
  DetailsListLayoutMode,
  IColumn,
  CommandBar,
  ICommandBarItemProps,
  MessageBar,
  MessageBarType,
  Spinner,
  SpinnerSize,
  SelectionMode,
  Link,
  Icon
} from '@fluentui/react';
import { IClientDashboardProps } from './IClientDashboardProps';
import { IClient } from '../models/IClient';
import { ClientApiService } from '../services/ClientApiService';
import { MockClientDataService } from '../services/MockClientDataService';
import styles from './ClientDashboard.module.scss';

const ClientDashboard: React.FC<IClientDashboardProps> = (props) => {
  const [clients, setClients] = useState<IClient[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [operationMessage, setOperationMessage] = useState<{ message: string; type: MessageBarType } | null>(null);
  const [apiService] = useState(() => new ClientApiService(props.context));

  // Load clients from API with fallback to mock data
  const loadClients = useCallback(async () => {
    setLoading(true);
    setOperationMessage(null);

    try {
      // Try to load from API first
      const clientsData = await apiService.getClients();
      setClients(clientsData);

      if (clientsData.length === 0) {
        setOperationMessage({
          message: 'No clients found. Create your first client to get started.',
          type: MessageBarType.info
        });
      }
    } catch (error) {
      console.error('Error loading clients from API:', error);
      
      // Fallback to mock data for development/demo
      const mockClients = MockClientDataService.getClients();
      setClients(mockClients);
      
      setOperationMessage({
        message: 'Using demo data. Connect to the SaaS API for production use.',
        type: MessageBarType.warning
      });
    } finally {
      setLoading(false);
    }
  }, [apiService]);

  useEffect(() => {
    loadClients();
  }, [loadClients]);

  // Handle opening client site in new tab
  const handleOpenClient = (client: IClient): void => {
    window.open(client.siteUrl, '_blank');
  };

  // Handle managing client (placeholder for future implementation)
  const handleManageClient = (client: IClient): void => {
    setOperationMessage({
      message: `Manage functionality for "${client.clientName}" will be implemented in a future release.`,
      type: MessageBarType.info
    });
  };

  // Render status badge with color coding
  const renderStatusBadge = (status: string): JSX.Element => {
    let iconName: string;
    let className: string;

    switch (status) {
      case 'Active':
        iconName = 'StatusCircleCheckmark';
        className = styles.statusActive;
        break;
      case 'Provisioning':
        iconName = 'StatusCircleSync';
        className = styles.statusProvisioning;
        break;
      case 'Error':
        iconName = 'StatusCircleErrorX';
        className = styles.statusError;
        break;
      default:
        iconName = 'StatusCircleQuestionMark';
        className = styles.statusUnknown;
    }

    return (
      <Stack horizontal verticalAlign="center" tokens={{ childrenGap: 4 }}>
        <Icon iconName={iconName} className={className} />
        <Text className={className}>{status}</Text>
      </Stack>
    );
  };

  // Define columns for the DetailsList
  const columns: IColumn[] = [
    {
      key: 'clientName',
      name: 'Client Name',
      fieldName: 'clientName',
      minWidth: 150,
      maxWidth: 250,
      isResizable: true,
      onRender: (item: IClient) => (
        <Stack>
          <Text variant="medium" styles={{ root: { fontWeight: 600 } }}>
            {item.clientName}
          </Text>
        </Stack>
      )
    },
    {
      key: 'siteUrl',
      name: 'Site URL',
      fieldName: 'siteUrl',
      minWidth: 200,
      maxWidth: 400,
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
      maxWidth: 150,
      isResizable: true,
      onRender: (item: IClient) => renderStatusBadge(item.status)
    },
    {
      key: 'createdAt',
      name: 'Created',
      fieldName: 'createdAt',
      minWidth: 100,
      maxWidth: 150,
      isResizable: true,
      onRender: (item: IClient) => (
        <Text>{new Date(item.createdAt).toLocaleDateString()}</Text>
      )
    },
    {
      key: 'actions',
      name: 'Actions',
      minWidth: 150,
      maxWidth: 200,
      onRender: (item: IClient) => (
        <Stack horizontal tokens={{ childrenGap: 8 }}>
          <DefaultButton
            text="Open"
            iconProps={{ iconName: 'OpenInNewTab' }}
            onClick={() => handleOpenClient(item)}
            disabled={item.status === 'Error'}
          />
          <DefaultButton
            text="Manage"
            iconProps={{ iconName: 'Settings' }}
            onClick={() => handleManageClient(item)}
          />
        </Stack>
      )
    }
  ];

  // Command bar items
  const commandBarItems: ICommandBarItemProps[] = [
    {
      key: 'refresh',
      text: 'Refresh',
      iconProps: { iconName: 'Refresh' },
      onClick: () => { void loadClients(); }
    },
    {
      key: 'newClient',
      text: 'New Client',
      iconProps: { iconName: 'Add' },
      buttonStyles: { root: { backgroundColor: '#0078d4', color: 'white' } },
      onClick: () => {
        setOperationMessage({
          message: 'Create client functionality will be implemented in a future release.',
          type: MessageBarType.info
        });
      }
    }
  ];

  return (
    <div className={styles.clientDashboard}>
      <Stack tokens={{ childrenGap: 20 }}>
        {/* Header */}
        <Stack tokens={{ childrenGap: 8 }}>
          <Text variant="xxLarge" styles={{ root: { fontWeight: 600 } }}>
            Client Dashboard
          </Text>
          <Text variant="medium" styles={{ root: { color: '#605e5c' } }}>
            Manage all your clients in one place
          </Text>
        </Stack>

        {/* Message Bar */}
        {operationMessage && (
          <MessageBar
            messageBarType={operationMessage.type}
            onDismiss={() => setOperationMessage(null)}
            dismissButtonAriaLabel="Close"
          >
            {operationMessage.message}
          </MessageBar>
        )}

        {/* Command Bar */}
        <CommandBar items={commandBarItems} />

        {/* Loading Spinner */}
        {loading ? (
          <Stack horizontalAlign="center" tokens={{ padding: 40 }}>
            <Spinner size={SpinnerSize.large} label="Loading clients..." />
          </Stack>
        ) : (
          <>
            {/* Clients List */}
            {clients.length > 0 ? (
              <DetailsList
                items={clients}
                columns={columns}
                layoutMode={DetailsListLayoutMode.justified}
                selectionMode={SelectionMode.none}
                isHeaderVisible={true}
                className={styles.clientList}
              />
            ) : (
              <Stack horizontalAlign="center" tokens={{ padding: 40 }}>
                <Icon iconName="DocumentSearch" styles={{ root: { fontSize: 48, color: '#a19f9d', marginBottom: 16 } }} />
                <Text variant="large" styles={{ root: { marginBottom: 8 } }}>
                  No clients found
                </Text>
                <Text styles={{ root: { color: '#605e5c', marginBottom: 16 } }}>
                  Get started by creating your first client
                </Text>
                <PrimaryButton
                  text="Create Client"
                  iconProps={{ iconName: 'Add' }}
                  onClick={() => {
                    setOperationMessage({
                      message: 'Create client functionality will be implemented in a future release.',
                      type: MessageBarType.info
                    });
                  }}
                />
              </Stack>
            )}

            {/* Summary */}
            {clients.length > 0 && (() => {
              // Calculate status counts in a single pass
              const statusCounts = clients.reduce((acc, client) => {
                acc[client.status] = (acc[client.status] || 0) + 1;
                return acc;
              }, {} as Record<string, number>);

              return (
                <Stack horizontal horizontalAlign="space-between" styles={{ root: { marginTop: 16, padding: '12px 0', borderTop: '1px solid #edebe9' } }}>
                  <Text variant="medium">
                    Total Clients: <strong>{clients.length}</strong>
                  </Text>
                  <Text variant="medium">
                    Active: <strong>{statusCounts.Active || 0}</strong> |
                    Provisioning: <strong>{statusCounts.Provisioning || 0}</strong> |
                    Error: <strong>{statusCounts.Error || 0}</strong>
                  </Text>
                </Stack>
              );
            })()}
          </>
        )}
      </Stack>
    </div>
  );
};

export default ClientDashboard;

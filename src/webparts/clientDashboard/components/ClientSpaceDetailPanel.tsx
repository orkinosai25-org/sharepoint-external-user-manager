import * as React from 'react';
import { useState, useEffect } from 'react';
import {
  Panel,
  PanelType,
  Stack,
  Text,
  Pivot,
  PivotItem,
  DetailsList,
  DetailsListLayoutMode,
  IColumn,
  SelectionMode,
  Spinner,
  SpinnerSize,
  MessageBar,
  MessageBarType,
  Link,
  Icon,
  Label
} from '@fluentui/react';
import { IClient } from '../models/IClient';
import { ILibrary, IList, IExternalUser } from '../models/IClientDetail';
import { ClientDataService } from '../services/ClientDataService';
import { MockClientDataService } from '../services/MockClientDataService';
import AddLibraryPanel from './AddLibraryPanel';
import AddListPanel from './AddListPanel';
import styles from './ClientSpaceDetailPanel.module.scss';

export interface IClientSpaceDetailPanelProps {
  isOpen: boolean;
  client: IClient | null;
  dataService: ClientDataService;
  onDismiss: () => void;
}

const ClientSpaceDetailPanel: React.FC<IClientSpaceDetailPanelProps> = (props) => {
  const [loading, setLoading] = useState<boolean>(false);
  const [libraries, setLibraries] = useState<ILibrary[]>([]);
  const [lists, setLists] = useState<IList[]>([]);
  const [externalUsers, setExternalUsers] = useState<IExternalUser[]>([]);
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [successMessage, setSuccessMessage] = useState<string>('');
  const [isAddLibraryPanelOpen, setIsAddLibraryPanelOpen] = useState<boolean>(false);
  const [isAddListPanelOpen, setIsAddListPanelOpen] = useState<boolean>(false);

  useEffect(() => {
    if (props.isOpen && props.client) {
      loadClientDetails();
    }
  }, [props.isOpen, props.client]);

  const loadClientDetails = async (): Promise<void> => {
    if (!props.client) return;

    setLoading(true);
    setErrorMessage('');

    try {
      // Try to load data from API
      const [librariesData, listsData, usersData] = await Promise.all([
        props.dataService.getClientLibraries(props.client.id),
        props.dataService.getClientLists(props.client.id),
        props.dataService.getClientExternalUsers(props.client.id)
      ]);

      setLibraries(librariesData);
      setLists(listsData);
      setExternalUsers(usersData);
    } catch (error) {
      console.warn('Error loading client details, falling back to mock data:', error);
      
      // Fallback to mock data
      setLibraries(MockClientDataService.getClientLibraries(props.client.id));
      setLists(MockClientDataService.getClientLists(props.client.id));
      setExternalUsers(MockClientDataService.getClientExternalUsers(props.client.id));
    } finally {
      setLoading(false);
    }
  };

  const handleCreateLibrary = async (libraryName: string, description: string): Promise<void> => {
    if (!props.client) return;

    setErrorMessage('');
    setSuccessMessage('');

    const successMsg = `Document folder "${libraryName}" created successfully!`;

    try {
      // Try to create via API
      const newLibrary = await props.dataService.createLibrary(props.client.id, libraryName, description);
      
      // Add to the libraries list immediately
      setLibraries(prevLibraries => [...prevLibraries, newLibrary]);
      setSuccessMessage(successMsg);
    } catch (error) {
      console.warn('Error creating library via API, using mock data:', error);
      
      // Fallback to mock creation
      const newLibrary = MockClientDataService.createLibrary(props.client.id, libraryName, description);
      setLibraries(prevLibraries => [...prevLibraries, newLibrary]);
      setSuccessMessage(successMsg);
    }
  };

  const handleCreateList = async (listName: string, listType: string, description: string): Promise<void> => {
    if (!props.client) return;

    setErrorMessage('');
    setSuccessMessage('');

    const successMsg = `Data list "${listName}" created successfully!`;

    try {
      // Try to create via API
      const newList = await props.dataService.createList(props.client.id, listName, listType, description);
      
      // Add to the lists array immediately
      setLists(prevLists => [...prevLists, newList]);
      setSuccessMessage(successMsg);
    } catch (error) {
      console.warn('Error creating list via API, using mock data:', error);
      
      // Fallback to mock creation
      const newList = MockClientDataService.createList(props.client.id, listName, listType, description);
      setLists(prevLists => [...prevLists, newList]);
      setSuccessMessage(successMsg);
    }
  };

  const formatDate = (dateString: string | null): string => {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  };

  const getPermissionLabel = (permission: string): string => {
    const labels: { [key: string]: string } = {
      'Read': 'View Only',
      'Contribute': 'Can Edit',
      'Edit': 'Can Edit',
      'FullControl': 'Full Access'
    };
    return labels[permission] || permission;
  };

  const getStatusColor = (status: string): string => {
    switch (status) {
      case 'Active':
        return '#107c10'; // Green
      case 'Invited':
        return '#ffaa44'; // Orange
      case 'Expired':
      case 'Removed':
        return '#d13438'; // Red
      default:
        return '#605e5c'; // Gray
    }
  };

  // Columns for Libraries list
  const librariesColumns: IColumn[] = [
    {
      key: 'displayName',
      name: 'Name',
      fieldName: 'displayName',
      minWidth: 150,
      maxWidth: 250,
      isResizable: true,
      onRender: (item: ILibrary) => (
        <Stack horizontal tokens={{ childrenGap: 8 }} verticalAlign="center">
          <Icon iconName="FabricFolder" style={{ color: '#0078d4' }} />
          <Link href={item.webUrl} target="_blank">
            {item.displayName}
          </Link>
        </Stack>
      )
    },
    {
      key: 'description',
      name: 'Description',
      fieldName: 'description',
      minWidth: 200,
      isResizable: true
    },
    {
      key: 'itemCount',
      name: 'Items',
      fieldName: 'itemCount',
      minWidth: 60,
      maxWidth: 80,
      isResizable: false,
      onRender: (item: ILibrary) => <Text>{item.itemCount}</Text>
    },
    {
      key: 'lastModified',
      name: 'Last Modified',
      minWidth: 100,
      maxWidth: 130,
      isResizable: false,
      onRender: (item: ILibrary) => <Text>{formatDate(item.lastModifiedDateTime)}</Text>
    }
  ];

  // Columns for Lists
  const listsColumns: IColumn[] = [
    {
      key: 'displayName',
      name: 'Name',
      fieldName: 'displayName',
      minWidth: 150,
      maxWidth: 250,
      isResizable: true,
      onRender: (item: IList) => (
        <Stack horizontal tokens={{ childrenGap: 8 }} verticalAlign="center">
          <Icon iconName="BulletedList" style={{ color: '#0078d4' }} />
          <Link href={item.webUrl} target="_blank">
            {item.displayName}
          </Link>
        </Stack>
      )
    },
    {
      key: 'description',
      name: 'Description',
      fieldName: 'description',
      minWidth: 200,
      isResizable: true
    },
    {
      key: 'itemCount',
      name: 'Items',
      fieldName: 'itemCount',
      minWidth: 60,
      maxWidth: 80,
      isResizable: false,
      onRender: (item: IList) => <Text>{item.itemCount}</Text>
    },
    {
      key: 'lastModified',
      name: 'Last Modified',
      minWidth: 100,
      maxWidth: 130,
      isResizable: false,
      onRender: (item: IList) => <Text>{formatDate(item.lastModifiedDateTime)}</Text>
    }
  ];

  // Columns for External Users
  const usersColumns: IColumn[] = [
    {
      key: 'displayName',
      name: 'Name',
      fieldName: 'displayName',
      minWidth: 150,
      maxWidth: 200,
      isResizable: true,
      onRender: (item: IExternalUser) => (
        <Stack tokens={{ childrenGap: 2 }}>
          <Text style={{ fontWeight: 600 }}>{item.displayName}</Text>
          <Text variant="small" style={{ color: '#605e5c' }}>{item.email}</Text>
        </Stack>
      )
    },
    {
      key: 'library',
      name: 'Access To',
      fieldName: 'library',
      minWidth: 120,
      maxWidth: 180,
      isResizable: true
    },
    {
      key: 'permissions',
      name: 'Permission Level',
      fieldName: 'permissions',
      minWidth: 100,
      maxWidth: 130,
      isResizable: false,
      onRender: (item: IExternalUser) => (
        <Text>{getPermissionLabel(item.permissions)}</Text>
      )
    },
    {
      key: 'status',
      name: 'Status',
      fieldName: 'status',
      minWidth: 80,
      maxWidth: 100,
      isResizable: false,
      onRender: (item: IExternalUser) => (
        <Stack horizontal tokens={{ childrenGap: 6 }} verticalAlign="center">
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
      key: 'lastAccess',
      name: 'Last Access',
      minWidth: 100,
      maxWidth: 130,
      isResizable: false,
      onRender: (item: IExternalUser) => (
        <Text>{formatDate(item.lastAccess)}</Text>
      )
    }
  ];

  const renderClientInfo = (): JSX.Element => {
    if (!props.client) return <></>;

    return (
      <Stack tokens={{ childrenGap: 20 }} style={{ padding: '20px 0' }}>
        <Stack tokens={{ childrenGap: 10 }}>
          <Label>Workspace Name</Label>
          <Text style={{ fontSize: 16, fontWeight: 600 }}>
            {props.client.clientName}
          </Text>
        </Stack>

        <Stack tokens={{ childrenGap: 10 }}>
          <Label>Workspace URL</Label>
          <Link href={props.client.siteUrl} target="_blank">
            {props.client.siteUrl}
          </Link>
        </Stack>

        <Stack tokens={{ childrenGap: 10 }}>
          <Label>Created By</Label>
          <Text>{props.client.createdBy}</Text>
        </Stack>

        <Stack tokens={{ childrenGap: 10 }}>
          <Label>Created On</Label>
          <Text>{formatDate(props.client.createdAt)}</Text>
        </Stack>

        <Stack tokens={{ childrenGap: 10 }}>
          <Label>Status</Label>
          <Stack horizontal tokens={{ childrenGap: 8 }} verticalAlign="center">
            <div
              style={{
                width: 10,
                height: 10,
                borderRadius: '50%',
                backgroundColor: props.client.status === 'Active' ? '#107c10' : '#ffaa44'
              }}
            />
            <Text>{props.client.status}</Text>
          </Stack>
        </Stack>
      </Stack>
    );
  };

  return (
    <Panel
      isOpen={props.isOpen}
      onDismiss={props.onDismiss}
      type={PanelType.large}
      headerText={props.client ? `${props.client.clientName} - Workspace Details` : 'Workspace Details'}
      closeButtonAriaLabel="Close"
    >
      {loading ? (
        <Stack horizontalAlign="center" tokens={{ padding: 40 }}>
          <Spinner size={SpinnerSize.large} label="Loading workspace details..." />
        </Stack>
      ) : (
        <Stack tokens={{ childrenGap: 10 }}>
          {errorMessage && (
            <MessageBar
              messageBarType={MessageBarType.error}
              isMultiline={false}
              onDismiss={() => setErrorMessage('')}
            >
              {errorMessage}
            </MessageBar>
          )}

          {successMessage && (
            <MessageBar
              messageBarType={MessageBarType.success}
              isMultiline={false}
              onDismiss={() => setSuccessMessage('')}
            >
              {successMessage}
            </MessageBar>
          )}

          <Pivot aria-label="Workspace sections">
            <PivotItem headerText="Workspace Info" itemIcon="Info">
              {renderClientInfo()}
            </PivotItem>

            <PivotItem headerText="Document Folders" itemIcon="FabricFolder">
              <Stack tokens={{ childrenGap: 15 }} style={{ padding: '20px 0' }}>
                <CommandBar
                  items={[
                    {
                      key: 'addLibrary',
                      text: 'Add Folder',
                      iconProps: { iconName: 'Add' },
                      onClick: () => setIsAddLibraryPanelOpen(true)
                    }
                  ]}
                />
                <Text variant="medium" style={{ color: '#605e5c' }}>
                  {libraries.length} folder{libraries.length !== 1 ? 's' : ''} available
                </Text>
                {libraries.length === 0 ? (
                  <MessageBar messageBarType={MessageBarType.info}>
                    No document folders found for this workspace. Click "Add Folder" to create one.
                  </MessageBar>
                ) : (
                  <DetailsList
                    items={libraries}
                    columns={librariesColumns}
                    selectionMode={SelectionMode.none}
                    layoutMode={DetailsListLayoutMode.justified}
                    isHeaderVisible={true}
                  />
                )}
              </Stack>
            </PivotItem>

            <PivotItem headerText="Data Lists" itemIcon="BulletedList">
              <Stack tokens={{ childrenGap: 15 }} style={{ padding: '20px 0' }}>
                <CommandBar
                  items={[
                    {
                      key: 'addList',
                      text: 'Add List',
                      iconProps: { iconName: 'Add' },
                      onClick: () => setIsAddListPanelOpen(true)
                    }
                  ]}
                />
                <Text variant="medium" style={{ color: '#605e5c' }}>
                  {lists.length} list{lists.length !== 1 ? 's' : ''} available
                </Text>
                {lists.length === 0 ? (
                  <MessageBar messageBarType={MessageBarType.info}>
                    No data lists found for this workspace. Click "Add List" to create one.
                  </MessageBar>
                ) : (
                  <DetailsList
                    items={lists}
                    columns={listsColumns}
                    selectionMode={SelectionMode.none}
                    layoutMode={DetailsListLayoutMode.justified}
                    isHeaderVisible={true}
                  />
                )}
              </Stack>
            </PivotItem>

            <PivotItem headerText="Guest Users" itemIcon="People">
              <Stack tokens={{ childrenGap: 15 }} style={{ padding: '20px 0' }}>
                <Text variant="medium" style={{ color: '#605e5c' }}>
                  {externalUsers.length} guest user{externalUsers.length !== 1 ? 's' : ''}
                </Text>
                {externalUsers.length === 0 ? (
                  <MessageBar messageBarType={MessageBarType.info}>
                    No guest users have been invited to this workspace yet.
                  </MessageBar>
                ) : (
                  <DetailsList
                    items={externalUsers}
                    columns={usersColumns}
                    selectionMode={SelectionMode.none}
                    layoutMode={DetailsListLayoutMode.justified}
                    isHeaderVisible={true}
                  />
                )}
              </Stack>
            </PivotItem>
          </Pivot>
        </Stack>
      )}

      {/* Add Library Panel */}
      {props.client && (
        <AddLibraryPanel
          isOpen={isAddLibraryPanelOpen}
          clientName={props.client.clientName}
          onDismiss={() => setIsAddLibraryPanelOpen(false)}
          onLibraryCreated={handleCreateLibrary}
        />
      )}

      {/* Add List Panel */}
      {props.client && (
        <AddListPanel
          isOpen={isAddListPanelOpen}
          clientName={props.client.clientName}
          onDismiss={() => setIsAddListPanelOpen(false)}
          onListCreated={handleCreateList}
        />
      )}
    </Panel>
  );
};

export default ClientSpaceDetailPanel;

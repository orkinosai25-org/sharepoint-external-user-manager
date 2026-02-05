import * as React from 'react';
import { useState, useEffect } from 'react';
import {
  Modal,
  Stack,
  Text,
  TextField,
  PrimaryButton,
  DefaultButton,
  MessageBar,
  MessageBarType,
  Spinner,
  SpinnerSize,
  DetailsList,
  DetailsListLayoutMode,
  Selection,
  SelectionMode,
  IColumn,
  CommandBar,
  ICommandBarItemProps,
  Dropdown,
  IDropdownOption,
  IconButton,
  Dialog,
  DialogType,
  DialogFooter
} from '@fluentui/react';
import { IExternalLibrary, IExternalUser } from '../models/IExternalLibrary';

export interface IManageUsersModalProps {
  isOpen: boolean;
  library: IExternalLibrary | null;
  onClose: () => void;
  onAddUser: (libraryId: string, email: string, permission: 'Read' | 'Edit', company?: string, project?: string) => Promise<void>;
  onBulkAddUsers: (libraryId: string, emails: string[], permission: 'Read' | 'Edit', company?: string, project?: string) => Promise<any>;
  onRemoveUser: (libraryId: string, userId: string) => Promise<void>;
  onGetUsers: (libraryId: string) => Promise<IExternalUser[]>;
  onSearchUsers: (query: string) => Promise<IExternalUser[]>;
  onUpdateUserMetadata: (libraryId: string, userId: string, company: string, project: string) => Promise<void>;
}

export interface IAddUserFormData {
  email: string;
  emails: string; // For bulk mode
  permission: 'Read' | 'Edit';
  isBulkMode: boolean;
  company: string;
  project: string;
}

export const ManageUsersModal: React.FC<IManageUsersModalProps> = ({
  isOpen,
  library,
  onClose,
  onAddUser,
  onBulkAddUsers,
  onRemoveUser,
  onGetUsers,
  onSearchUsers,
  onUpdateUserMetadata
}) => {
  const [users, setUsers] = useState<IExternalUser[]>([]);
  const [selectedUsers, setSelectedUsers] = useState<IExternalUser[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>('');
  const [operationMessage, setOperationMessage] = useState<{ message: string; type: MessageBarType } | null>(null);
  
  // Add User Form State
  const [showAddUserForm, setShowAddUserForm] = useState<boolean>(false);
  const [addUserForm, setAddUserForm] = useState<IAddUserFormData>({
    email: '',
    emails: '',
    permission: 'Read',
    isBulkMode: false,
    company: '',
    project: ''
  });
  const [addingUser, setAddingUser] = useState<boolean>(false);
  const [validationErrors, setValidationErrors] = useState<{[key: string]: string}>({});
  const [bulkResults, setBulkResults] = useState<any[] | null>(null);
  
  // Remove User Confirmation
  const [showRemoveConfirmation, setShowRemoveConfirmation] = useState<boolean>(false);
  const [removingUser, setRemovingUser] = useState<boolean>(false);

  // Edit User Metadata
  const [showEditModal, setShowEditModal] = useState<boolean>(false);
  const [editingUser, setEditingUser] = useState<IExternalUser | null>(null);
  const [editForm, setEditForm] = useState<{ company: string; project: string }>({ company: '', project: '' });
  const [updatingUser, setUpdatingUser] = useState<boolean>(false);

  const [selection] = useState(new Selection({
    onSelectionChanged: () => {
      setSelectedUsers(selection.getSelection() as IExternalUser[]);
    }
  }));

  // Load users when modal opens
  useEffect(() => {
    if (isOpen && library) {
      loadUsers();
    } else if (!isOpen) {
      // Reset state when modal closes
      setUsers([]);
      setSelectedUsers([]);
      setError('');
      setOperationMessage(null);
      setShowAddUserForm(false);
      setShowRemoveConfirmation(false);
      setShowEditModal(false);
      setEditingUser(null);
      setBulkResults(null);
      setAddUserForm({ email: '', emails: '', permission: 'Read', isBulkMode: false, company: '', project: '' });
      selection.setAllSelected(false);
    }
  }, [isOpen, library]);

  const loadUsers = async (): Promise<void> => {
    if (!library) return;
    
    setLoading(true);
    setError('');
    setOperationMessage(null);
    
    try {
      const loadedUsers = await onGetUsers(library.id);
      setUsers(loadedUsers);
      
      if (loadedUsers.length === 0) {
        setOperationMessage({
          message: 'No external users found for this library.',
          type: MessageBarType.info
        });
      }
    } catch (err) {
      const errorMessage = err.message || 'Failed to load users';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleAddUser = async (): Promise<void> => {
    if (!library) return;
    
    if (!validateAddUserForm()) {
      return;
    }

    setAddingUser(true);
    setError('');
    setBulkResults(null);
    
    try {
      if (addUserForm.isBulkMode) {
        // Parse emails from bulk input
        const emails = parseEmailsFromText(addUserForm.emails);
        
        if (emails.length === 0) {
          setError('Please enter at least one valid email address');
          return;
        }

        // Call bulk add function
        const results = await onBulkAddUsers(library.id, emails, addUserForm.permission, addUserForm.company?.trim() || undefined, addUserForm.project?.trim() || undefined);
        
        setBulkResults(results);
        
        const successCount = results.filter((r: any) => r.status === 'success' || r.status === 'invitation_sent').length;
        const alreadyMemberCount = results.filter((r: any) => r.status === 'already_member').length;
        const failedCount = results.filter((r: any) => r.status === 'failed').length;
        
        let message = `Bulk operation completed: `;
        const messageParts = [];
        if (successCount > 0) messageParts.push(`${successCount} added`);
        if (alreadyMemberCount > 0) messageParts.push(`${alreadyMemberCount} already members`);
        if (failedCount > 0) messageParts.push(`${failedCount} failed`);
        
        message += messageParts.join(', ');
        
        setOperationMessage({
          message,
          type: failedCount === 0 ? MessageBarType.success : MessageBarType.warning
        });
        
      } else {
        // Single user addition
        await onAddUser(library.id, addUserForm.email.trim(), addUserForm.permission, addUserForm.company?.trim() || undefined, addUserForm.project?.trim() || undefined);
        
        setOperationMessage({
          message: `Successfully added ${addUserForm.email} to ${library.name}`,
          type: MessageBarType.success
        });
      }
      
      // Reset form and reload users only on full success for single mode
      // For bulk mode, keep the form open to show results
      if (!addUserForm.isBulkMode) {
        setAddUserForm({ email: '', emails: '', permission: 'Read', isBulkMode: false, company: '', project: '' });
        setShowAddUserForm(false);
      }
      
      await loadUsers();
      
    } catch (err) {
      setError(err.message || 'Failed to add user(s)');
    } finally {
      setAddingUser(false);
    }
  };

  const parseEmailsFromText = (text: string): string[] => {
    if (!text.trim()) return [];
    
    // Split by comma, semicolon, or newline, then filter and trim
    return text
      .split(/[,;\n]/)
      .map(email => email.trim())
      .filter(email => email && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email));
  };

  const handleRemoveUsers = async (): Promise<void> => {
    if (!library || selectedUsers.length === 0) return;
    
    setRemovingUser(true);
    setError('');
    
    try {
      // Remove each selected user
      for (const user of selectedUsers) {
        await onRemoveUser(library.id, user.id);
      }
      
      setOperationMessage({
        message: `Successfully removed ${selectedUsers.length} user(s) from ${library.name}`,
        type: MessageBarType.success
      });
      
      setShowRemoveConfirmation(false);
      setSelectedUsers([]);
      selection.setAllSelected(false);
      await loadUsers();
      
    } catch (err) {
      setError(err.message || 'Failed to remove users');
    } finally {
      setRemovingUser(false);
    }
  };

  const validateAddUserForm = (): boolean => {
    const errors: {[key: string]: string} = {};
    
    if (addUserForm.isBulkMode) {
      // Bulk mode validation
      if (!addUserForm.emails.trim()) {
        errors.emails = 'Please enter at least one email address';
      } else {
        const emails = parseEmailsFromText(addUserForm.emails);
        if (emails.length === 0) {
          errors.emails = 'Please enter valid email addresses (separated by commas, semicolons, or newlines)';
        }
      }
    } else {
      // Single mode validation
      if (!addUserForm.email.trim()) {
        errors.email = 'Email address is required';
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(addUserForm.email.trim())) {
        errors.email = 'Please enter a valid email address';
      }
    }
    
    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleInputChange = (field: keyof IAddUserFormData) => 
    (event: React.FormEvent<HTMLInputElement | HTMLTextAreaElement>, newValue?: string) => {
      setAddUserForm(prev => ({
        ...prev,
        [field]: newValue || ''
      }));
      
      // Clear validation error for this field
      if (validationErrors[field]) {
        setValidationErrors(prev => {
          const newErrors = { ...prev };
          delete newErrors[field];
          return newErrors;
        });
      }
    };

  const handlePermissionChange = (event: React.FormEvent<HTMLDivElement>, option?: IDropdownOption) => {
    if (option) {
      setAddUserForm(prev => ({
        ...prev,
        permission: option.key as 'Read' | 'Edit'
      }));
    }
  };

  const handleClose = (): void => {
    if (!addingUser && !removingUser && !updatingUser && !loading) {
      onClose();
    }
  };

  const handleEditUser = (user: IExternalUser): void => {
    setEditingUser(user);
    setEditForm({
      company: user.company || '',
      project: user.project || ''
    });
    setShowEditModal(true);
  };

  const handleUpdateUserMetadata = async (): Promise<void> => {
    if (!editingUser || !library) return;

    setUpdatingUser(true);
    try {

      // Call the service method through the prop

      await onUpdateUserMetadata(library.id, editingUser.id, editForm.company, editForm.project);

      // Update the local users list
      setUsers(prev => prev.map(user => 
        user.id === editingUser.id 
          ? { ...user, company: editForm.company || undefined, project: editForm.project || undefined }
          : user
      ));

      setOperationMessage({
        message: `Successfully updated metadata for ${editingUser.displayName}`,
        type: MessageBarType.success
      });

      setShowEditModal(false);
      setEditingUser(null);
    } catch (error) {
      setOperationMessage({
        message: `Failed to update metadata: ${error.message}`,
        type: MessageBarType.error
      });
    } finally {
      setUpdatingUser(false);
    }
  };

  // Define columns for the users list
  const columns: IColumn[] = [
    {
      key: 'displayName',
      name: 'Name',
      fieldName: 'displayName',
      minWidth: 150,
      maxWidth: 200,
      isResizable: true,
      onRender: (item: IExternalUser) => (
        <Text variant="medium">{item.displayName || 'Unknown'}</Text>
      )
    },
    {
      key: 'email',
      name: 'Email',
      fieldName: 'email',
      minWidth: 200,
      maxWidth: 250,
      isResizable: true,
      onRender: (item: IExternalUser) => (
        <Text variant="small">{item.email}</Text>
      )
    },
    {
      key: 'permissions',
      name: 'Permissions',
      fieldName: 'permissions',
      minWidth: 120,
      maxWidth: 150,
      isResizable: true,
      onRender: (item: IExternalUser) => (
        <Text variant="small">{item.permissions}</Text>
      )
    },
    {
      key: 'company',
      name: 'Company',
      fieldName: 'company',
      minWidth: 120,
      maxWidth: 180,
      isResizable: true,
      onRender: (item: IExternalUser) => (
        <Text variant="small">{item.company || '-'}</Text>
      )
    },
    {
      key: 'project',
      name: 'Project',
      fieldName: 'project',
      minWidth: 120,
      maxWidth: 180,
      isResizable: true,
      onRender: (item: IExternalUser) => (
        <Text variant="small">{item.project || '-'}</Text>
      )
    },
    {
      key: 'invitedDate',
      name: 'Invited Date',
      fieldName: 'invitedDate',
      minWidth: 120,
      maxWidth: 150,
      isResizable: true,
      onRender: (item: IExternalUser) => (
        <Text variant="small">
          {item.invitedDate.toLocaleDateString()}
        </Text>
      )
    },
    {
      key: 'actions',
      name: 'Actions',
      fieldName: 'actions',
      minWidth: 100,
      maxWidth: 120,
      isResizable: false,
      onRender: (item: IExternalUser) => (
        <Stack horizontal tokens={{ childrenGap: 5 }}>
          <IconButton
            iconProps={{ iconName: 'Edit' }}
            title="Edit Company/Project"
            ariaLabel="Edit user metadata"
            onClick={() => handleEditUser(item)}
            styles={{
              root: { minWidth: 24, width: 24, height: 24 },
              icon: { fontSize: 12 }
            }}
          />
        </Stack>
      )
    }
  ];

  const commandBarItems: ICommandBarItemProps[] = [
    {
      key: 'addUser',
      text: 'Add User',
      iconProps: { iconName: 'AddFriend' },
      onClick: () => {
        setAddUserForm(prev => ({ ...prev, isBulkMode: false }));
        setShowAddUserForm(true);
      }
    },
    {
      key: 'bulkAddUsers',
      text: 'Bulk Add Users',
      iconProps: { iconName: 'AddGroup' },
      onClick: () => {
        setAddUserForm(prev => ({ ...prev, isBulkMode: true }));
        setBulkResults(null);
        setShowAddUserForm(true);
      }
    },
    {
      key: 'removeUser',
      text: 'Remove User',
      iconProps: { iconName: 'RemoveFriend' },
      disabled: selectedUsers.length === 0,
      onClick: () => setShowRemoveConfirmation(true)
    },
    {
      key: 'refresh',
      text: 'Refresh',
      iconProps: { iconName: 'Refresh' },
      onClick: loadUsers
    }
  ];

  const permissionOptions: IDropdownOption[] = [
    { key: 'Read', text: 'Read' },
    { key: 'Edit', text: 'Edit' }
  ];

  const modalProps = {
    isOpen,
    onDismiss: handleClose,
    isBlocking: addingUser || removingUser || loading,
    containerClassName: 'manage-users-modal'
  };

  return (
    <>
      <Modal {...modalProps}>
        <div style={{ padding: '20px', minWidth: '600px', maxWidth: '800px' }}>
          <Stack tokens={{ childrenGap: 20 }}>
            <Stack.Item>
              <Stack horizontal horizontalAlign="space-between" verticalAlign="center">
                <Stack>
                  <Text variant="xLarge" styles={{ root: { fontWeight: 'semibold' } }}>
                    Manage External Users
                  </Text>
                  <Text variant="medium" styles={{ root: { color: '#666', marginTop: '4px' } }}>
                    {library ? `Library: ${library.name}` : 'No library selected'}
                  </Text>
                </Stack>
                <IconButton
                  iconProps={{ iconName: 'Cancel' }}
                  ariaLabel="Close"
                  onClick={handleClose}
                  disabled={addingUser || removingUser || loading}
                />
              </Stack>
            </Stack.Item>

            {error && (
              <Stack.Item>
                <MessageBar messageBarType={MessageBarType.error}>
                  {error}
                </MessageBar>
              </Stack.Item>
            )}

            {operationMessage && (
              <Stack.Item>
                <MessageBar messageBarType={operationMessage.type}>
                  {operationMessage.message}
                </MessageBar>
              </Stack.Item>
            )}

            <Stack.Item>
              <CommandBar
                items={commandBarItems}
                ariaLabel="User Management Actions"
              />
            </Stack.Item>

            <Stack.Item>
              {loading ? (
                <Stack horizontalAlign="center" tokens={{ childrenGap: 10 }}>
                  <Spinner size={SpinnerSize.large} label="Loading users..." />
                </Stack>
              ) : (
                <DetailsList
                  items={users}
                  columns={columns}
                  setKey="set"
                  layoutMode={DetailsListLayoutMode.justified}
                  selection={selection}
                  selectionPreservedOnEmptyClick={true}
                  selectionMode={SelectionMode.multiple}
                  ariaLabelForSelectionColumn="Toggle selection"
                  ariaLabelForSelectAllCheckbox="Toggle selection for all items"
                  checkButtonAriaLabel="select row"
                />
              )}
            </Stack.Item>

            <Stack.Item>
              <Stack horizontal tokens={{ childrenGap: 15 }}>
                <Text variant="small" styles={{ root: { color: '#666' } }}>
                  Total External Users: {users.length}
                </Text>
                <Text variant="small" styles={{ root: { color: '#666' } }}>
                  Selected: {selectedUsers.length}
                </Text>
              </Stack>
            </Stack.Item>

            {/* Add User Form */}
            {showAddUserForm && (
              <Stack.Item>
                <div style={{ 
                  border: '1px solid #edebe9', 
                  borderRadius: '4px', 
                  padding: '16px',
                  backgroundColor: '#faf9f8'
                }}>
                  <Stack tokens={{ childrenGap: 15 }}>
                    <Stack.Item>
                      <Text variant="mediumPlus" styles={{ root: { fontWeight: 'semibold' } }}>
                        {addUserForm.isBulkMode ? 'Bulk Add External Users' : 'Add External User'}
                      </Text>
                    </Stack.Item>
                    
                    <Stack.Item>
                      <Stack horizontal={!addUserForm.isBulkMode} tokens={{ childrenGap: 10 }}>
                        <Stack.Item grow>
                          {addUserForm.isBulkMode ? (
                            <TextField
                              label="Email Addresses *"
                              multiline
                              rows={6}
                              value={addUserForm.emails}
                              onChange={handleInputChange('emails')}
                              disabled={addingUser}
                              errorMessage={validationErrors.emails}
                              placeholder="Enter multiple email addresses separated by commas, semicolons, or new lines&#10;&#10;Example:&#10;user1@external.com&#10;user2@partner.com, user3@vendor.com"
                              description="Enter multiple email addresses for bulk invitation"
                            />
                          ) : (
                            <TextField
                              label="Email Address *"
                              value={addUserForm.email}
                              onChange={handleInputChange('email')}
                              disabled={addingUser}
                              errorMessage={validationErrors.email}
                              placeholder="Enter user's email address"
                              description="Enter the email address of the external user to invite"
                            />
                          )}
                        </Stack.Item>
                        {!addUserForm.isBulkMode && (
                          <Stack.Item>
                            <Dropdown
                              label="Permission Level *"
                              options={permissionOptions}
                              selectedKey={addUserForm.permission}
                              onChange={handlePermissionChange}
                              disabled={addingUser}
                              styles={{ dropdown: { width: 150 } }}
                            />
                          </Stack.Item>
                        )}
                      </Stack>
                    </Stack.Item>

                    {addUserForm.isBulkMode && (
                      <Stack.Item>
                        <Dropdown
                          label="Permission Level for all users *"
                          options={permissionOptions}
                          selectedKey={addUserForm.permission}
                          onChange={handlePermissionChange}
                          disabled={addingUser}
                          styles={{ dropdown: { width: 200 } }}
                        />
                      </Stack.Item>
                    )}

                    {/* Company and Project fields for both single and bulk mode */}
                    <Stack.Item>
                      <Stack horizontal tokens={{ childrenGap: 10 }}>
                        <Stack.Item grow>
                          <TextField
                            label="Company"
                            value={addUserForm.company}
                            onChange={handleInputChange('company')}
                            disabled={addingUser}
                            placeholder="Enter company name"
                            description="Company or organization the user belongs to"
                          />
                        </Stack.Item>
                        <Stack.Item grow>
                          <TextField
                            label="Project"
                            value={addUserForm.project}
                            onChange={handleInputChange('project')}
                            disabled={addingUser}
                            placeholder="Enter project name"
                            description="Project or initiative the user is associated with"
                          />
                        </Stack.Item>
                      </Stack>
                    </Stack.Item>

                    {/* Bulk Results Display */}
                    {bulkResults && (
                      <Stack.Item>
                        <div style={{ 
                          border: '1px solid #edebe9',
                          borderRadius: '4px',
                          padding: '12px',
                          backgroundColor: '#fff'
                        }}>
                          <Text variant="medium" styles={{ root: { fontWeight: 'semibold', marginBottom: '8px' } }}>
                            Bulk Operation Results:
                          </Text>
                          <div style={{ maxHeight: '200px', overflowY: 'auto' }}>
                            {bulkResults.map((result: any, index: number) => (
                              <div key={index} style={{ 
                                display: 'flex', 
                                justifyContent: 'space-between', 
                                padding: '4px 0',
                                borderBottom: index < bulkResults.length - 1 ? '1px solid #f3f2f1' : 'none'
                              }}>
                                <Text variant="small">{result.email}</Text>
                                <Text 
                                  variant="small" 
                                  styles={{ 
                                    root: { 
                                      color: result.status === 'success' || result.status === 'invitation_sent' 
                                        ? '#107c10' 
                                        : result.status === 'already_member' 
                                        ? '#797775' 
                                        : '#d13438',
                                      fontWeight: 'semibold'
                                    } 
                                  }}
                                >
                                  {result.status === 'success' ? '✓ Added' :
                                   result.status === 'invitation_sent' ? '✓ Invited' :
                                   result.status === 'already_member' ? '- Already member' :
                                   '✗ Failed'}
                                </Text>
                              </div>
                            ))}
                          </div>
                        </div>
                      </Stack.Item>
                    )}

                    <Stack.Item>
                      <Stack horizontal tokens={{ childrenGap: 10 }}>
                        <PrimaryButton
                          text={addingUser 
                            ? (addUserForm.isBulkMode ? 'Adding Users...' : 'Adding...') 
                            : (addUserForm.isBulkMode ? 'Add Users' : 'Add User')
                          }
                          onClick={handleAddUser}
                          disabled={addingUser || (addUserForm.isBulkMode 
                            ? !addUserForm.emails.trim() 
                            : !addUserForm.email.trim()
                          )}
                          iconProps={addingUser ? undefined : { iconName: addUserForm.isBulkMode ? 'AddGroup' : 'AddFriend' }}
                        />
                        <DefaultButton
                          text="Cancel"
                          onClick={() => {
                            setShowAddUserForm(false);
                            setAddUserForm({ email: '', emails: '', permission: 'Read', isBulkMode: false, company: '', project: '' });
                            setValidationErrors({});
                            setBulkResults(null);
                          }}
                          disabled={addingUser}
                        />
                        {bulkResults && (
                          <DefaultButton
                            text="Close Results"
                            onClick={() => {
                              setShowAddUserForm(false);
                              setAddUserForm({ email: '', emails: '', permission: 'Read', isBulkMode: false, company: '', project: '' });
                              setValidationErrors({});
                              setBulkResults(null);
                            }}
                          />
                        )}
                      </Stack>
                    </Stack.Item>
                  </Stack>
                </div>
              </Stack.Item>
            )}

            <Stack.Item>
              <Stack horizontal tokens={{ childrenGap: 10 }}>
                <DefaultButton
                  text="Close"
                  onClick={handleClose}
                  disabled={addingUser || removingUser || loading}
                />
              </Stack>
            </Stack.Item>
          </Stack>
        </div>
      </Modal>

      {/* Remove User Confirmation Dialog */}
      <Dialog
        hidden={!showRemoveConfirmation}
        onDismiss={() => setShowRemoveConfirmation(false)}
        dialogContentProps={{
          type: DialogType.normal,
          title: 'Remove External Users',
          subText: `Are you sure you want to remove ${selectedUsers.length} user(s) from "${library?.name}"? This action cannot be undone.`
        }}
        modalProps={{
          isBlocking: removingUser
        }}
      >
        <DialogFooter>
          <PrimaryButton
            onClick={handleRemoveUsers}
            text={removingUser ? 'Removing...' : 'Remove'}
            disabled={removingUser}
          />
          <DefaultButton
            onClick={() => setShowRemoveConfirmation(false)}
            text="Cancel"
            disabled={removingUser}
          />
        </DialogFooter>
      </Dialog>

      {/* Edit User Metadata Dialog */}
      <Dialog
        hidden={!showEditModal}
        onDismiss={() => setShowEditModal(false)}
        dialogContentProps={{
          type: DialogType.normal,
          title: 'Edit User Metadata',
          subText: `Update company and project information for ${editingUser?.displayName || editingUser?.email}`
        }}
        modalProps={{
          isBlocking: updatingUser
        }}
        minWidth={400}
      >
        <Stack tokens={{ childrenGap: 15 }}>
          <TextField
            label="Company"
            value={editForm.company}
            onChange={(event, newValue) => setEditForm(prev => ({ ...prev, company: newValue || '' }))}
            disabled={updatingUser}
            placeholder="Enter company name"
          />
          <TextField
            label="Project"
            value={editForm.project}
            onChange={(event, newValue) => setEditForm(prev => ({ ...prev, project: newValue || '' }))}
            disabled={updatingUser}
            placeholder="Enter project name"
          />
        </Stack>
        <DialogFooter>
          <PrimaryButton
            onClick={handleUpdateUserMetadata}
            text={updatingUser ? 'Updating...' : 'Update'}
            disabled={updatingUser}
          />
          <DefaultButton
            onClick={() => setShowEditModal(false)}
            text="Cancel"
            disabled={updatingUser}
          />
        </DialogFooter>
      </Dialog>
    </>
  );
};
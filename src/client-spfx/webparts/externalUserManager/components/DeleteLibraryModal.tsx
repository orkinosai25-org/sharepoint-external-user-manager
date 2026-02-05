import * as React from 'react';
import { useState } from 'react';
import {
  Modal,
  Stack,
  Text,
  PrimaryButton,
  DefaultButton,
  MessageBar,
  MessageBarType,
  Spinner,
  SpinnerSize,
  Checkbox,
  TextField
} from '@fluentui/react';
import { IExternalLibrary } from '../models/IExternalLibrary';

export interface IDeleteLibraryModalProps {
  isOpen: boolean;
  libraries: IExternalLibrary[];
  onClose: () => void;
  onLibrariesDeleted: (deletedLibraryIds: string[]) => void;
  onDeleteLibrary: (libraryId: string) => Promise<void>;
}

export const DeleteLibraryModal: React.FC<IDeleteLibraryModalProps> = ({
  isOpen,
  libraries,
  onClose,
  onLibrariesDeleted,
  onDeleteLibrary
}) => {
  const [isDeleting, setIsDeleting] = useState(false);
  const [confirmationText, setConfirmationText] = useState('');
  const [acknowledgeDataLoss, setAcknowledgeDataLoss] = useState(false);
  const [error, setError] = useState<string>('');
  const [deletionProgress, setDeletionProgress] = useState<{[key: string]: 'pending' | 'deleting' | 'completed' | 'failed'}>({});

  const isMultipleLibraries = libraries.length > 1;
  const confirmationPhrase = 'DELETE';
  const isConfirmationValid = confirmationText === confirmationPhrase;
  const canDelete = isConfirmationValid && acknowledgeDataLoss && !isDeleting;

  const totalExternalUsers = libraries.reduce((sum, lib) => sum + lib.externalUsersCount, 0);
  const hasExternalUsers = totalExternalUsers > 0;

  const resetForm = (): void => {
    setConfirmationText('');
    setAcknowledgeDataLoss(false);
    setError('');
    setDeletionProgress({});
  };

  const handleClose = (): void => {
    if (!isDeleting) {
      resetForm();
      onClose();
    }
  };

  const handleDelete = async (): Promise<void> => {
    if (!canDelete) return;

    setError('');
    setIsDeleting(true);
    
    // Initialize progress tracking
    const initialProgress: {[key: string]: 'pending' | 'deleting' | 'completed' | 'failed'} = {};
    libraries.forEach(lib => {
      initialProgress[lib.id] = 'pending';
    });
    setDeletionProgress(initialProgress);

    const deletedIds: string[] = [];
    let hasErrors = false;

    // Delete libraries one by one to provide detailed progress feedback
    for (const library of libraries) {
      try {
        setDeletionProgress(prev => ({
          ...prev,
          [library.id]: 'deleting'
        }));

        await onDeleteLibrary(library.id);
        
        setDeletionProgress(prev => ({
          ...prev,
          [library.id]: 'completed'
        }));
        
        deletedIds.push(library.id);
      } catch (err) {
        hasErrors = true;
        setDeletionProgress(prev => ({
          ...prev,
          [library.id]: 'failed'
        }));
        
        setError(prev => {
          const newError = `Failed to delete "${library.name}": ${err.message}`;
          return prev ? `${prev}\n${newError}` : newError;
        });
      }
    }

    setIsDeleting(false);

    // If some libraries were deleted successfully, notify parent
    if (deletedIds.length > 0) {
      onLibrariesDeleted(deletedIds);
    }

    // If all libraries were deleted successfully, close the modal
    if (!hasErrors) {
      setTimeout(() => {
        handleClose();
      }, 1000); // Give user time to see success message
    }
  };

  const getProgressIcon = (status: 'pending' | 'deleting' | 'completed' | 'failed'): string => {
    switch (status) {
      case 'pending': return 'Clock';
      case 'deleting': return 'Sync';
      case 'completed': return 'CheckMark';
      case 'failed': return 'ErrorBadge';
      default: return 'Clock';
    }
  };

  const getProgressColor = (status: 'pending' | 'deleting' | 'completed' | 'failed'): string => {
    switch (status) {
      case 'pending': return '#666';
      case 'deleting': return '#0078d4';
      case 'completed': return '#107c10';
      case 'failed': return '#d13438';
      default: return '#666';
    }
  };

  const modalProps = {
    isOpen,
    onDismiss: handleClose,
    isBlocking: isDeleting,
    containerClassName: 'delete-library-modal'
  };

  return (
    <Modal {...modalProps}>
      <div style={{ padding: '20px', minWidth: '450px', maxWidth: '600px' }}>
        <Stack tokens={{ childrenGap: 20 }}>
          <Stack.Item>
            <Text variant="xLarge" styles={{ root: { fontWeight: 'semibold', color: '#d13438' } }}>
              Delete {isMultipleLibraries ? 'Libraries' : 'Library'}
            </Text>
            <Text variant="medium" styles={{ root: { color: '#666', marginTop: '8px' } }}>
              This action cannot be undone. All content and settings will be permanently deleted.
            </Text>
          </Stack.Item>

          {/* Libraries to be deleted */}
          <Stack.Item>
            <Text variant="mediumPlus" styles={{ root: { fontWeight: 'semibold' } }}>
              {isMultipleLibraries ? 'Libraries to be deleted:' : 'Library to be deleted:'}
            </Text>
            {libraries.map(library => (
              <Stack 
                key={library.id} 
                horizontal 
                verticalAlign="center" 
                tokens={{ childrenGap: 10 }}
                styles={{ root: { marginTop: '8px', padding: '8px', backgroundColor: '#fef9f9', border: '1px solid #fde7e9' } }}
              >
                {isDeleting && (
                  <div style={{ color: getProgressColor(deletionProgress[library.id] || 'pending') }}>
                    {deletionProgress[library.id] === 'deleting' ? (
                      <Spinner size={SpinnerSize.small} />
                    ) : (
                      <Text>{getProgressIcon(deletionProgress[library.id] || 'pending')}</Text>
                    )}
                  </div>
                )}
                <Stack>
                  <Text variant="medium" styles={{ root: { fontWeight: 'semibold' } }}>
                    {library.name}
                  </Text>
                  <Text variant="small" styles={{ root: { color: '#666' } }}>
                    {library.externalUsersCount} external user{library.externalUsersCount !== 1 ? 's' : ''} • 
                    Owner: {library.owner}
                  </Text>
                </Stack>
              </Stack>
            ))}
          </Stack.Item>

          {/* Warning messages */}
          {hasExternalUsers && (
            <Stack.Item>
              <MessageBar messageBarType={MessageBarType.warning}>
                <strong>Warning:</strong> {totalExternalUsers} external user{totalExternalUsers !== 1 ? 's' : ''} 
                {isMultipleLibraries ? ' have' : ' has'} access to {isMultipleLibraries ? 'these libraries' : 'this library'}. 
                They will lose access when the {isMultipleLibraries ? 'libraries are' : 'library is'} deleted.
              </MessageBar>
            </Stack.Item>
          )}

          {error && (
            <Stack.Item>
              <MessageBar messageBarType={MessageBarType.error}>
                <pre style={{ whiteSpace: 'pre-wrap', margin: 0 }}>{error}</pre>
              </MessageBar>
            </Stack.Item>
          )}

          {/* Confirmation checkboxes */}
          <Stack.Item>
            <Checkbox
              label={`I understand that all content in ${isMultipleLibraries ? 'these libraries' : 'this library'} will be permanently deleted`}
              checked={acknowledgeDataLoss}
              onChange={(_, checked) => setAcknowledgeDataLoss(checked || false)}
              disabled={isDeleting}
            />
          </Stack.Item>

          {/* Confirmation text input */}
          <Stack.Item>
            <TextField
              label={`Type "${confirmationPhrase}" to confirm deletion`}
              value={confirmationText}
              onChange={(_, newValue) => setConfirmationText(newValue || '')}
              disabled={isDeleting}
              placeholder={confirmationPhrase}
              styles={{
                fieldGroup: {
                  borderColor: isConfirmationValid ? '#107c10' : undefined
                }
              }}
            />
          </Stack.Item>

          {/* Action buttons */}
          <Stack.Item>
            <Stack horizontal tokens={{ childrenGap: 10 }}>
              <PrimaryButton
                text={isDeleting ? 'Deleting...' : `Delete ${isMultipleLibraries ? 'Libraries' : 'Library'}`}
                onClick={handleDelete}
                disabled={!canDelete}
                iconProps={isDeleting ? undefined : { iconName: 'Delete' }}
                styles={{
                  root: {
                    backgroundColor: '#d13438',
                    borderColor: '#d13438'
                  },
                  rootHovered: {
                    backgroundColor: '#b91b1b',
                    borderColor: '#b91b1b'
                  }
                }}
              />
              {isDeleting && <Spinner size={SpinnerSize.small} />}
              <DefaultButton
                text="Cancel"
                onClick={handleClose}
                disabled={isDeleting}
              />
            </Stack>
          </Stack.Item>

          {/* Progress summary during deletion */}
          {isDeleting && Object.keys(deletionProgress).length > 0 && (
            <Stack.Item>
              <MessageBar messageBarType={MessageBarType.info}>
                Deleting {libraries.length} librar{libraries.length !== 1 ? 'ies' : 'y'}...
                <br />
                Completed: {Object.keys(deletionProgress).filter(id => deletionProgress[id] === 'completed').length}
                {Object.keys(deletionProgress).some(id => deletionProgress[id] === 'failed') && 
                  ` • Failed: ${Object.keys(deletionProgress).filter(id => deletionProgress[id] === 'failed').length}`
                }
              </MessageBar>
            </Stack.Item>
          )}
        </Stack>
      </div>
    </Modal>
  );
};
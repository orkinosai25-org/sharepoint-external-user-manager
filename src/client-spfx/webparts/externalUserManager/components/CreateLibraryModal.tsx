import * as React from 'react';
import { useState } from 'react';
import {
  Modal,
  Stack,
  Text,
  TextField,
  PrimaryButton,
  DefaultButton,
  Checkbox,
  MessageBar,
  MessageBarType,
  Spinner,
  SpinnerSize
} from '@fluentui/react';
import { IExternalLibrary } from '../models/IExternalLibrary';

export interface ICreateLibraryModalProps {
  isOpen: boolean;
  onClose: () => void;
  onLibraryCreated: (library: IExternalLibrary) => void;
  onCreateLibrary: (config: {
    title: string;
    description?: string;
    enableExternalSharing?: boolean;
  }) => Promise<IExternalLibrary>;
}

export interface ICreateLibraryFormData {
  title: string;
  description: string;
  enableExternalSharing: boolean;
}

export const CreateLibraryModal: React.FC<ICreateLibraryModalProps> = ({
  isOpen,
  onClose,
  onLibraryCreated,
  onCreateLibrary
}) => {
  const [formData, setFormData] = useState<ICreateLibraryFormData>({
    title: '',
    description: '',
    enableExternalSharing: false
  });
  
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string>('');
  const [validationErrors, setValidationErrors] = useState<{[key: string]: string}>({});

  const validateForm = (): boolean => {
    const errors: {[key: string]: string} = {};
    
    // Title validation
    if (!formData.title.trim()) {
      errors.title = 'Library name is required';
    } else if (formData.title.length > 100) {
      errors.title = 'Library name must be less than 100 characters';
    } else if (!/^[a-zA-Z0-9\s\-_]+$/.test(formData.title)) {
      errors.title = 'Library name can only contain letters, numbers, spaces, hyphens, and underscores';
    }

    // Description validation
    if (formData.description.length > 500) {
      errors.description = 'Description must be less than 500 characters';
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleInputChange = (field: keyof ICreateLibraryFormData) => 
    (event: React.FormEvent<HTMLInputElement | HTMLTextAreaElement>, newValue?: string) => {
      setFormData(prev => ({
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

  const handleCheckboxChange = (field: keyof ICreateLibraryFormData) =>
    (event?: React.FormEvent<HTMLElement | HTMLInputElement>, checked?: boolean) => {
      setFormData(prev => ({
        ...prev,
        [field]: checked || false
      }));
    };

  const handleCreate = async (): Promise<void> => {
    setError('');
    
    if (!validateForm()) {
      return;
    }

    setIsCreating(true);
    
    try {
      const newLibrary = await onCreateLibrary({
        title: formData.title.trim(),
        description: formData.description.trim() || undefined,
        enableExternalSharing: formData.enableExternalSharing
      });
      
      onLibraryCreated(newLibrary);
      handleClose();
    } catch (err) {
      setError(err.message || 'Failed to create library');
    } finally {
      setIsCreating(false);
    }
  };

  const handleClose = (): void => {
    if (!isCreating) {
      setFormData({
        title: '',
        description: '',
        enableExternalSharing: false
      });
      setError('');
      setValidationErrors({});
      onClose();
    }
  };

  const modalProps = {
    isOpen,
    onDismiss: handleClose,
    isBlocking: isCreating,
    containerClassName: 'create-library-modal'
  };

  return (
    <Modal {...modalProps}>
      <div style={{ padding: '20px', minWidth: '400px', maxWidth: '500px' }}>
        <Stack tokens={{ childrenGap: 20 }}>
          <Stack.Item>
            <Text variant="xLarge" styles={{ root: { fontWeight: 'semibold' } }}>
              Create New Library
            </Text>
            <Text variant="medium" styles={{ root: { color: '#666', marginTop: '8px' } }}>
              Create a new document library for sharing with external users
            </Text>
          </Stack.Item>

          {error && (
            <Stack.Item>
              <MessageBar messageBarType={MessageBarType.error}>
                {error}
              </MessageBar>
            </Stack.Item>
          )}

          <Stack.Item>
            <TextField
              label="Library Name *"
              value={formData.title}
              onChange={handleInputChange('title')}
              disabled={isCreating}
              errorMessage={validationErrors.title}
              placeholder="Enter a name for the library"
              maxLength={100}
              description="The name will be used in URLs and must be unique within this site"
            />
          </Stack.Item>

          <Stack.Item>
            <TextField
              label="Description"
              value={formData.description}
              onChange={handleInputChange('description')}
              disabled={isCreating}
              errorMessage={validationErrors.description}
              placeholder="Enter a description (optional)"
              multiline
              rows={3}
              maxLength={500}
              description="Provide a brief description of the library's purpose"
            />
          </Stack.Item>

          <Stack.Item>
            <Checkbox
              label="Enable external sharing"
              checked={formData.enableExternalSharing}
              onChange={handleCheckboxChange('enableExternalSharing')}
              disabled={isCreating}
            />
            <Text variant="small" styles={{ root: { color: '#666', marginTop: '4px' } }}>
              Allow this library to be shared with users outside your organization
            </Text>
          </Stack.Item>

          {formData.enableExternalSharing && (
            <Stack.Item>
              <MessageBar messageBarType={MessageBarType.warning}>
                External sharing must be enabled at the tenant and site level. 
                Contact your administrator if external sharing is not working.
              </MessageBar>
            </Stack.Item>
          )}

          <Stack.Item>
            <Stack horizontal tokens={{ childrenGap: 10 }}>
              <PrimaryButton
                text={isCreating ? 'Creating...' : 'Create Library'}
                onClick={handleCreate}
                disabled={isCreating || !formData.title.trim()}
                iconProps={isCreating ? undefined : { iconName: 'Add' }}
              />
              {isCreating && <Spinner size={SpinnerSize.small} />}
              <DefaultButton
                text="Cancel"
                onClick={handleClose}
                disabled={isCreating}
              />
            </Stack>
          </Stack.Item>

          {/* Information section */}
          <Stack.Item>
            <MessageBar messageBarType={MessageBarType.info}>
              <Text variant="small">
                <strong>Note:</strong> The new library will be created with default permissions. 
                You can manage permissions and external sharing after creation.
              </Text>
            </MessageBar>
          </Stack.Item>
        </Stack>
      </div>
    </Modal>
  );
};
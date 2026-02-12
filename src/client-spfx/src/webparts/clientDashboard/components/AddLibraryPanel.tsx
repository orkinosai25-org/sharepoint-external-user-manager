import * as React from 'react';
import { useState } from 'react';
import {
  Panel,
  PanelType,
  Stack,
  TextField,
  PrimaryButton,
  DefaultButton,
  Spinner,
  SpinnerSize,
  MessageBar,
  MessageBarType,
  Label
} from '@fluentui/react';

export interface IAddLibraryPanelProps {
  isOpen: boolean;
  clientName: string;
  onDismiss: () => void;
  onLibraryCreated: (libraryName: string, description: string) => Promise<void>;
}

const AddLibraryPanel: React.FC<IAddLibraryPanelProps> = (props) => {
  const [libraryName, setLibraryName] = useState<string>('');
  const [description, setDescription] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [validationErrors, setValidationErrors] = useState<{ [key: string]: string }>({});

  const handleDismiss = (): void => {
    if (!isSubmitting) {
      resetForm();
      props.onDismiss();
    }
  };

  const resetForm = (): void => {
    setLibraryName('');
    setDescription('');
    setErrorMessage('');
    setValidationErrors({});
  };

  const validateForm = (): boolean => {
    const errors: { [key: string]: string } = {};

    if (!libraryName.trim()) {
      errors.libraryName = 'Folder name is required';
    } else if (libraryName.trim().length < 3) {
      errors.libraryName = 'Folder name must be at least 3 characters';
    } else if (libraryName.trim().length > 100) {
      errors.libraryName = 'Folder name must be less than 100 characters';
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (): Promise<void> => {
    setErrorMessage('');
    
    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);

    try {
      await props.onLibraryCreated(libraryName.trim(), description.trim());
      resetForm();
      props.onDismiss();
    } catch (error) {
      console.error('Error creating library:', error);
      setErrorMessage(error instanceof Error ? error.message : 'Failed to create document folder. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const onRenderFooterContent = (): JSX.Element => {
    return (
      <Stack horizontal tokens={{ childrenGap: 10 }}>
        <PrimaryButton
          text="Create Folder"
          onClick={handleSubmit}
          disabled={isSubmitting}
        />
        <DefaultButton
          text="Cancel"
          onClick={handleDismiss}
          disabled={isSubmitting}
        />
      </Stack>
    );
  };

  return (
    <Panel
      isOpen={props.isOpen}
      onDismiss={handleDismiss}
      type={PanelType.medium}
      headerText="Add Document Folder"
      closeButtonAriaLabel="Close"
      onRenderFooterContent={onRenderFooterContent}
      isFooterAtBottom={true}
    >
      <Stack tokens={{ childrenGap: 20 }} style={{ marginTop: 20 }}>
        {errorMessage && (
          <MessageBar
            messageBarType={MessageBarType.error}
            isMultiline={false}
            onDismiss={() => setErrorMessage('')}
          >
            {errorMessage}
          </MessageBar>
        )}

        <Stack tokens={{ childrenGap: 10 }}>
          <Label>Client Workspace</Label>
          <TextField
            value={props.clientName}
            disabled
            readOnly
          />
        </Stack>

        <TextField
          label="Folder Name"
          placeholder="e.g., Contracts, Evidence, Discovery Documents"
          value={libraryName}
          onChange={(_, newValue) => setLibraryName(newValue || '')}
          required
          disabled={isSubmitting}
          errorMessage={validationErrors.libraryName}
          description="Enter a simple, descriptive name for the document folder"
        />

        <TextField
          label="Description (optional)"
          placeholder="Brief description of what will be stored here"
          value={description}
          onChange={(_, newValue) => setDescription(newValue || '')}
          multiline
          rows={3}
          disabled={isSubmitting}
          description="Help team members understand what this folder is for"
        />

        {isSubmitting && (
          <Stack horizontalAlign="center" tokens={{ padding: 20 }}>
            <Spinner size={SpinnerSize.medium} label="Creating document folder..." />
          </Stack>
        )}
      </Stack>
    </Panel>
  );
};

export default AddLibraryPanel;

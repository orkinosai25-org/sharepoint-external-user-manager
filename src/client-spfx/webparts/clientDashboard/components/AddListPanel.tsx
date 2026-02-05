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
  Label,
  Dropdown,
  IDropdownOption,
  Text
} from '@fluentui/react';

export interface IAddListPanelProps {
  isOpen: boolean;
  clientName: string;
  onDismiss: () => void;
  onListCreated: (listName: string, listType: string, description: string) => Promise<void>;
}

// Simplified list types with user-friendly names
const listTypeOptions: IDropdownOption[] = [
  { key: 'GenericList', text: 'Simple List' },
  { key: 'Tasks', text: 'Task List' },
  { key: 'Contacts', text: 'Contacts' },
  { key: 'Events', text: 'Calendar/Events' },
  { key: 'Links', text: 'Links' },
  { key: 'Announcements', text: 'Announcements' },
  { key: 'IssueTracking', text: 'Issue Tracker' }
];

const AddListPanel: React.FC<IAddListPanelProps> = (props) => {
  const [listName, setListName] = useState<string>('');
  const [listType, setListType] = useState<string>('GenericList');
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
    setListName('');
    setListType('GenericList');
    setDescription('');
    setErrorMessage('');
    setValidationErrors({});
  };

  const validateForm = (): boolean => {
    const errors: { [key: string]: string } = {};

    if (!listName.trim()) {
      errors.listName = 'List name is required';
    } else if (listName.trim().length < 3) {
      errors.listName = 'List name must be at least 3 characters';
    } else if (listName.trim().length > 100) {
      errors.listName = 'List name must be less than 100 characters';
    }

    if (!listType) {
      errors.listType = 'Please select a list type';
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
      await props.onListCreated(listName.trim(), listType, description.trim());
      resetForm();
      props.onDismiss();
    } catch (error) {
      console.error('Error creating list:', error);
      setErrorMessage(error instanceof Error ? error.message : 'Failed to create data list. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const onRenderFooterContent = (): JSX.Element => {
    return (
      <Stack horizontal tokens={{ childrenGap: 10 }}>
        <PrimaryButton
          text="Create List"
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
      headerText="Add Data List"
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
          label="List Name"
          placeholder="e.g., Project Tasks, Key Contacts, Important Dates"
          value={listName}
          onChange={(_, newValue) => setListName(newValue || '')}
          required
          disabled={isSubmitting}
          errorMessage={validationErrors.listName}
          description="Enter a simple, descriptive name for the list"
        />

        <Stack tokens={{ childrenGap: 5 }}>
          <Dropdown
            label="List Type"
            placeholder="Select a list type"
            options={listTypeOptions}
            selectedKey={listType}
            onChange={(_, option) => {
              if (option?.key) {
                setListType(option.key as string);
              }
            }}
            required
            disabled={isSubmitting}
            errorMessage={validationErrors.listType}
          />
          <Text variant="small" style={{ color: '#605e5c' }}>
            Choose the type of list that best fits your needs
          </Text>
        </Stack>

        <TextField
          label="Description (optional)"
          placeholder="Brief description of what this list will track"
          value={description}
          onChange={(_, newValue) => setDescription(newValue || '')}
          multiline
          rows={3}
          disabled={isSubmitting}
          description="Help team members understand what this list is for"
        />

        {isSubmitting && (
          <Stack horizontalAlign="center" tokens={{ padding: 20 }}>
            <Spinner size={SpinnerSize.medium} label="Creating data list..." />
          </Stack>
        )}
      </Stack>
    </Panel>
  );
};

export default AddListPanel;

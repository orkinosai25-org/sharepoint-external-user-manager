import * as React from 'react';
import { useState } from 'react';
import {
  Panel,
  PanelType,
  TextField,
  PrimaryButton,
  DefaultButton,
  Stack,
  MessageBar,
  MessageBarType,
  Spinner,
  SpinnerSize
} from '@fluentui/react';

export interface IAddClientPanelProps {
  isOpen: boolean;
  onDismiss: () => void;
  onClientAdded: (clientName: string) => Promise<void>;
}

const AddClientPanel: React.FC<IAddClientPanelProps> = (props) => {
  const [clientName, setClientName] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string>('');

  const handleSubmit = async (): Promise<void> => {
    // Validate client name
    if (!clientName || clientName.trim().length === 0) {
      setErrorMessage('Please enter a client name.');
      return;
    }

    if (clientName.trim().length < 3) {
      setErrorMessage('Client name must be at least 3 characters long.');
      return;
    }

    if (clientName.trim().length > 100) {
      setErrorMessage('Client name must not exceed 100 characters.');
      return;
    }

    setIsSubmitting(true);
    setErrorMessage('');

    try {
      // Call the parent callback to handle the actual creation
      await props.onClientAdded(clientName.trim());
      
      // Close the panel on success
      handleClose();
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : 'Failed to add client. Please try again.';
      setErrorMessage(errorMsg);
      setIsSubmitting(false);
    }
  };

  const handleClose = (): void => {
    // Reset form state
    setClientName('');
    setErrorMessage('');
    setIsSubmitting(false);
    props.onDismiss();
  };

  return (
    <Panel
      isOpen={props.isOpen}
      onDismiss={handleClose}
      type={PanelType.medium}
      headerText="Add New Client"
      closeButtonAriaLabel="Close"
      isBlocking={isSubmitting}
    >
      <Stack tokens={{ childrenGap: 20 }} style={{ marginTop: 20 }}>
        {errorMessage && (
          <MessageBar
            messageBarType={MessageBarType.error}
            isMultiline={false}
            onDismiss={() => setErrorMessage('')}
            dismissButtonAriaLabel="Close"
          >
            {errorMessage}
          </MessageBar>
        )}

        <TextField
          label="Client Name"
          placeholder="Enter client name (e.g., Acme Corporation)"
          value={clientName}
          onChange={(e, newValue) => setClientName(newValue || '')}
          required
          disabled={isSubmitting}
          description="Enter the name of the new client. A dedicated workspace will be created for them."
        />

        {isSubmitting && (
          <MessageBar messageBarType={MessageBarType.info}>
            <Stack horizontal tokens={{ childrenGap: 10 }} verticalAlign="center">
              <Spinner size={SpinnerSize.small} />
              <span>Creating client workspace...</span>
            </Stack>
          </MessageBar>
        )}

        <Stack horizontal tokens={{ childrenGap: 10 }} style={{ marginTop: 20 }}>
          <PrimaryButton
            text="Add Client"
            onClick={handleSubmit}
            disabled={isSubmitting || !clientName.trim()}
          />
          <DefaultButton
            text="Cancel"
            onClick={handleClose}
            disabled={isSubmitting}
          />
        </Stack>
      </Stack>
    </Panel>
  );
};

export default AddClientPanel;

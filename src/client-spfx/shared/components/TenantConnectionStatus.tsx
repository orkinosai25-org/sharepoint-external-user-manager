import * as React from 'react';
import { MessageBar, MessageBarType, Spinner, SpinnerSize, Stack } from '@fluentui/react';

export interface ITenantConnectionStatusProps {
  /** Whether the tenant is connected */
  isConnected: boolean;
  /** Whether the check is still loading */
  isLoading: boolean;
  /** Error message if check failed */
  error?: string;
  /** Portal URL for onboarding */
  portalUrl?: string;
}

/**
 * Component to display tenant connection status
 * 
 * Shows whether the tenant has been onboarded to the SaaS platform
 * and provides guidance if not connected.
 */
export const TenantConnectionStatus: React.FC<ITenantConnectionStatusProps> = (props) => {
  const portalUrl = props.portalUrl || 'https://portal.yourdomain.com/onboarding';

  if (props.isLoading) {
    return (
      <MessageBar messageBarType={MessageBarType.info}>
        <Stack horizontal tokens={{ childrenGap: 8 }} verticalAlign="center">
          <Spinner size={SpinnerSize.small} />
          <span>Checking tenant connection...</span>
        </Stack>
      </MessageBar>
    );
  }

  if (props.error) {
    return (
      <MessageBar messageBarType={MessageBarType.error} isMultiline={true}>
        <strong>Connection Error</strong>
        <p>{props.error}</p>
        <p>Please contact your administrator or try again later.</p>
      </MessageBar>
    );
  }

  if (!props.isConnected) {
    return (
      <MessageBar messageBarType={MessageBarType.severeWarning} isMultiline={true}>
        <strong>Tenant Not Connected</strong>
        <p>
          This tenant has not been onboarded to the SharePoint External User Manager platform.
          Please complete the onboarding process to use this feature.
        </p>
        <a href={portalUrl} target="_blank" rel="noopener noreferrer">
          Start onboarding process
        </a>
      </MessageBar>
    );
  }

  return null; // Don't show anything if connected successfully
};

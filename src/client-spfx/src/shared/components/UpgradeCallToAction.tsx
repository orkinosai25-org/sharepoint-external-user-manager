import * as React from 'react';
import { MessageBar, MessageBarType, Link, Stack, Text } from '@fluentui/react';

export interface IUpgradeCallToActionProps {
  /** Current subscription tier */
  tier: string;
  /** Feature that requires upgrade */
  feature: string;
  /** Portal URL for upgrading */
  portalUrl?: string;
  /** Optional custom message */
  message?: string;
}

/**
 * Component to display upgrade call-to-action when a feature is blocked
 * 
 * Shows a friendly message prompting users to upgrade their subscription
 * to access premium features.
 */
export const UpgradeCallToAction: React.FC<IUpgradeCallToActionProps> = (props) => {
  const portalUrl = props.portalUrl || 'https://portal.yourdomain.com/pricing';
  
  const defaultMessage = `Your current ${props.tier} plan does not include ${props.feature}. ` +
    `Upgrade your subscription to access this feature.`;

  return (
    <MessageBar
      messageBarType={MessageBarType.warning}
      isMultiline={true}
    >
      <Stack tokens={{ childrenGap: 8 }}>
        <Text>{props.message || defaultMessage}</Text>
        <Link href={portalUrl} target="_blank" rel="noopener noreferrer">
          View upgrade options
        </Link>
      </Stack>
    </MessageBar>
  );
};

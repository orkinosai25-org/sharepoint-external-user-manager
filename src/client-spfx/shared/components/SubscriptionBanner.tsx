import * as React from 'react';
import { MessageBar, MessageBarType, Stack, Link } from '@fluentui/react';
import { ISubscriptionStatus } from '../services/SaaSApiClient';

export interface ISubscriptionBannerProps {
  /** Subscription status */
  subscription: ISubscriptionStatus;
  /** Portal URL for managing subscription */
  portalUrl?: string;
}

/**
 * Component to display subscription information banner
 * 
 * Shows the current subscription tier and provides links to manage
 * or upgrade the subscription.
 */
export const SubscriptionBanner: React.FC<ISubscriptionBannerProps> = (props) => {
  const { subscription } = props;
  const portalUrl = props.portalUrl || 'https://portal.yourdomain.com/dashboard';

  // Don't show banner for active paid subscriptions
  if (subscription.isActive && subscription.tier !== 'Starter' && subscription.status !== 'Trial') {
    return null;
  }

  // Trial expiring soon
  if (subscription.status === 'Trial' && subscription.trialExpiry) {
    const daysLeft = Math.ceil((subscription.trialExpiry.getTime() - Date.now()) / (1000 * 60 * 60 * 24));
    
    if (daysLeft <= 7) {
      return (
        <MessageBar
          messageBarType={MessageBarType.warning}
          isMultiline={false}
        >
          <Stack horizontal tokens={{ childrenGap: 8 }} verticalAlign="center">
            <span>
              Your trial expires in <strong>{daysLeft} day{daysLeft !== 1 ? 's' : ''}</strong>.
            </span>
            <Link href={`${portalUrl}#subscription`} target="_blank" rel="noopener noreferrer">
              Upgrade now
            </Link>
          </Stack>
        </MessageBar>
      );
    }
  }

  // Free/Starter plan
  if (subscription.tier === 'Starter' && !subscription.isActive) {
    return (
      <MessageBar
        messageBarType={MessageBarType.info}
        isMultiline={false}
      >
        <Stack horizontal tokens={{ childrenGap: 8 }} verticalAlign="center">
          <span>You're on the <strong>Starter</strong> plan.</span>
          <Link href={`${portalUrl}#subscription`} target="_blank" rel="noopener noreferrer">
            Upgrade for more features
          </Link>
        </Stack>
      </MessageBar>
    );
  }

  return null;
};

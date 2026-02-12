import * as React from 'react';
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
export declare const SubscriptionBanner: React.FC<ISubscriptionBannerProps>;
//# sourceMappingURL=SubscriptionBanner.d.ts.map
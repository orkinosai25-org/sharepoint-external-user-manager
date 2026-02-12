import * as React from 'react';
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
export declare const UpgradeCallToAction: React.FC<IUpgradeCallToActionProps>;
//# sourceMappingURL=UpgradeCallToAction.d.ts.map
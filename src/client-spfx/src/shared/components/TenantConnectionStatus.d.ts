import * as React from 'react';
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
export declare const TenantConnectionStatus: React.FC<ITenantConnectionStatusProps>;
//# sourceMappingURL=TenantConnectionStatus.d.ts.map
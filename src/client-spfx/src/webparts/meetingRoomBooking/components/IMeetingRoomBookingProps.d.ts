import { WebPartContext } from '@microsoft/sp-webpart-base';
export interface IMeetingRoomBookingProps {
    description: string;
    context: WebPartContext;
    defaultLocation: string;
    enableTeamsIntegration: boolean;
    enableRecurringBookings: boolean;
    maxBookingDuration: number;
    requireApproval: boolean;
}
//# sourceMappingURL=IMeetingRoomBookingProps.d.ts.map
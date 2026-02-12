import { Version } from '@microsoft/sp-core-library';
import { IPropertyPaneConfiguration } from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';
export interface IMeetingRoomBookingWebPartProps {
    description: string;
    defaultLocation: string;
    enableTeamsIntegration: boolean;
    enableRecurringBookings: boolean;
    maxBookingDuration: number;
    requireApproval: boolean;
}
export default class MeetingRoomBookingWebPart extends BaseClientSideWebPart<IMeetingRoomBookingWebPartProps> {
    render(): void;
    onDispose(): void;
    protected get dataVersion(): Version;
    protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration;
}
//# sourceMappingURL=MeetingRoomBookingWebPart.d.ts.map
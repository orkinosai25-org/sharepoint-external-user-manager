import { Version } from '@microsoft/sp-core-library';
import { IPropertyPaneConfiguration } from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';
export interface ITimesheetManagementWebPartProps {
    description: string;
}
export default class TimesheetManagementWebPart extends BaseClientSideWebPart<ITimesheetManagementWebPartProps> {
    render(): void;
    onDispose(): void;
    protected get dataVersion(): Version;
    protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration;
}
//# sourceMappingURL=TimesheetManagementWebPart.d.ts.map
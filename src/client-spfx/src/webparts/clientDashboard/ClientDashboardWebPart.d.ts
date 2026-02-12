import { Version } from '@microsoft/sp-core-library';
import { IPropertyPaneConfiguration } from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';
export interface IClientDashboardWebPartProps {
    description: string;
}
export default class ClientDashboardWebPart extends BaseClientSideWebPart<IClientDashboardWebPartProps> {
    render(): void;
    onDispose(): void;
    protected get dataVersion(): Version;
    protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration;
}
//# sourceMappingURL=ClientDashboardWebPart.d.ts.map
import { Version } from '@microsoft/sp-core-library';
import { IPropertyPaneConfiguration } from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';
export interface IExternalUserManagerWebPartProps {
    description: string;
    backendApiUrl: string;
    portalUrl: string;
}
export default class ExternalUserManagerWebPart extends BaseClientSideWebPart<IExternalUserManagerWebPartProps> {
    render(): void;
    onDispose(): void;
    protected get dataVersion(): Version;
    protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration;
}
//# sourceMappingURL=ExternalUserManagerWebPart.d.ts.map
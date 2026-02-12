import { Version } from '@microsoft/sp-core-library';
import { IPropertyPaneConfiguration } from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';
export interface IAiPoweredFaqWebPartProps {
    description: string;
    azureOpenAiEndpoint: string;
    azureOpenAiApiKey: string;
    enableAiSuggestions: boolean;
    enableAnalytics: boolean;
}
export default class AiPoweredFaqWebPart extends BaseClientSideWebPart<IAiPoweredFaqWebPartProps> {
    render(): void;
    onDispose(): void;
    protected get dataVersion(): Version;
    protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration;
}
//# sourceMappingURL=AiPoweredFaqWebPart.d.ts.map
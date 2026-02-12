import { Version } from '@microsoft/sp-core-library';
import { IPropertyPaneConfiguration } from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';
export interface IInventoryProductCatalogWebPartProps {
    description: string;
}
export default class InventoryProductCatalogWebPart extends BaseClientSideWebPart<IInventoryProductCatalogWebPartProps> {
    render(): void;
    onDispose(): void;
    protected get dataVersion(): Version;
    protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration;
}
//# sourceMappingURL=InventoryProductCatalogWebPart.d.ts.map
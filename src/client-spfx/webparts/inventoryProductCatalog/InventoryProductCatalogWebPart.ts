import * as React from 'react';
import * as ReactDom from 'react-dom';
import { Version } from '@microsoft/sp-core-library';
import {
  IPropertyPaneConfiguration,
  PropertyPaneTextField
} from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';

import strings from 'InventoryProductCatalogWebPartStrings';
import InventoryProductCatalog from './components/InventoryProductCatalog';
import { IInventoryProductCatalogProps } from './components/IInventoryProductCatalogProps';

export interface IInventoryProductCatalogWebPartProps {
  description: string;
}

export default class InventoryProductCatalogWebPart extends BaseClientSideWebPart<IInventoryProductCatalogWebPartProps> {

  public render(): void {
    const element: React.ReactElement<IInventoryProductCatalogProps> = React.createElement(
      InventoryProductCatalog,
      {
        description: this.properties.description,
        context: this.context
      }
    );

    ReactDom.render(element, this.domElement);
  }

  public onDispose(): void {
    ReactDom.unmountComponentAtNode(this.domElement);
  }

  protected get dataVersion(): Version {
    return Version.parse('1.0');
  }

  protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration {
    return {
      pages: [
        {
          header: {
            description: strings.PropertyPaneDescription
          },
          groups: [
            {
              groupName: strings.BasicGroupName,
              groupFields: [
                PropertyPaneTextField('description', {
                  label: strings.DescriptionFieldLabel
                })
              ]
            }
          ]
        }
      ]
    };
  }
}
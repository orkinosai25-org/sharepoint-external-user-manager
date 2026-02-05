import * as React from 'react';
import * as ReactDom from 'react-dom';
import { Version } from '@microsoft/sp-core-library';
import {
  IPropertyPaneConfiguration,
  PropertyPaneTextField,
  PropertyPaneToggle
} from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';

import strings from 'AiPoweredFaqWebPartStrings';
import AiPoweredFaq from './components/AiPoweredFaq';
import { IAiPoweredFaqProps } from './components/IAiPoweredFaqProps';

export interface IAiPoweredFaqWebPartProps {
  description: string;
  azureOpenAiEndpoint: string;
  azureOpenAiApiKey: string;
  enableAiSuggestions: boolean;
  enableAnalytics: boolean;
}

export default class AiPoweredFaqWebPart extends BaseClientSideWebPart<IAiPoweredFaqWebPartProps> {

  public render(): void {
    const element: React.ReactElement<IAiPoweredFaqProps> = React.createElement(
      AiPoweredFaq,
      {
        description: this.properties.description,
        context: this.context,
        azureOpenAiEndpoint: this.properties.azureOpenAiEndpoint,
        azureOpenAiApiKey: this.properties.azureOpenAiApiKey,
        enableAiSuggestions: this.properties.enableAiSuggestions,
        enableAnalytics: this.properties.enableAnalytics
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
            },
            {
              groupName: strings.AzureAiGroupName,
              groupFields: [
                PropertyPaneTextField('azureOpenAiEndpoint', {
                  label: strings.AzureOpenAiEndpointLabel
                }),
                PropertyPaneTextField('azureOpenAiApiKey', {
                  label: strings.AzureOpenAiApiKeyLabel
                }),
                PropertyPaneToggle('enableAiSuggestions', {
                  label: strings.EnableAiSuggestionsLabel,
                  onText: 'Enabled',
                  offText: 'Disabled'
                }),
                PropertyPaneToggle('enableAnalytics', {
                  label: strings.EnableAnalyticsLabel,
                  onText: 'Enabled',
                  offText: 'Disabled'
                })
              ]
            }
          ]
        }
      ]
    };
  }
}
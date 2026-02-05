import * as React from 'react';
import * as ReactDom from 'react-dom';
import { Version } from '@microsoft/sp-core-library';
import {
  IPropertyPaneConfiguration,
  PropertyPaneTextField,
  PropertyPaneToggle,
  PropertyPaneDropdown
} from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';

import strings from 'MeetingRoomBookingWebPartStrings';
import MeetingRoomBooking from './components/MeetingRoomBooking';
import { IMeetingRoomBookingProps } from './components/IMeetingRoomBookingProps';

export interface IMeetingRoomBookingWebPartProps {
  description: string;
  defaultLocation: string;
  enableTeamsIntegration: boolean;
  enableRecurringBookings: boolean;
  maxBookingDuration: number;
  requireApproval: boolean;
}

export default class MeetingRoomBookingWebPart extends BaseClientSideWebPart<IMeetingRoomBookingWebPartProps> {

  public render(): void {
    const element: React.ReactElement<IMeetingRoomBookingProps> = React.createElement(
      MeetingRoomBooking,
      {
        description: this.properties.description,
        context: this.context,
        defaultLocation: this.properties.defaultLocation,
        enableTeamsIntegration: this.properties.enableTeamsIntegration,
        enableRecurringBookings: this.properties.enableRecurringBookings,
        maxBookingDuration: this.properties.maxBookingDuration,
        requireApproval: this.properties.requireApproval
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
                }),
                PropertyPaneTextField('defaultLocation', {
                  label: strings.DefaultLocationLabel
                })
              ]
            },
            {
              groupName: strings.BookingSettingsGroupName,
              groupFields: [
                PropertyPaneToggle('enableTeamsIntegration', {
                  label: strings.EnableTeamsIntegrationLabel,
                  onText: 'Enabled',
                  offText: 'Disabled'
                }),
                PropertyPaneToggle('enableRecurringBookings', {
                  label: strings.EnableRecurringBookingsLabel,
                  onText: 'Enabled',
                  offText: 'Disabled'
                }),
                PropertyPaneDropdown('maxBookingDuration', {
                  label: strings.MaxBookingDurationLabel,
                  options: [
                    { key: 2, text: '2 hours' },
                    { key: 4, text: '4 hours' },
                    { key: 8, text: '8 hours' },
                    { key: 24, text: '1 day' }
                  ]
                }),
                PropertyPaneToggle('requireApproval', {
                  label: strings.RequireApprovalLabel,
                  onText: 'Required',
                  offText: 'Not Required'
                })
              ]
            }
          ]
        }
      ]
    };
  }
}
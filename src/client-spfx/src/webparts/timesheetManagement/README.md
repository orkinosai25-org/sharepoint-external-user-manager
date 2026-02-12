# Timesheet Management Web Part

## Overview

The Timesheet Management web part provides a comprehensive solution for tracking and managing employee timesheets within SharePoint. Built with React and Fluent UI, this web part enables employees to submit time entries and managers to review and approve timesheets efficiently.

## Features

### Core Functionality
- **Timesheet Creation**: Employees can create and submit weekly timesheets
- **Time Entry Management**: Detailed time tracking by project, task, and hours
- **Approval Workflow**: Multi-level approval process for timesheet submissions
- **Status Tracking**: Real-time status updates (Draft, Submitted, Approved, Rejected)
- **Reporting Dashboard**: Summary views of timesheet data and analytics

### User Experience
- **Intuitive Interface**: Clean, modern UI using Fluent UI components
- **Responsive Design**: Works seamlessly on desktop, tablet, and mobile devices
- **Bulk Operations**: Select multiple timesheets for batch approval/rejection
- **Search & Filter**: Advanced filtering by employee, date range, and status
- **Export Capabilities**: Export timesheet data to Excel or PDF

### Integration Points
- **SharePoint Lists**: Stores timesheet data in SharePoint Lists for easy management
- **Microsoft Teams**: Embedded support for Teams integration
- **Power Automate**: Automated workflows for notifications and approvals
- **Microsoft Graph**: Integration with user profiles and organizational data
- **Azure AD**: Seamless authentication and role-based access control

## Setup Instructions

### Prerequisites
- SharePoint Online environment
- SharePoint Framework (SPFx) 1.18.2 or higher
- Node.js 16.x or 18.x
- Appropriate permissions to deploy web parts

### Installation Steps

1. **Deploy the Solution**
   ```bash
   npm install
   npm run build
   npm run package-solution
   ```

2. **Upload to App Catalog**
   - Navigate to SharePoint Admin Center
   - Go to More Features > Apps > App Catalog
   - Upload the `.sppkg` file from `sharepoint/solution/`

3. **Add to SharePoint Site**
   - Go to Site Contents > Add an App
   - Find "Timesheet Management" and add it
   - Add the web part to pages using the web part picker

### Configuration

#### Web Part Properties
- **Description**: Customize the web part description
- **Default View**: Set default timesheet view (My Timesheets, Team Timesheets, All Timesheets)
- **Approval Settings**: Configure approval workflow requirements
- **Export Options**: Enable/disable export functionality

#### SharePoint List Setup
The web part automatically creates the following SharePoint lists if they don't exist:
- **Timesheets**: Main timesheet data
- **TimeEntries**: Individual time entries
- **Projects**: Available projects for time tracking
- **Tasks**: Available tasks per project

#### Permissions Configuration
- **Employees**: Create and edit own timesheets
- **Managers**: Approve/reject team timesheets
- **HR/Admins**: Full access to all timesheet data

## Usage Examples

### Employee Workflow
1. **Create Timesheet**: Click "New Timesheet" to start a new weekly timesheet
2. **Add Time Entries**: Add daily time entries with project, task, and hours
3. **Submit for Approval**: Submit completed timesheet for manager review
4. **Track Status**: Monitor approval status and receive notifications

### Manager Workflow
1. **Review Submissions**: View pending timesheets from team members
2. **Approve/Reject**: Approve accurate timesheets or reject with comments
3. **Bulk Actions**: Process multiple timesheets simultaneously
4. **Generate Reports**: Export timesheet data for payroll or billing

### Administrator Setup
1. **Configure Projects**: Set up available projects and tasks
2. **Manage Users**: Assign roles and permissions
3. **Customize Workflows**: Configure approval processes
4. **Monitor Usage**: Track web part usage and performance

## Teams Integration

### Installation in Teams
1. **Package for Teams**: The web part is pre-configured for Teams compatibility
2. **Add as Tab**: Add to Teams channels as a custom tab
3. **Personal App**: Install as a personal app for individual use

### Teams-Specific Features
- **@mentions**: Notify managers about timesheet submissions
- **Adaptive Cards**: Rich notifications in Teams channels
- **Bot Integration**: Future integration with Teams bots for voice commands

## Technical Architecture

### Frontend Components
- **TimesheetManagement.tsx**: Main React component
- **CreateTimesheetModal.tsx**: Modal for creating new timesheets
- **TimeEntryForm.tsx**: Form for individual time entries
- **ApprovalPanel.tsx**: Manager approval interface

### Data Models
- **ITimesheetEntry**: Main timesheet data structure
- **ITimeEntry**: Individual time entry model
- **IProject**: Project information model
- **ITask**: Task definition model

### Services
- **TimesheetDataService**: SharePoint data operations
- **ApprovalService**: Approval workflow logic
- **NotificationService**: User notifications
- **ExportService**: Data export functionality

## Customization Options

### Styling
- Modify `TimesheetManagement.module.scss` for custom styling
- Override Fluent UI theme tokens for organization branding
- Responsive design supports mobile-first approach

### Business Logic
- Extend `MockTimesheetDataService.ts` with real SharePoint integration
- Add custom validation rules for time entries
- Implement advanced reporting features

### Integration Extensions
- Connect to external HR systems
- Integrate with project management tools
- Add custom approval workflows

## Troubleshooting

### Common Issues
1. **Permission Errors**: Verify user has contribute access to SharePoint lists
2. **Loading Issues**: Check SharePoint list creation and permissions
3. **Teams Integration**: Ensure proper app manifest configuration

### Debug Mode
Enable debug mode in web part properties to see detailed logging information.

### Support Resources
- Check SharePoint Framework documentation
- Review Fluent UI component library
- Consult Teams development guidelines

## Future Enhancements

### Planned Features
- **AI-Powered Insights**: Automatic project suggestions based on historical data
- **Mobile App**: Dedicated mobile application for time tracking
- **Voice Integration**: Voice-to-text time entry capabilities
- **Advanced Analytics**: Machine learning-powered reporting and forecasting

### Roadmap
- Q2 2024: Enhanced mobile experience
- Q3 2024: AI integration and smart suggestions
- Q4 2024: Advanced reporting and analytics dashboard

## Contributing

To contribute to this web part:
1. Fork the repository
2. Create a feature branch
3. Follow TypeScript and React best practices
4. Submit a pull request with detailed description

## License

This project is licensed under the MIT License - see the LICENSE file for details.
# Client Dashboard WebPart

## Overview

The Client Dashboard is a SharePoint Framework (SPFx) webpart that provides a firm-level dashboard for viewing and managing all clients. It's designed for legal firms and professional services organizations to easily access and manage their client spaces in SharePoint.

## Features

### ✅ User-Friendly Interface
- Clean, professional table/list view of all clients
- Non-technical language throughout ("Client" instead of "Site")
- Intuitive design suitable for users with no SharePoint knowledge

### 📊 Client Information Display
- **Client Name**: Clearly displayed with emphasis
- **Site URL**: Clickable links to client SharePoint sites
- **Status**: Visual status indicators with color coding
  - 🟢 Active (Green)
  - 🟠 Provisioning (Orange)
  - 🔴 Error (Red)
- **Actions**: Quick action buttons
  - **Open**: Opens the client's SharePoint site in a new tab
  - **Manage**: Access client management features

### 🔄 Data Loading
- Loads client data from the SaaS Backend API
- Automatic fallback to mock data for development/demo
- Graceful error handling with user-friendly messages
- Refresh capability to reload data

### 📱 Responsive Design
- Works on desktop and mobile devices
- Adaptive layout using Fluent UI components

## Architecture

### Components

```
src/webparts/clientDashboard/
├── ClientDashboardWebPart.ts          # SPFx webpart entry point
├── ClientDashboardWebPart.manifest.json # Webpart configuration
├── components/
│   ├── ClientDashboard.tsx            # Main React component
│   ├── ClientDashboard.module.scss    # Styling
│   └── IClientDashboardProps.ts       # Props interface
├── models/
│   └── IClient.ts                     # Client data model
├── services/
│   ├── ClientDataService.ts           # Backend API service
│   └── MockClientDataService.ts       # Mock data for development
└── loc/
    ├── en-us.ts                       # English localization
    └── mystrings.d.ts                 # String type definitions
```

### Data Flow

1. **Component Mount**: ClientDashboard component loads
2. **API Call**: ClientDataService attempts to fetch data from backend API
3. **Authentication**: Uses AAD token provider for secure API access
4. **Fallback**: If API fails, automatically falls back to MockClientDataService
5. **Display**: Data rendered in Fluent UI DetailsList component
6. **Actions**: Users can interact with Open and Manage buttons

## Usage

### Adding to a SharePoint Page

1. Navigate to a SharePoint page
2. Edit the page
3. Click "+" to add a web part
4. Search for "Client Dashboard"
5. Add the web part to the page
6. Save and publish the page

### User Actions

#### Opening a Client Site
- Click the "Open" button (⧉ icon) next to any client
- The client's SharePoint site opens in a new browser tab
- Only available for clients with "Active" status

#### Managing a Client
- Click the "Manage" button (⚙ icon) next to any client
- Opens client management interface (to be implemented)

#### Refreshing Data
- Click the "Refresh" button in the command bar
- Reloads client data from the backend API

#### Getting Help
- Click the "Help" button (ⓘ icon) in the command bar
- Displays help information about the dashboard features

## Configuration

### Backend API Integration

The webpart connects to the SaaS backend API to fetch client data. The API endpoint is configured in `ClientDataService.ts`:

```typescript
this.baseUrl = process.env.BACKEND_API_URL || 'https://your-backend-api.azurewebsites.net/api';
```

#### API Endpoint
- **URL**: `GET /clients`
- **Authentication**: Azure AD JWT token
- **Response**: Array of client objects

#### Client Object Structure
```typescript
{
  id: number;
  tenantId: number;
  clientName: string;
  siteUrl: string;
  siteId: string;
  createdBy: string;
  createdAt: string;
  status: 'Active' | 'Provisioning' | 'Error';
  errorMessage?: string;
}
```

### Mock Data

For development and testing, the webpart includes mock data service (`MockClientDataService`) with 5 sample clients:
- Acme Corporation
- Global Industries Ltd
- Tech Innovations Inc
- Metro Properties Group
- Healthcare Solutions Partners

## Technical Details

### Dependencies
- **@microsoft/sp-core-library**: ^1.18.2
- **@microsoft/sp-webpart-base**: ^1.18.2
- **@fluentui/react**: ^8.110.10
- **React**: ^17.0.1

### Browser Support
- Modern browsers (Chrome, Edge, Firefox, Safari)
- Internet Explorer 11 (with polyfills)

### Responsive Breakpoints
- Mobile: < 768px
- Desktop: ≥ 768px

## Development

### Local Development
```bash
# Install dependencies
npm install

# Start development server
npm run serve

# Build for production
npm run build

# Package solution
npm run package-solution
```

### Testing
```bash
# Access the workbench
https://your-tenant.sharepoint.com/_layouts/15/workbench.aspx

# Add the webpart to test it
```

## Security

### Authentication
- Uses Azure AD authentication via SPFx context
- Token automatically obtained from `aadTokenProviderFactory`
- Secure API calls with JWT bearer tokens

### Permissions
- Requires user to have access to the SharePoint site
- Backend API enforces tenant-level permissions
- Client data filtered based on user's tenant

## Acceptance Criteria

✅ **Loads from SaaS API**: Integrates with backend `/clients` endpoint

✅ **Non-technical language**: Uses "Client" instead of "Site" throughout the UI

✅ **Works for non-SharePoint users**: Simple, intuitive interface with clear labels and help text

✅ **Table/List view**: Clean DetailsList with all required columns

✅ **Actions**: Open and Manage buttons for each client

✅ **Status indicators**: Visual status display with color coding

## Future Enhancements

- Client creation interface
- Advanced filtering and search
- Bulk actions on multiple clients
- Export to Excel
- Client analytics and insights
- Integration with Teams

## Support

For issues or questions, contact your system administrator or the development team.

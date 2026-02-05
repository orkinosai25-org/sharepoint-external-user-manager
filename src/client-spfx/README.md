# SharePoint Framework (SPFx) Client

## Overview

This directory contains the SharePoint Framework web parts that are installed by customers in their SharePoint Online tenants. The SPFx client provides the user interface for managing external users, client spaces, libraries, and lists.

## Features

- **Client Dashboard**: Firm-level dashboard for viewing and managing all client spaces
- **External User Manager**: Track and manage external users with permissions
- **Library Management**: Create and manage document libraries
- **List Management**: Create and manage SharePoint lists
- **Company & Project Metadata**: Associate external users with companies and projects
- **Responsive Design**: Works across desktop and mobile devices

## Technology Stack

- **Framework**: SharePoint Framework (SPFx) 1.18.2
- **UI Library**: Fluent UI (Fabric) 8.x
- **Language**: TypeScript 4.5.5
- **Styling**: SCSS Modules
- **Build**: Gulp-based SPFx build pipeline

## Project Structure

```
src/client-spfx/
├── webparts/                           # Web part components
│   ├── clientDashboard/                # Client dashboard web part
│   │   ├── components/                 # React components
│   │   ├── models/                     # Data models
│   │   ├── services/                   # API services
│   │   └── ClientDashboardWebPart.ts   # Web part definition
│   │
│   ├── externalUserManager/            # External user management
│   │   ├── components/                 # React components
│   │   ├── models/                     # Data models
│   │   ├── services/                   # API and data services
│   │   └── ExternalUserManagerWebPart.ts
│   │
│   └── [other web parts]/              # Additional web parts
│
├── config/                             # SPFx configuration files
│   ├── config.json                     # Build configuration
│   ├── package-solution.json           # Solution packaging config
│   └── serve.json                      # Dev server configuration
│
├── gulpfile.js                         # Gulp build tasks
├── tsconfig.json                       # TypeScript configuration
└── package.json                        # Node.js dependencies
```

## Prerequisites

- **Node.js**: Version 16.x or 18.x 
  - ⚠️ SPFx 1.18.2 does not support Node.js 20+
  - Recommended: Node.js 18 LTS
- **SharePoint Framework CLI**: 
  ```bash
  npm install -g @microsoft/sharepoint-framework-yeoman-generator
  ```
- **Gulp CLI**: 
  ```bash
  npm install -g gulp-cli
  ```

## Quick Start

### Installation

```bash
# Navigate to client directory
cd src/client-spfx

# Install dependencies
npm install
```

### Development

```bash
# Start development server with hot reload
npm run serve

# The dev server will start at https://localhost:4321
# Access the workbench at: https://localhost:4321/temp/workbench.html

# To add to SharePoint, append to your site URL:
# ?debug=true&noredir=true&debugManifestsFile=https://localhost:4321/temp/manifests.js
```

### Build for Production

```bash
# Build the solution
npm run build

# Create deployment package (.sppkg)
npm run package-solution

# Package location: sharepoint/solution/sharepoint-external-user-manager.sppkg
```

### Automated Setup

For new developers, setup scripts are provided:

**Windows:**
```cmd
setup.cmd
```

**macOS/Linux:**
```bash
chmod +x setup.sh
./setup.sh
```

## Available Scripts

| Script | Command | Description |
|--------|---------|-------------|
| **serve** | `npm run serve` | Start development server |
| **build** | `npm run build` | Build solution for production |
| **clean** | `npm run clean` | Clean build artifacts |
| **test** | `npm run test` | Run unit tests |
| **package-solution** | `npm run package-solution` | Create .sppkg package |

## Web Parts

### 1. Client Dashboard
**Purpose**: Firm-level dashboard showing all client spaces

**Features**:
- View all clients in a table
- Quick actions: Open site, Manage settings
- Non-technical language suitable for legal professionals
- Automatic fallback from API to mock data

**Location**: `webparts/clientDashboard/`

### 2. External User Manager
**Purpose**: Manage external users and their access to libraries

**Features**:
- List all external libraries
- View external user counts and permissions
- Add/remove external users
- Track company and project metadata
- Manage user access (Read/Edit)

**Location**: `webparts/externalUserManager/`

### 3. Additional Web Parts
- **AI-Powered FAQ**: Intelligent FAQ system
- **Inventory Product Catalogue**: Product management
- **Meeting Room Booking**: Room reservation system
- **Timesheet Management**: Time tracking

## API Integration

The web parts integrate with the backend API hosted at `/src/api-dotnet`.

### Service Layer

Each web part includes:
- **API Service**: Calls backend SaaS API
- **Mock Service**: Provides sample data for development/demo
- **Fallback Logic**: Automatically uses mock data if API unavailable

Example:
```typescript
// Backend API call (production)
const clients = await ClientDataService.getClients(tenantId);

// Fallback to mock data (development/demo)
const clients = await MockClientDataService.getClients();
```

## Configuration

### Backend API URL

Set the backend API URL in the web part properties or via SharePoint tenant properties:

```typescript
// In WebPart.ts
export interface IWebPartProps {
  apiUrl: string; // Default: process.env.BACKEND_API_URL
}
```

### Authentication

SPFx uses the current user's SharePoint context. The backend API validates tokens issued by Azure AD.

## Deployment

### Manual Deployment

1. **Build the package**:
   ```bash
   npm run build
   npm run package-solution
   ```

2. **Upload to SharePoint**:
   - Go to SharePoint Admin Center
   - Navigate to: Apps → App Catalog
   - Upload `sharepoint/solution/*.sppkg`

3. **Deploy**:
   - Click "Deploy" when prompted
   - Choose tenant-wide deployment (optional)

4. **Add to site**:
   - Navigate to your SharePoint site
   - Edit a page
   - Add web part: Search for "External User Manager" or "Client Dashboard"

### Automated Deployment (CI/CD)

GitHub Actions workflow handles automated deployment:

See: [`.github/workflows/README.md`](../../.github/workflows/README.md)

## Development Guidelines

### Code Style

- **TypeScript**: Strict mode enabled
- **Linting**: ESLint with SPFx configuration
- **Formatting**: Follow existing code patterns
- **Comments**: Add comments for complex logic only

### Component Structure

```typescript
// Component structure example
import * as React from 'react';
import styles from './Component.module.scss';

export interface IComponentProps {
  // Props interface
}

export interface IComponentState {
  // State interface
}

export default class Component extends React.Component<IComponentProps, IComponentState> {
  constructor(props: IComponentProps) {
    super(props);
    this.state = {
      // Initial state
    };
  }

  public render(): React.ReactElement<IComponentProps> {
    return (
      <div className={styles.container}>
        {/* Component JSX */}
      </div>
    );
  }
}
```

### Adding New Web Parts

```bash
# Use Yeoman generator
yo @microsoft/sharepoint

# Follow prompts:
# - Solution name: [current]
# - Web part name: [your web part name]
# - Framework: React
```

## Testing

### Unit Tests

```bash
# Run tests
npm test

# Run tests with coverage
npm test -- --coverage
```

### Manual Testing

1. Start the dev server: `npm run serve`
2. Navigate to: `https://localhost:4321/temp/workbench.html`
3. Add your web part to the workbench
4. Test functionality

### SharePoint Testing

1. Build and package: `npm run package-solution`
2. Upload to SharePoint App Catalog
3. Add to a test site
4. Verify functionality in SharePoint environment

## Troubleshooting

### Node Version Issues

```bash
# Check Node version
node --version

# Should be 16.x or 18.x
# Use nvm to switch versions if needed
nvm use 18
```

### Build Failures

```bash
# Clean and rebuild
npm run clean
rm -rf node_modules package-lock.json
npm install
npm run build
```

### Certificate Trust Issues (HTTPS)

```bash
# Trust the dev certificate
gulp trust-dev-cert
```

## Contributing

When making changes:

1. ✅ Follow existing code patterns
2. ✅ Test locally before committing
3. ✅ Update documentation if needed
4. ✅ Use UK English for user-facing text
5. ✅ Keep language solicitor-friendly (Client, Space, Access)

## Next Steps

- **ISSUE-09**: Refactor to thin SaaS client (all operations via backend API)
- Remove direct Graph API calls from SPFx
- Add subscription status checks
- Add upgrade CTAs for trial/expired subscriptions

## Resources

- [SPFx Documentation](https://docs.microsoft.com/en-us/sharepoint/dev/spfx/sharepoint-framework-overview)
- [Fluent UI Documentation](https://developer.microsoft.com/en-us/fluentui)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/handbook/intro.html)
- [React Documentation](https://reactjs.org/docs/getting-started.html)

---

**For questions or support, see the [Developer Guide](../../DEVELOPER_GUIDE.md)**

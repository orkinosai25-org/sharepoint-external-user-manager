# SharePoint External User Manager

A modern SharePoint Framework (SPFx) web part built with React and Fluent UI for managing external users and shared libraries with comprehensive metadata tracking.

## Features

- **Modern UI**: Built with Fluent UI (Fabric) components for a clean, professional interface
- **Library Management**: View and manage external libraries with detailed information
- **User Management**: Track external users and their access permissions
- **📊 Metadata Tracking**: Company and Project metadata for external users
- **Responsive Design**: Works across desktop and mobile devices
- **Modular Architecture**: Ready for backend integration

## 🆕 Company and Project Metadata Features

### User Organization
- **Company Tracking**: Associate external users with their companies/organizations
- **Project Assignment**: Link users to specific projects or initiatives
- **Visual Display**: Company and Project columns in user management interface

### Management Capabilities
- **Add Users with Metadata**: Specify company and project when inviting new users
- **Bulk Operations**: Apply metadata to multiple users simultaneously
- **Edit Metadata**: Update company and project information for existing users
- **Persistent Storage**: Metadata stored in browser localStorage (demo) or SharePoint lists (production)

### Use Cases
- **Project Management**: Track which external users belong to which projects
- **Vendor Management**: Organize users by their companies for relationship management
- **Compliance**: Maintain records of user affiliations for audit purposes
- **Access Reviews**: Easily identify users by company/project during reviews

## Project Structure

```
src/
├── webparts/
│   └── externalUserManager/
│       ├── components/
│       │   ├── ExternalUserManager.tsx       # Main React component
│       │   ├── ExternalUserManager.module.scss # Styling
│       │   └── IExternalUserManagerProps.ts  # Component props interface
│       ├── models/
│       │   └── IExternalLibrary.ts           # Data models
│       ├── services/
│       │   └── MockDataService.ts            # Mock data service
│       ├── loc/                              # Localization files
│       └── ExternalUserManagerWebPart.ts     # SPFx web part class
```

## Key Components

### External User Manager Component
- Displays libraries in a responsive DetailsList
- Shows external user counts, permissions, and ownership
- Provides action buttons for library management
- Includes loading states and status information

### Mock Data Service
- Provides sample data for 5 external libraries
- Includes realistic user scenarios and permission levels
- Ready to be replaced with actual SharePoint API calls

### Features Implemented

1. **Library Listing**: Clean list view of external libraries with:
   - Library name and description
   - Site URL and owner information
   - External user counts
   - Permission levels with color coding
   - Last modified dates

2. **Action Placeholders**: 
   - Add Library button
   - Remove selected libraries
   - Manage Users for individual libraries
   - Refresh data functionality

3. **Selection Management**: Multi-select support with action button states

4. **Responsive Design**: Mobile-friendly layout with Fluent UI components

## Getting Started

### Quick Setup
For new developers, we provide setup scripts to help with environment configuration:

**Windows:**
```cmd
setup.cmd
```

**macOS/Linux:**
```bash
./setup.sh
```

### Prerequisites
- **Node.js**: Version 16.x or 18.x (⚠️ SPFx 1.18.2 doesn't support Node 20+)
- **SharePoint Framework CLI**: `npm install -g @microsoft/sharepoint-framework-yeoman-generator`
- **Gulp CLI**: `npm install -g gulp-cli`

### Installation
```bash
# Clone and setup
git clone <repository-url>
cd sharepoint-external-user-manager
npm install
```

### Development
```bash
# Start development server with hot reload
npm run serve

# Access at: https://localhost:4321/temp/manifests.js
# Add to SharePoint with: ?debug=true&noredir=true&debugManifestsFile=https://localhost:4321/temp/manifests.js
```

### Build & Package
```bash
# Build for production
npm run build

# Create deployment package
npm run package-solution

# Package will be in sharepoint/solution/
```

### Automated Deployment
🚀 **GitHub Actions workflow available for automated deployment!**

The project includes a complete CI/CD pipeline that automatically:
- Builds and packages the SPFx solution
- Deploys to SharePoint App Catalog
- Publishes the solution for use

See [GitHub Actions Workflow Documentation](.github/workflows/README.md) for setup instructions.

### New Developer Onboarding
📖 **See [DEVELOPER_GUIDE.md](./DEVELOPER_GUIDE.md) for comprehensive setup and development instructions.**

## 🚀 SaaS Backend (NEW!)

The project now includes a complete multi-tenant SaaS backend built with Azure Functions!

### Backend Features
- ✅ **Multi-tenant Architecture**: Database-per-tenant isolation
- ✅ **Azure Functions**: Serverless API with auto-scaling
- ✅ **Subscription Management**: Trial/Pro/Enterprise tiers with licensing
- ✅ **Authentication**: Azure AD multi-tenant with JWT validation
- ✅ **Rate Limiting**: Per-tenant throttling and quota enforcement
- ✅ **Audit Logging**: Comprehensive audit trail in Cosmos DB
- ✅ **Infrastructure as Code**: Bicep templates for Azure deployment

### Backend Tech Stack
- **Runtime**: Node.js 18 LTS
- **Framework**: Azure Functions v4
- **Language**: TypeScript
- **Database**: Azure Cosmos DB (metadata) + Azure SQL (tenant data)
- **Authentication**: Azure AD OAuth 2.0
- **Monitoring**: Application Insights

### Quick Start (Backend)

```bash
cd backend
npm install
npm start
# API available at http://localhost:7071/api
```

See [backend/README.md](./backend/README.md) for detailed setup instructions.

### 📚 Complete SaaS Documentation

The [`docs/saas/`](./docs/saas/) directory contains comprehensive documentation:

- **[Architecture](./docs/saas/architecture.md)**: System design, components, and data flow
- **[Data Model](./docs/saas/data-model.md)**: Database schemas and entity relationships
- **[Security](./docs/saas/security.md)**: Authentication, authorization, and compliance
- **[API Specification](./docs/saas/api-spec.md)**: Complete API reference with examples
- **[Onboarding Flow](./docs/saas/onboarding.md)**: Tenant onboarding and trial management
- **[Marketplace Plan](./docs/saas/marketplace-plan.md)**: Microsoft Commercial Marketplace integration

### Deployment

**Backend Infrastructure:**
```bash
az deployment group create \
  --resource-group rg-spexternal \
  --template-file deployment/backend.bicep \
  --parameters environment=dev
```

**CI/CD:** GitHub Actions workflow automatically deploys backend on push to main branch.

See [deployment/README.md](./deployment/README.md) for complete deployment guide.

## Next Steps

### Immediate (MVP)
1. ✅ Complete backend API endpoints (tenants, users, policies, audit)
2. 🔄 Integrate SPFx web part with backend API
3. 🔄 Add subscription status UI components
4. 🔄 Implement end-to-end testing

### Phase 2 (Post-MVP)
1. Microsoft Commercial Marketplace integration
2. Advanced features (approvals, risk flags, review campaigns)
3. Multi-region deployment for global scale
4. Enhanced analytics and reporting dashboards

## Technology Stack

### Frontend (SPFx)
- **Framework**: SharePoint Framework (SPFx) 1.18.2
- **UI Library**: Fluent UI 8.x
- **Language**: TypeScript 4.5.5
- **Styling**: SCSS Modules
- **Build**: Gulp-based SPFx build pipeline

### Backend (SaaS API)
- **Runtime**: Node.js 18 LTS
- **Framework**: Azure Functions v4
- **Language**: TypeScript 5.3
- **Database**: Azure Cosmos DB + Azure SQL
- **Authentication**: Azure AD (multi-tenant)
- **Infrastructure**: Azure (Functions, Cosmos DB, Key Vault, App Insights)

## License

MIT License - see LICENSE file for details.

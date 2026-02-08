# SharePoint External User Manager - SaaS Platform

A modern, multi-tenant SaaS solution for managing external users, client spaces, and document access in SharePoint Online. Built with a clean separation between client-side SPFx web parts and a cloud-hosted backend API with Blazor administrative portal.

## 🏗️ Architecture

This solution follows a split architecture pattern:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Customer's SharePoint                     │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  SPFx Client Web Parts (Installed by Customer)             │ │
│  │  - Client Dashboard                                         │ │
│  │  - External User Manager                                    │ │
│  │  - Library & List Management                                │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                            ↕ HTTPS API Calls
┌─────────────────────────────────────────────────────────────────┐
│                    SaaS Platform (Azure - Hosted by Us)         │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Blazor Portal (Marketing + Admin Dashboard)               │ │
│  │  - Pricing & Sign-up                                        │ │
│  │  - Onboarding Wizard                                        │ │
│  │  - Subscription Management                                  │ │
│  │  - Tenant Configuration                                     │ │
│  └────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Backend API (Multi-tenant ASP.NET Core)                   │ │
│  │  - Tenant Management                                        │ │
│  │  - Client Space Provisioning                                │ │
│  │  - External User Operations (via Graph API)                │ │
│  │  - Stripe Billing Integration                               │ │
│  │  - Audit Logging                                            │ │
│  └────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Azure Infrastructure                                       │ │
│  │  - Azure SQL Database (multi-tenant)                        │ │
│  │  - Key Vault (secrets management)                           │ │
│  │  - Application Insights (monitoring)                        │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## 📁 Repository Structure

```
/
├── src/
│   ├── client-spfx/          # SharePoint Framework web parts (customer-installed)
│   │   ├── webparts/         # SPFx web part components
│   │   ├── config/           # SPFx configuration
│   │   └── package.json      # Node.js dependencies
│   │
│   ├── portal-blazor/        # Blazor Web App (SaaS admin portal)
│   │   └── README.md         # [Planned - ISSUE-08]
│   │
│   ├── api-dotnet/           # ASP.NET Core Web API (multi-tenant backend)
│   │   ├── src/              # API source code (currently Node.js/Azure Functions)
│   │   ├── database/         # SQL migrations and seeds
│   │   └── package.json      # Dependencies
│   │
│   └── shared/               # Shared models, DTOs, and contracts
│       └── README.md         # [Planned]
│
├── infra/
│   └── bicep/                # Azure infrastructure as code (Bicep templates)
│       └── main.bicep        # Main infrastructure template
│
├── docs/                     # Documentation
│   └── saas/                 # SaaS architecture and API documentation
│
├── .github/
│   └── workflows/            # CI/CD pipelines
│
└── README.md                 # This file
```

## 🚀 Getting Started

### Prerequisites

- **Node.js**: Version 16.x or 18.x (for SPFx client)
- **.NET 8 SDK**: For future Blazor portal and API refactor
- **Azure Subscription**: For deployment
- **Microsoft 365 Tenant**: For testing SPFx web parts
- **Stripe Account**: For billing integration

### Quick Start - SPFx Client

```bash
# Navigate to SPFx client directory
cd src/client-spfx

# Install dependencies
npm install

# Start development server
npm run serve

# Build for production
npm run build

# Create deployment package
npm run package-solution
# Package will be in: src/client-spfx/sharepoint/solution/*.sppkg
```

### Quick Start - Backend API

```bash
# Navigate to API directory
cd src/api-dotnet

# Install dependencies
npm install

# Start local development (Azure Functions)
npm start
# API available at http://localhost:7071/api
```

### Quick Start - Blazor Portal

```bash
# [Coming in ISSUE-08]
cd src/portal-blazor
dotnet restore
dotnet run
```

## 📦 Build Commands

### Build Everything

From the repository root:

```bash
# Build SPFx Client
cd src/client-spfx && npm install && npm run build && cd ../..

# Build Backend API
cd src/api-dotnet && npm install && npm run build && cd ../..

# Build Blazor Portal (when implemented)
# cd src/portal-blazor && dotnet build && cd ../..
```

### Run Tests

```bash
# SPFx Client
cd src/client-spfx && npm test

# Backend API
cd src/api-dotnet && npm test

# Blazor Portal (when implemented)
# cd src/portal-blazor && dotnet test
```

## 🎯 Key Features

### SPFx Client (Customer-Installed)
- ✅ **Client Dashboard**: Firm-level view of all client spaces
- ✅ **External User Management**: Invite, remove, and track external users
- ✅ **Library Management**: Create and manage document libraries
- ✅ **List Management**: Create and manage SharePoint lists
- ✅ **Metadata Tracking**: Company and project associations for external users
- ✅ **Responsive Design**: Works on desktop and mobile

### Backend API (SaaS Platform)
- ✅ **Multi-tenant Architecture**: Complete tenant isolation
- ✅ **Microsoft Graph Integration**: SharePoint site and user operations
- ✅ **Stripe Billing**: Subscription management and webhooks
- ✅ **Audit Logging**: Comprehensive activity tracking
- ✅ **Rate Limiting**: Per-tenant throttling and quotas
- ✅ **Authentication**: Azure AD multi-tenant with JWT validation

### Blazor Portal (Coming Soon)
- 🔄 **Pricing Page**: Display subscription tiers
- 🔄 **Onboarding Wizard**: Streamlined tenant setup
- 🔄 **Admin Dashboard**: Manage clients and subscriptions
- 🔄 **Billing Integration**: Stripe checkout and subscription management

## 🔒 Security

- **Tenant Isolation**: Every database table includes `TenantId` for complete data separation
- **Authentication**: Azure AD OAuth 2.0 with JWT token validation
- **Authorization**: Role-based access control (RBAC) with Admin/User roles
- **Secrets Management**: Azure Key Vault for production secrets (never commit secrets to repo)
- **Audit Trail**: All administrative actions are logged
- **Rate Limiting**: Per-tenant throttling to prevent abuse
- **Quality Gates**: Automated secret scanning and dependency vulnerability checks

**Security Best Practices**: See [`docs/SECURITY_NOTES.md`](./docs/SECURITY_NOTES.md) for detailed security guidelines.

## 📚 Documentation

### Getting Started
- **[README](./README.md)**: Quick start and overview (this file)
- **[Architecture Overview](./ARCHITECTURE.md)**: Detailed system architecture
- **[Developer Guide](./DEVELOPER_GUIDE.md)**: Development setup and guidelines

### Deployment
- **[Deployment Guide](./docs/DEPLOYMENT.md)**: Complete deployment instructions
- **[Infrastructure Guide](./infra/bicep/README.md)**: Azure Bicep templates and setup
- **[ISSUE-10 Quick Reference](./ISSUE_10_QUICK_REFERENCE.md)**: Deployment commands
- **[Release Checklist](./docs/RELEASE_CHECKLIST.md)**: Pre-release verification steps

### Quality & Security
- **[Branch Protection](./docs/BRANCH_PROTECTION.md)**: GitHub branch protection configuration
- **[Security Notes](./docs/SECURITY_NOTES.md)**: Security best practices and requirements
- **[Workflows README](./.github/workflows/README.md)**: CI/CD pipeline documentation

### User Guides
- **[Solicitor Guide](./SOLICITOR_GUIDE.md)**: Non-technical user guide
- **[Technical Documentation](./TECHNICAL_DOCUMENTATION.md)**: API specifications

### SaaS Platform Documentation
- **[SaaS Architecture](./docs/saas/)**: Complete SaaS architecture docs
  - [Architecture](./docs/saas/architecture.md)
  - [Data Model](./docs/saas/data-model.md)
  - [Security](./docs/saas/security.md)
  - [API Specification](./docs/saas/api-spec.md)

## 🛠️ Technology Stack

### Frontend
- **SharePoint Framework (SPFx)**: 1.18.2
- **React**: 17.0.1
- **Fluent UI**: 8.x
- **TypeScript**: 4.5.5

### Backend (Current)
- **Azure Functions**: v4 (Node.js 18)
- **TypeScript**: 5.3
- **Azure Cosmos DB**: Metadata storage
- **Azure SQL**: Tenant data

### Backend (Target - ISSUE-02)
- **ASP.NET Core**: .NET 8
- **Entity Framework Core**: 8.x
- **Azure SQL**: Multi-tenant database

### Portal (Target - ISSUE-08)
- **Blazor**: .NET 8
- **Bootstrap**: 5.x
- **Microsoft Identity**: Entra ID integration

### Infrastructure
- **Azure App Service**: API and portal hosting
- **Azure SQL Database**: Data storage
- **Azure Key Vault**: Secrets management
- **Application Insights**: Monitoring and diagnostics
- **Bicep**: Infrastructure as Code

## 🚢 Deployment

### Quick Deploy to Azure

Deploy the complete SaaS platform to Azure with one command:

```bash
./deploy-dev.sh
```

This script will:
1. Create Azure resource group
2. Deploy infrastructure (Bicep)
3. Build and deploy API
4. Build and deploy Blazor Portal
5. Build SPFx package

For detailed deployment instructions, see:
- **[Complete Deployment Guide](./docs/DEPLOYMENT.md)** - Step-by-step instructions
- **[Infrastructure Guide](./infra/bicep/README.md)** - Bicep templates and Azure setup
- **[Quick Reference](./ISSUE_10_QUICK_REFERENCE.md)** - Commands and configuration

### CI/CD Pipelines

GitHub Actions workflows automatically:
- ✅ **Quality gates on PRs** - All builds, tests, and security checks must pass
- Build and test on pull requests
- Deploy to dev environment on merge to `develop`
- Deploy to production on merge to `main`

**Quality Gate Workflows** (Required for Merge):
- `ci-quality-gates.yml` - **Comprehensive CI checks** (SPFx, API, Portal, Security)
  - Blocks merge if builds fail
  - Blocks merge if tests fail
  - Runs secret scanning and dependency checks

**Build Workflows:**
- `build-api.yml` - Builds ASP.NET Core API
- `build-blazor.yml` - Builds Blazor Portal
- `test-build.yml` - Builds SPFx Client

**Deployment Workflows:**
- `deploy-dev.yml` - Deploys to dev environment
- `deploy-backend.yml` - Deploys Azure Functions
- `deploy-spfx.yml` - Deploys SPFx to SharePoint

See [`.github/workflows/README.md`](./.github/workflows/README.md) for details.

**Branch Protection:**
- Main branch is protected with required status checks
- All CI quality gates must pass before merge
- At least 1 code review approval required
- See [`docs/BRANCH_PROTECTION.md`](./docs/BRANCH_PROTECTION.md) for configuration

### Manual Deployment

```bash
# Deploy infrastructure
az deployment group create \
  --resource-group rg-spexternal-dev \
  --template-file infra/bicep/main.bicep \
  --parameters environment=dev

# Deploy SPFx package
# Upload src/client-spfx/sharepoint/solution/*.sppkg to SharePoint App Catalog

# Deploy backend API
# GitHub Actions handles this automatically
```

See detailed deployment guide: [`deployment/README.md`](./deployment/README.md)

## 📋 Roadmap

### Phase 1: Foundation (Current)
- ✅ **ISSUE-01**: Repository restructure
- ✅ **ISSUE-02**: ASP.NET Core API skeleton
- ✅ **ISSUE-03**: Azure SQL + EF Core migrations
- ✅ **ISSUE-04**: Client space provisioning
- ✅ **ISSUE-05**: External user management backend
- ✅ **ISSUE-06**: Library & list management backend
- ✅ **ISSUE-07**: Stripe billing integration

### Phase 2: Portal & Integration
- ✅ **ISSUE-08**: Blazor SaaS portal
- ✅ **ISSUE-09**: SPFx client refactor (thin SaaS client)
- ✅ **ISSUE-10**: Azure deployment (Bicep + CI/CD)
- ✅ **ISSUE-11**: Quality gates & merge protection ← **JUST COMPLETED**

### Phase 3: Advanced Features (Post-MVP)
- Microsoft Commercial Marketplace integration
- Advanced governance and compliance features
- Multi-region deployment
- Enhanced analytics and reporting

## 🤝 Contributing

This is a private project under active development. Please follow these guidelines:

1. Work on issues in order (ISSUE-01 → ISSUE-11)
2. Each issue has clear acceptance criteria
3. All code must pass CI checks before merge
4. No secrets in repository
5. Use UK English for all UI text
6. Keep language solicitor-friendly (Client, Space, Access)

## 📝 License

MIT License - see [LICENSE](./LICENSE) file for details.

## 🆘 Support

For development questions, see:
- [Developer Guide](./DEVELOPER_GUIDE.md)
- [Technical Documentation](./TECHNICAL_DOCUMENTATION.md)
- [Backend README](./src/api-dotnet/README.md)
- [SPFx Client README](./src/client-spfx/README.md) (to be created)

---

**Built with ❤️ for legal professionals managing client document access**

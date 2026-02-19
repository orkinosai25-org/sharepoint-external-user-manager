# CI/CD and Deployment Guide - ISSUE F

## Overview

This document describes the Continuous Integration and Continuous Deployment (CI/CD) setup for the SharePoint External User Manager SaaS platform.

**Date**: February 19, 2026  
**Status**: ✅ Complete  
**Related Issue**: ISSUE F — CI/CD and Deployment

## Architecture

### CI/CD Pipeline Structure

```
┌─────────────────────────────────────────────────────────────┐
│                     Code Changes                            │
│         (Push to develop/main or Pull Request)             │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                  Build & Test Phase                         │
│  ┌────────────┐  ┌────────────┐  ┌────────────────────┐   │
│  │   API      │  │  Portal    │  │   SPFx Client      │   │
│  │  Build     │  │  Build     │  │   Build & Package  │   │
│  └────────────┘  └────────────┘  └────────────────────┘   │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                 Quality Gates (PRs only)                    │
│  • Linting          • Tests          • Security Scans      │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                   Deployment Phase                          │
│  ┌─────────────────┐         ┌──────────────────────┐      │
│  │   Development   │         │     Production       │      │
│  │    (develop)    │         │       (main)         │      │
│  │                 │         │  (requires approval) │      │
│  └─────────────────┘         └──────────────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

## Workflows

### 1. Build Workflows (No Deployment)

These workflows validate that code builds successfully but do not deploy.

#### `build-api.yml`
- **Triggers**: Push to main/develop, PRs to main, Manual
- **Purpose**: Build and validate .NET API
- **Actions**:
  - Restore NuGet packages
  - Build in Release configuration
  - Run tests (if available)
  - Publish build artifacts
- **Artifacts**: `api-build` (7 days retention)

#### `build-blazor.yml`
- **Triggers**: Push to main/develop, PRs to main, Manual
- **Purpose**: Build and validate Blazor Portal
- **Actions**:
  - Restore NuGet packages
  - Build in Release configuration
  - Run tests (if available)
  - Publish build artifacts
- **Artifacts**: `portal-build` (7 days retention)

#### `test-build.yml`
- **Triggers**: PRs to main, Manual
- **Purpose**: Test SPFx solution builds correctly
- **Actions**:
  - Setup Node.js 18.19.0
  - Install npm dependencies with `--legacy-peer-deps`
  - Run linting (if available)
  - Build SPFx solution
  - Package as `.sppkg`
  - Verify package creation
- **Artifacts**: `test-spfx-package` (7 days retention)

### 2. Quality Gates Workflow

#### `ci-quality-gates.yml`
- **Triggers**: PRs to main, Push to main, Manual
- **Purpose**: Enforce quality standards before merge
- **Jobs**:
  1. **SPFx Build & Lint**: Validates SPFx client code
  2. **Azure Functions Build & Test**: Validates Node.js Functions (if present)
  3. **.NET API Build & Test**: Validates .NET Web API
  4. **Blazor Portal Build & Test**: Validates Blazor Portal
  5. **Security Scan**: Checks for secrets and vulnerabilities
  6. **Quality Summary**: Aggregates results
- **Gates**: All jobs must pass for PR to be mergeable
- **Features**:
  - Parallel execution for speed
  - Cancels in-progress runs on new commits
  - Secret scanning with TruffleHog
  - Dependency vulnerability scanning (npm audit, dotnet list package --vulnerable)

### 3. Deployment Workflows

#### `deploy-dev.yml` (Development Environment)
- **Triggers**: Push to `develop` branch, Manual
- **Purpose**: Deploy to development environment
- **Method**: Publish Profile authentication
- **Jobs**:
  1. **Build**: Compile API, Portal, and SPFx
  2. **Deploy Infrastructure** (manual only): Deploy Bicep templates
  3. **Deploy API**: Deploy to development App Service
  4. **Deploy Portal**: Deploy to development App Service
  5. **Health Check**: Verify deployment status
- **Required Secrets**:
  - `API_PUBLISH_PROFILE`: Development API App Service publish profile
  - `PORTAL_PUBLISH_PROFILE`: Development Portal App Service publish profile
- **Optional Secrets** (infrastructure only):
  - `AZURE_CREDENTIALS`: Service principal for Bicep deployment
  - `SQL_ADMIN_USERNAME`, `SQL_ADMIN_PASSWORD`: SQL Server credentials

#### `deploy-prod.yml` (Production Environment)
- **Triggers**: Push to `main` branch, Manual
- **Purpose**: Deploy to production environment
- **Method**: Publish Profile authentication
- **Environment Protection**: Requires approval for deployment jobs
- **Jobs**:
  1. **Build**: Compile API, Portal, and SPFx
  2. **Deploy Infrastructure** (manual only): Deploy Bicep templates
  3. **Deploy API**: Deploy to production App Service (requires approval)
  4. **Deploy Portal**: Deploy to production App Service (requires approval)
  5. **Deployment Summary**: Generate post-deployment checklist
- **Required Secrets**:
  - `API_PUBLISH_PROFILE_PROD`: Production API App Service publish profile
  - `PORTAL_PUBLISH_PROFILE_PROD`: Production Portal App Service publish profile

#### `main_clientspace.yml` (ClientSpace - Special Production App)
- **Triggers**: Push to `main` branch (Portal changes only), Manual
- **Purpose**: Deploy Portal to specific "ClientSpace" App Service
- **Method**: Publish Profile authentication
- **Jobs**:
  1. **Build**: Compile Blazor Portal (Windows runner)
  2. **Deploy**: Deploy to ClientSpace App Service (Windows runner)
- **Required Secrets**:
  - `PUBLISH_PROFILE`: ClientSpace App Service publish profile
- **Note**: Uses Windows runners for compatibility

#### `deploy-spfx.yml` (SharePoint App Catalog)
- **Triggers**: Push to `main` branch, Manual
- **Purpose**: Build and deploy SPFx solution to SharePoint
- **Method**: PnP PowerShell with Azure AD App authentication
- **Jobs**:
  1. **Build**: Package SPFx solution as `.sppkg`
  2. **Deploy**: Upload to SharePoint App Catalog and publish
- **Required Secrets**:
  - `SPO_URL`: SharePoint tenant URL
  - `SPO_CLIENT_ID`: Azure AD App Client ID
  - `SPO_CLIENT_SECRET`: Azure AD App Client Secret
  - `SPO_TENANT_ID` (optional): Azure AD Tenant ID
- **Environment Protection**: Requires approval (production environment)
- **Features**:
  - Automatic retry on connection failures
  - Detailed deployment summary
  - Error diagnostics and troubleshooting guidance

#### `deploy-backend.yml` (Azure Functions - Legacy)
- **Triggers**: Push to main/develop (API changes), Manual
- **Purpose**: Deploy Azure Functions backend
- **Method**: Azure Functions action with Service Principal
- **Status**: ⚠️ Optional - API deployment now handled by deploy-dev/prod
- **Required Secrets**:
  - `AZURE_CREDENTIALS`: Service principal credentials
- **Note**: Can be deprecated in favor of deploy-dev/prod workflows

## Deployment Method: Publish Profiles

### Why Publish Profiles?

The project uses **publish profiles** for deploying .NET applications to Azure App Services:

✅ **Advantages**:
- Simpler setup (no Azure AD app registration needed)
- More reliable (direct App Service authentication)
- Easier troubleshooting (clear error messages)
- No Azure CLI required
- Scoped permissions (each profile only accesses one App Service)
- Works identically in all environments

❌ **Disadvantages of Service Principals** (not used):
- Complex setup (Azure AD app, role assignments, JSON credentials)
- More failure points (token expiration, permission issues)
- Harder to debug (generic Azure login errors)
- Requires Azure CLI in workflows
- Broader permissions scope

### How Publish Profiles Work

1. **Obtain from Azure Portal**:
   - Navigate to App Service → Overview
   - Click "Get publish profile"
   - Download `.PublishSettings` XML file

2. **Add to GitHub Secrets**:
   - Copy entire XML content
   - Add as repository secret
   - Name according to environment (e.g., `API_PUBLISH_PROFILE`)

3. **Used in Workflow**:
   ```yaml
   - name: Deploy to Azure App Service
     uses: azure/webapps-deploy@v3
     with:
       app-name: 'my-app-service'
       package: ./app-package
       publish-profile: ${{ secrets.PUBLISH_PROFILE }}
   ```

## Secret Management

### Secret Naming Convention

| Environment | API Secret | Portal Secret |
|-------------|-----------|---------------|
| Development | `API_PUBLISH_PROFILE` | `PORTAL_PUBLISH_PROFILE` |
| Production | `API_PUBLISH_PROFILE_PROD` | `PORTAL_PUBLISH_PROFILE_PROD` |
| ClientSpace | N/A | `PUBLISH_PROFILE` |

### Secret Rotation

Publish profiles should be rotated:
- Every 90 days (recommended)
- When a team member with access leaves
- After any security incident

**How to Rotate**:
1. Download new publish profile from Azure Portal
2. Update GitHub secret with new XML content
3. Test deployment
4. Old profile becomes invalid automatically

## Environment Configuration

### Development Environment
- **Branch**: `develop`
- **Auto-Deploy**: Yes
- **Approval Required**: No
- **App Services**:
  - API: `spexternal-api-dev`
  - Portal: `spexternal-portal-dev`
- **Resource Group**: `spexternal-dev-rg`

### Production Environment
- **Branch**: `main`
- **Auto-Deploy**: Yes
- **Approval Required**: Yes (configured in GitHub Environment settings)
- **App Services**:
  - API: `spexternal-api-prod`
  - Portal: `spexternal-portal-prod`
  - ClientSpace: `ClientSpace`
- **Resource Group**: `spexternal-prod-rg`

## Branch Protection

### Recommended Settings

For the `main` branch:

1. **Require pull request reviews before merging**
   - Required approvals: 1
   - Dismiss stale reviews: Yes

2. **Require status checks to pass**
   - Required checks:
     - `SPFx Client - Build & Lint`
     - `.NET API - Build & Test`
     - `Blazor Portal - Build & Test`
   - Require branches to be up to date: Yes

3. **Require conversation resolution before merging**: Yes

4. **Do not allow bypassing the above settings**: Yes

5. **Restrict who can push to matching branches**: Admin only

For the `develop` branch:
- Same as `main` but allow bypassing for admins

## Monitoring and Troubleshooting

### Build Failures

**Check**:
1. Workflow run logs in Actions tab
2. Build output for compilation errors
3. Dependency resolution issues (NuGet/npm)

**Common Issues**:
- .NET version mismatch (should be 8.0.x)
- Node.js version mismatch (should be 18.19.0)
- Missing `package-lock.json` or corrupt cache
- NuGet package vulnerabilities (warnings only)

### Deployment Failures

**Check**:
1. Deployment job logs
2. Secret configuration (is `*_PUBLISH_PROFILE` set?)
3. Azure App Service status (is it running?)
4. Azure App Service logs (Kudu console)

**Common Issues**:
- Publish profile expired or invalid
- App Service stopped or in failed state
- App Service plan scale limits reached
- Application startup errors (check App Service logs)

### SPFx Deployment Failures

**Check**:
1. SharePoint connection (is `SPO_URL` correct?)
2. Azure AD App permissions (does it have `Sites.FullControl.All`?)
3. Client secret expiration
4. PnP PowerShell errors in logs

## Deployment Checklist

### Before First Deployment

- [ ] Create Azure App Services (API, Portal)
- [ ] Download publish profiles from each App Service
- [ ] Add publish profiles as GitHub secrets
- [ ] Configure Azure AD App for SPFx (if deploying SPFx)
- [ ] Set up GitHub Environments (production with approvers)
- [ ] Configure branch protection rules
- [ ] Test deployment to development first

### Before Each Production Release

- [ ] All tests passing in development
- [ ] Code reviewed and approved
- [ ] Release notes prepared
- [ ] Stakeholders notified of deployment window
- [ ] Rollback plan prepared
- [ ] Monitor scheduled (check logs during deployment)

### After Each Production Deployment

- [ ] Verify API is responding
- [ ] Verify Portal is accessible
- [ ] Run smoke tests
- [ ] Check application logs for errors
- [ ] Verify database migrations (if any)
- [ ] Notify stakeholders of completion
- [ ] Update release documentation

## Workflow Execution Summary

| Workflow | Trigger | Duration (est.) | Artifacts | Deploys |
|----------|---------|-----------------|-----------|---------|
| build-api.yml | Push, PR | 2-3 min | Yes | No |
| build-blazor.yml | Push, PR | 2-3 min | Yes | No |
| test-build.yml | PR | 3-5 min | Yes | No |
| ci-quality-gates.yml | PR, Push | 5-8 min | No | No |
| deploy-dev.yml | Push to develop | 5-10 min | Yes | Yes |
| deploy-prod.yml | Push to main | 5-10 min | Yes | Yes (with approval) |
| main_clientspace.yml | Push to main | 3-5 min | Yes | Yes |
| deploy-spfx.yml | Push to main | 5-8 min | Yes | Yes (with approval) |

## Success Criteria (Done When)

✅ **CI builds green** - All build workflows pass successfully  
✅ **Quality gates enforced** - ci-quality-gates.yml enforces standards on PRs  
✅ **Deployment workflows functional** - deploy-dev and deploy-prod work correctly  
✅ **Secrets documented** - WORKFLOW_SECRET_SETUP.md explains all required secrets  
✅ **Branch protection recommended** - Documented in this guide  

## Additional Resources

- [GitHub Actions Secret Setup Guide](./WORKFLOW_SECRET_SETUP.md)
- [Azure App Service Deployment Documentation](https://docs.microsoft.com/azure/app-service/deploy-github-actions)
- [.github/workflows/README.md](./.github/workflows/README.md)
- [Branch Protection Documentation](./docs/BRANCH_PROTECTION.md)

## Related Issues

- ISSUE A — Backend API (provides API to deploy)
- ISSUE B — SaaS Portal MVP UI (provides Portal to deploy)
- ISSUE C — Azure AD & OAuth (authentication for deployments)
- ISSUE F — **THIS ISSUE** - CI/CD and Deployment

## Security Summary

See [ISSUE_F_SECURITY_SUMMARY.md](./ISSUE_F_SECURITY_SUMMARY.md) for security considerations related to CI/CD implementation.

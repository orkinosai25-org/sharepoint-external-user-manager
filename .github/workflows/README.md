# GitHub Actions Workflows

This directory contains GitHub Actions workflows for the SharePoint External User Manager project.

## Overview

The repository uses GitHub Actions for continuous integration (CI) and continuous deployment (CD) to ensure code quality and automate deployments.

### Workflow Categories

1. **Quality Gates** - Enforce code quality and security standards (blocks PRs if failing)
2. **Build Workflows** - Build and test individual components
3. **Deployment Workflows** - Deploy to Azure environments

---

## 🛡️ CI Quality Gates (`ci-quality-gates.yml`)

**Purpose**: Comprehensive quality checks that run on every pull request to `main` branch. All checks must pass before merging is allowed.

### When It Runs
- On pull requests to `main` branch
- On pushes to `main` branch (verification)
- Manual trigger via workflow dispatch

### Jobs

#### 1. SPFx Client - Build & Lint
- Installs dependencies with `npm ci`
- Runs ESLint for code quality
- Builds SPFx solution
- Packages solution (`.sppkg`)
- Verifies package creation

#### 2. Azure Functions API - Build, Lint & Test
- Installs TypeScript dependencies
- Runs ESLint on Azure Functions code
- Builds TypeScript to JavaScript
- Runs Jest tests

#### 3. .NET API - Build & Test
- Restores NuGet packages
- Builds .NET 8 Web API
- Runs unit tests (if available)

#### 4. Blazor Portal - Build & Test
- Restores NuGet packages
- Builds Blazor Web App
- Runs unit tests (if available)

#### 5. Security Scan
- **Secret scanning**: Uses TruffleHog to detect committed secrets
- **Dependency vulnerabilities**: Scans npm and NuGet packages for known vulnerabilities
- Reports findings but doesn't block merge (advisory)

#### 6. Quality Summary
- Aggregates results from all jobs
- Generates summary in GitHub Actions UI
- Fails if any required check fails
- Blocks merge if quality gates don't pass

### Required for Merge

Configure these status checks as required in branch protection:
- `SPFx Client - Build & Lint`
- `Azure Functions API - Build, Lint & Test`
- `.NET API - Build & Test`
- `Blazor Portal - Build & Test`
- `Quality Gates Summary`

See [Branch Protection Rules](../../docs/BRANCH_PROTECTION.md) for configuration instructions.

### Viewing Results

Results appear in:
- Pull request checks section
- Actions tab → CI Quality Gates workflow
- Job summary page with detailed status table

---

## Deploy SPFx Solution (`deploy-spfx.yml`)

This workflow automatically builds and deploys the SharePoint Framework (SPFx) solution to your SharePoint tenant's App Catalog whenever code is pushed to the `main` branch.

### Workflow Overview

The deployment process consists of two main jobs:

1. **Build Job**:
   - Sets up Node.js 18.x environment
   - Installs npm dependencies
   - Builds the SPFx solution
   - Packages the solution into a `.sppkg` file
   - Uploads the package as a build artifact

2. **Deploy Job**:
   - Downloads the build artifact
   - Installs PnP PowerShell module
   - Connects to SharePoint Online
   - Uploads the `.sppkg` file to the App Catalog
   - Publishes the solution
   - Provides deployment summary

### Required Repository Secrets

Before using this workflow, you must configure the following secrets in your GitHub repository:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `SPO_URL` | SharePoint tenant URL | `https://turqoisecms-admin.sharepoint.com` |
| `SPO_USERNAME` | SharePoint admin username | `admin@turqoisecms-admin.onmicrosoft.com` |
| `SPO_PASSWORD` | SharePoint admin password | `YourSecurePassword123!` |

### Setting Up Repository Secrets

1. Navigate to your GitHub repository
2. Go to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each of the required secrets listed above

### Authentication Requirements

The deployment account must have:
- SharePoint Administrator role
- Access to the tenant App Catalog
- Appropriate permissions to upload and publish apps

**Note**: If your organization uses Multi-Factor Authentication (MFA), you may need to:
- Use an app password instead of the regular password
- Create a dedicated service account without MFA
- Use certificate-based authentication (requires workflow modification)

### Triggering the Workflow

The workflow can be triggered in two ways:

1. **Automatic**: Push changes to the `main` branch
2. **Manual**: Use the "Run workflow" button in the GitHub Actions tab

### Environment Protection

The workflow uses a `production` environment for the deploy job, which can be configured to:
- Require manual approval before deployment
- Restrict which branches can deploy
- Add deployment protection rules

To configure environment protection:
1. Go to **Settings** → **Environments**
2. Click on the `production` environment
3. Configure protection rules as needed

### Monitoring Deployments

Each deployment provides:
- **Build artifacts**: The `.sppkg` file is stored for 30 days
- **Deployment summary**: Success/failure status with details
- **Logs**: Detailed execution logs for troubleshooting

### Troubleshooting

#### Common Issues

1. **Node.js Version Mismatch**
   - The workflow uses Node.js 18.x as required by SPFx 1.18.2
   - Local development should use the same version

2. **Authentication Failures**
   - Verify the SPO_USERNAME and SPO_PASSWORD secrets
   - Check if the account has proper permissions
   - Consider using app passwords for MFA-enabled accounts

3. **Package Upload Failures**
   - Ensure the App Catalog is accessible
   - Check if the package name conflicts with existing apps
   - Verify sufficient storage space in the App Catalog

4. **Build Failures**
   - Check for TypeScript compilation errors
   - Verify all dependencies are properly installed
   - Review ESLint warnings that might block the build

#### Viewing Logs

To view detailed logs:
1. Go to the **Actions** tab in your repository
2. Click on the failed workflow run
3. Expand the failed step to see detailed error messages

#### Common Troubleshooting Steps

1. **Test Connection Manually**:
   ```powershell
   # Test PowerShell connection locally
   Install-Module -Name PnP.PowerShell -Force
   Connect-PnPOnline -Url "https://yourtenant.sharepoint.com" -Interactive
   Get-PnPApp
   ```

2. **Verify App Catalog Access**:
   - Ensure the service account can access `https://yourtenant.sharepoint.com/sites/appcatalog`
   - Check that the App Catalog is properly configured
   - Verify sufficient storage space in the App Catalog

3. **Check Package File**:
   - Verify the `.sppkg` file is created correctly in the build step
   - Check the package size (should be > 0 bytes)
   - Ensure the package isn't corrupted

4. **Authentication Debugging**:
   ```powershell
   # Check account status
   Get-PnPTenantServicePrincipal
   Get-PnPUser -Identity "admin@tenant.onmicrosoft.com"
   ```

### Security Considerations

- **Secrets Management**: Never commit credentials to the repository
- **Least Privilege**: Use accounts with minimum required permissions
- **Regular Rotation**: Periodically update passwords and secrets
- **Audit Trail**: Monitor deployment logs for security events

### Customization

You can customize the workflow by:
- Modifying the trigger conditions (branches, paths)
- Adding additional build steps (testing, linting)
- Changing the deployment environment
- Adding notifications (Slack, Teams, email)
- Including additional validation steps

### Related Documentation

- [SharePoint Framework Development](../../DEVELOPER_GUIDE.md)
- [Deployment Instructions](../../deployment-instructions.md)
- [PnP PowerShell Documentation](https://pnp.github.io/powershell/)

---

## Other Build Workflows

### Build API (`build-api.yml`)

Builds and tests the .NET 8 Web API.

**Triggers**:
- Push to `main` or `develop` (when API files change)
- Pull requests to `main` (when API files change)
- Manual dispatch

**Steps**:
1. Setup .NET 8
2. Restore dependencies
3. Build in Release configuration
4. Run tests (if available)
5. Publish build artifacts

### Build Blazor Portal (`build-blazor.yml`)

Builds and tests the Blazor Web App portal.

**Triggers**:
- Push to `main` or `develop` (when portal files change)
- Pull requests to `main` (when portal files change)
- Manual dispatch

**Steps**:
1. Setup .NET 8
2. Restore dependencies
3. Build in Release configuration
4. Run tests (if available)
5. Publish build artifacts

### Test Build SPFx (`test-build.yml`)

Tests SPFx solution builds without deploying.

**Triggers**:
- Pull requests to `main`
- Manual dispatch

**Steps**:
1. Setup Node.js 18.19.0
2. Install dependencies
3. Run linting (if available)
4. Build solution
5. Package solution
6. Verify package created
7. Upload artifacts

---

## Deployment Workflows

### Deploy Development (`deploy-dev.yml`)

Deploys the complete platform to development environment.

**Triggers**:
- Push to `develop` branch
- Manual dispatch

**Jobs**:
1. Deploy infrastructure (Bicep)
2. Deploy .NET API
3. Deploy Blazor Portal
4. Deploy Azure Functions
5. Build SPFx package

### Deploy Backend (`deploy-backend.yml`)

Deploys Azure Functions backend to production.

**Triggers**:
- Push to `main` branch
- Manual dispatch

**Environment**: Production (with manual approval)

---

## Workflow Dependencies

```
Pull Request to main
    ↓
CI Quality Gates (REQUIRED) ← All checks must pass
    ├── SPFx Build & Lint
    ├── Azure Functions Build & Test
    ├── .NET API Build & Test
    ├── Blazor Portal Build & Test
    └── Security Scan
    ↓
Merge Approved
    ↓
Push to main
    ↓
Deploy to Production
```

---

## Troubleshooting Workflows

### CI Quality Gates Failing

**Common issues**:

1. **Build failures**
   - Check Node.js version matches 18.19.0 for SPFx
   - Verify dependencies are up-to-date
   - Look for TypeScript compilation errors

2. **Lint failures**
   - Run `npm run lint` locally to see errors
   - Fix code style issues
   - Update ESLint rules if needed

3. **Test failures**
   - Run tests locally: `npm test`
   - Check for failing unit tests
   - Verify mocks and test data

4. **Security scan warnings**
   - Review TruffleHog findings
   - Check for accidentally committed secrets
   - Update vulnerable dependencies

### Getting Help

1. Check the workflow run logs in GitHub Actions
2. Expand failed steps to see detailed error messages
3. Run commands locally to reproduce issues
4. Review [Developer Guide](../../DEVELOPER_GUIDE.md) for setup instructions
5. Check [Security Notes](../../docs/SECURITY_NOTES.md) for security guidance

---

## Best Practices

### For Pull Requests
- ✅ Run builds and tests locally before pushing
- ✅ Address lint errors before creating PR
- ✅ Keep PRs focused and small
- ✅ Wait for all CI checks to pass before requesting review

### For Secrets Management
- ❌ Never commit secrets to repository
- ✅ Use GitHub Secrets for CI/CD credentials
- ✅ Use Azure Key Vault for production secrets
- ✅ Provide `.example` files for local settings

### For Deployments
- ✅ Always deploy to dev environment first
- ✅ Test thoroughly before production deployment
- ✅ Use manual approval for production releases
- ✅ Monitor Application Insights after deployment

---

## Additional Documentation

- **[Branch Protection Rules](../../docs/BRANCH_PROTECTION.md)** - How to configure required checks
- **[Release Checklist](../../docs/RELEASE_CHECKLIST.md)** - Pre-release verification steps
- **[Security Notes](../../docs/SECURITY_NOTES.md)** - Security best practices
- **[Deployment Guide](../../docs/DEPLOYMENT.md)** - Detailed deployment instructions
- **[GitHub Actions Documentation](https://docs.github.com/en/actions)** - Official GitHub Actions docs
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
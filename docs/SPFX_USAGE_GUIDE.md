# SPFx Web Parts - Optional Usage Guide

## Overview

ClientSpace includes optional SharePoint Framework (SPFx) web parts that customers can install in their SharePoint Online tenants. These web parts provide a rich, integrated experience directly within SharePoint pages.

**Important**: SPFx web parts are **optional**. All core functionality is available through the Blazor portal. SPFx web parts provide an enhanced SharePoint-native experience for users who prefer to work within SharePoint.

## Table of Contents

1. [When to Use SPFx Web Parts](#when-to-use-spfx-web-parts)
2. [Available Web Parts](#available-web-parts)
3. [Installation](#installation)
4. [Configuration](#configuration)
5. [Usage Scenarios](#usage-scenarios)
6. [Comparison: Portal vs SPFx](#comparison-portal-vs-spfx)
7. [Troubleshooting](#troubleshooting)

## When to Use SPFx Web Parts

### Use SPFx Web Parts When:

✅ **Your users work primarily in SharePoint**
- Team spends most of their time in SharePoint sites
- Want integrated experience without switching applications
- Need context-aware functionality within SharePoint pages

✅ **You want embedded collaboration tools**
- Display external user information on SharePoint pages
- Show client dashboards on team sites
- Provide quick access to user management from SharePoint

✅ **You need customization**
- Want to customize the appearance to match your SharePoint theme
- Need to embed functionality in specific pages or locations
- Want to create custom workflows using SPFx

✅ **You have SharePoint site collection admins who manage access**
- Delegate management to site owners
- Empower local teams to manage their own external users
- Reduce dependency on central IT

### Use the Blazor Portal When:

✅ **Centralized administration**
- IT team manages all external users centrally
- Need comprehensive analytics and reporting
- Require bulk operations across multiple clients

✅ **No SharePoint customization required**
- Standard portal interface is sufficient
- Don't want to install packages in SharePoint
- Prefer web-based administration

✅ **Enterprise features required**
- Advanced audit logging
- Tenant-wide policies
- Subscription and billing management

## Available Web Parts

### 1. Client Dashboard Web Part

**Purpose**: Firm-level dashboard showing all client spaces

**Use Cases**:
- Home page for firm's main SharePoint site
- Department landing page
- Practice area overview page

**Features**:
- View all client spaces in a table
- Quick links to open client sites
- External user count per client
- Last modified dates
- Quick actions (Open, Manage)

**Best Placed On**:
- Firm home page
- Department site home page
- Team collaboration hub

**Screenshot Example**:
```
┌─────────────────────────────────────────────────────────────┐
│ Client Dashboard                                    🔄 Refresh│
├─────────────────────────────────────────────────────────────┤
│ Client Name        Site URL           Users  Last Modified  │
│ ABC Corporation    /sites/abc-corp    12     2 days ago     │
│ XYZ Industries     /sites/xyz-ind      8     5 hours ago    │
│ Smith & Partners   /sites/smith-part   5     1 week ago     │
└─────────────────────────────────────────────────────────────┘
```

### 2. External User Manager Web Part

**Purpose**: Manage external users and their library access

**Use Cases**:
- Client site home page
- Matter/project management pages
- Document library landing pages

**Features**:
- List external users with access
- View permission levels (Read/Edit)
- Invite new external users
- Revoke access
- Track company and project metadata
- Filter and search users

**Best Placed On**:
- Client-specific SharePoint sites
- Project workspaces
- Secure collaboration sites

**Screenshot Example**:
```
┌─────────────────────────────────────────────────────────────┐
│ External Users                           [+ Invite User]    │
├─────────────────────────────────────────────────────────────┤
│ Name              Email              Company      Permission │
│ John Doe          john@ex.com        ABC Corp     Edit      │
│ Jane Smith        jane@ex.com        XYZ Inc      Read      │
│ Bob Johnson       bob@ex.com         ABC Corp     Edit      │
└─────────────────────────────────────────────────────────────┘
```

### 3. Library & List Management Web Parts

**Purpose**: Create and manage SharePoint libraries and lists

**Use Cases**:
- Site provisioning pages
- Administrative pages
- Setup wizards

**Features**:
- Create new document libraries
- Create new lists (Tasks, Issues, Contacts)
- Configure library/list settings
- Manage permissions
- Delete libraries/lists

**Best Placed On**:
- Site settings pages
- Administrative landing pages
- Setup/configuration pages

## Installation

### Prerequisites

- SharePoint Online tenant
- App Catalog enabled
- Site Collection Administrator or Tenant Administrator permissions

### Step 1: Obtain the Package

**Option A: Download from Release**
1. Navigate to the GitHub releases page
2. Download the latest `.sppkg` file
3. Save to your local machine

**Option B: Build from Source**
```bash
# Clone repository
git clone https://github.com/orkinosai25-org/sharepoint-external-user-manager.git
cd sharepoint-external-user-manager/src/client-spfx

# Install dependencies
npm install

# Build and package
npm run build
npm run package-solution

# Package location: sharepoint/solution/sharepoint-external-user-manager.sppkg
```

### Step 2: Upload to App Catalog

1. **Navigate to SharePoint Admin Center**
   - URL: `https://[tenant]-admin.sharepoint.com`

2. **Go to Apps → App Catalog**
   - If App Catalog doesn't exist, create one first

3. **Upload Package**
   - Click "Upload"
   - Select the `.sppkg` file
   - Click "OK"

4. **Deploy to Tenant**
   - Check "Make this solution available to all sites in the organization"
   - Click "Deploy"
   - Wait for deployment confirmation (usually 30-60 seconds)

### Step 3: Configure API Permissions (Required)

The web parts need permissions to call the backend API:

1. **Navigate to**: SharePoint Admin Center → Advanced → API access
2. **Approve Pending Requests**:
   - `https://[your-api].azurewebsites.net/access_as_user` - Full control
3. **Click "Approve"** for each request

Without this, web parts will display a permission error.

### Step 4: Add to Sites

**Option A: Site Collection App Catalog** (for specific sites)
1. Navigate to site
2. Site Settings → Site collection features → Add an App
3. Find and add "ClientSpace External User Manager"

**Option B: Tenant-Wide Deployment** (for all sites)
- Already deployed if you checked the box in Step 2
- Available immediately in web part picker

## Configuration

### Web Part Properties

Each web part can be configured via the property pane.

#### Client Dashboard Properties

**API Configuration**:
- **API Base URL**: `https://[your-api].azurewebsites.net/api`
  - Default: Auto-detected from tenant properties
  - Override: Enter custom API URL
- **Tenant ID**: Your tenant identifier
  - Default: Auto-detected from context
  - Override: Enter manually if needed

**Display Settings**:
- **Items Per Page**: Number of clients to show (default: 10)
- **Refresh Interval**: Auto-refresh interval in seconds (default: 300)
- **Show Last Modified**: Display last modified dates (default: true)
- **Compact View**: Use compact table layout (default: false)

**Styling**:
- **Theme**: Light, Dark, or Auto (matches SharePoint theme)
- **Accent Color**: Primary color for buttons and highlights
- **Custom CSS Class**: Additional CSS classes for styling

#### External User Manager Properties

**API Configuration**:
- Same as Client Dashboard

**Display Settings**:
- **Default Library**: Pre-select a library (optional)
- **Show Company Column**: Display company information (default: true)
- **Show Project Column**: Display project information (default: true)
- **Allow Bulk Operations**: Enable bulk invite/revoke (default: true)

**Permission Settings**:
- **Default Permission Level**: Read or Edit (default: Read)
- **Allow Permission Changes**: Let users change permissions (default: true)

### Tenant-Wide Configuration

Set tenant-wide defaults using SharePoint tenant properties:

```powershell
# Connect to SharePoint Online
Connect-SPOService -Url https://[tenant]-admin.sharepoint.com

# Set API URL for all web parts
Set-SPOStorageEntity -Site https://[tenant].sharepoint.com `
  -Key "ClientSpace_ApiUrl" `
  -Value "https://[your-api].azurewebsites.net/api" `
  -Description "ClientSpace API endpoint"

# Set Tenant ID
Set-SPOStorageEntity -Site https://[tenant].sharepoint.com `
  -Key "ClientSpace_TenantId" `
  -Value "your-tenant-id" `
  -Description "ClientSpace Tenant Identifier"
```

## Usage Scenarios

### Scenario 1: Client Portal Site

**Goal**: Create a dedicated portal for managing a specific client

**Setup**:
1. Create a SharePoint site for the client: `/sites/abc-corp`
2. Add "External User Manager" web part to home page
3. Add "Library Management" web part to a "Setup" page
4. Configure web parts with client-specific settings

**Benefits**:
- Site owners can manage external users without portal access
- Context-aware: automatically shows users for this site
- Integrated with SharePoint navigation and branding

### Scenario 2: Firm-Wide Dashboard

**Goal**: Central dashboard showing all clients

**Setup**:
1. Add "Client Dashboard" web part to firm's home page: `/sites/firm`
2. Configure to show all clients
3. Set auto-refresh for real-time updates

**Benefits**:
- Single pane of glass for all client spaces
- Quick navigation to any client site
- Visible to all firm members

### Scenario 3: Department Landing Page

**Goal**: Department-specific view of their clients

**Setup**:
1. Add "Client Dashboard" web part to department site
2. Filter to show only department's clients (via web part properties)
3. Add custom styling to match department branding

**Benefits**:
- Department autonomy
- Reduced noise from other departments' clients
- Department-specific branding and messaging

### Scenario 4: Embedded in Workflows

**Goal**: Integrate user management into existing workflows

**Setup**:
1. Add "External User Manager" web part to workflow pages
2. Use web part as part of client onboarding workflow
3. Combine with other web parts (document libraries, lists)

**Benefits**:
- Streamlined workflows
- Reduced context switching
- Improved user experience

## Comparison: Portal vs SPFx

| Feature | Blazor Portal | SPFx Web Parts |
|---------|---------------|----------------|
| **Installation** | None required (hosted) | Requires package upload |
| **Access** | Web browser | SharePoint pages |
| **Authentication** | Azure AD | SharePoint context |
| **User Management** | ✅ Full featured | ✅ Full featured |
| **Client Dashboard** | ✅ Full featured | ✅ Full featured |
| **Bulk Operations** | ✅ Yes | ✅ Yes (limited) |
| **Analytics** | ✅ Comprehensive | ❌ Basic |
| **Subscription Management** | ✅ Yes | ❌ No |
| **Audit Logs** | ✅ Full history | ❌ No |
| **Customization** | ⚠️ Limited | ✅ Full (via code) |
| **Mobile Experience** | ✅ Responsive | ✅ SharePoint mobile app |
| **Offline Mode** | ❌ No | ⚠️ Limited |
| **API Rate Limits** | Standard | Standard |
| **Updates** | Automatic | Manual (package update) |
| **Cost** | Included in subscription | Included in subscription |

### When to Use Both

Many customers use **both** the portal and SPFx web parts:

**Portal for**:
- IT administration
- Subscription management
- Comprehensive analytics
- Bulk operations
- Audit and compliance

**SPFx Web Parts for**:
- Day-to-day user management by site owners
- Embedded experiences in SharePoint
- Client-specific pages
- Department landing pages

## Troubleshooting

### Web Part Won't Load

**Symptoms**: Blank web part or error message

**Solutions**:
1. **Check API permissions**: Ensure API access is approved in SharePoint Admin Center
2. **Verify API URL**: Check web part properties for correct API URL
3. **Check browser console**: Look for network errors or authentication issues
4. **Clear browser cache**: Try incognito/private mode
5. **Verify package deployment**: Ensure package is deployed tenant-wide or to the site

### "Access Denied" Error

**Symptoms**: Web part shows "You don't have permission to access this resource"

**Solutions**:
1. **Check user permissions**: Ensure user has appropriate SharePoint permissions
2. **Verify API permissions**: Check that API access requests are approved
3. **Check tenant ID**: Verify tenant ID in web part properties matches your subscription
4. **Review audit logs**: Check portal audit logs for permission issues

### Web Part Shows Old Data

**Symptoms**: Data not refreshing or shows stale information

**Solutions**:
1. **Force refresh**: Click refresh button in web part
2. **Clear cache**: Clear browser cache and reload page
3. **Check refresh interval**: Verify auto-refresh is enabled in web part properties
4. **Verify API connectivity**: Test API endpoint directly in browser

### API URL Not Auto-Detected

**Symptoms**: Web part requires manual API URL entry

**Solutions**:
1. **Set tenant property**: Use PowerShell to set ClientSpace_ApiUrl
2. **Manual entry**: Enter API URL in web part properties
3. **Contact support**: If issue persists, contact ClientSpace support

### Package Update Issues

**Symptoms**: New version won't install or deploy

**Solutions**:
1. **Retract old version**: Retract the old package before uploading new one
2. **Clear SharePoint cache**: Wait 15-30 minutes for cache to clear
3. **Upgrade approach**:
   ```bash
   # Remove old version
   # Upload new version
   # Check "Replace existing package"
   # Deploy
   ```

### Performance Issues

**Symptoms**: Web part loads slowly or times out

**Solutions**:
1. **Reduce page size**: Show fewer items per page
2. **Increase refresh interval**: Reduce auto-refresh frequency
3. **Check API performance**: Verify API response times in Azure Portal
4. **Contact support**: May need to upgrade API tier

## Best Practices

### Deployment

✅ **Do**:
- Test in a development site collection first
- Deploy tenant-wide for consistent experience
- Set tenant properties for global configuration
- Document your configuration choices
- Train site owners on web part usage

❌ **Don't**:
- Deploy to production without testing
- Skip API permission approval step
- Hard-code API URLs in multiple places
- Forget to update packages when new versions are available

### Configuration

✅ **Do**:
- Use tenant properties for API URL
- Configure sensible defaults
- Match SharePoint theme
- Enable auto-refresh for dashboards
- Use compact view for embedded scenarios

❌ **Don't**:
- Override defaults unnecessarily
- Disable auto-refresh on dashboards
- Use too-frequent refresh intervals (causes API throttling)
- Mix multiple API URLs in one tenant

### Usage

✅ **Do**:
- Use Client Dashboard for overview pages
- Use External User Manager for client-specific pages
- Combine with native SharePoint web parts
- Train users on available features
- Provide documentation links

❌ **Don't**:
- Add multiple Client Dashboards to same page
- Use web parts on high-traffic public pages
- Forget to configure permissions
- Over-customize (keep it simple)

## Support and Resources

### Documentation

- [Installation Guide](./INSTALLATION_GUIDE.md) - Full installation steps
- [User Guide](./USER_GUIDE.md) - Portal usage guide
- [API Reference](./saas/api-spec.md) - API documentation
- [Developer Guide](../DEVELOPER_GUIDE.md) - For customization

### Getting Help

- **AI Assistant**: Available 24/7 in portal
- **Documentation**: Comprehensive guides
- **Support**: Email support@clientspace.com
- **Community**: Forum and knowledge base

### Custom Development

Need custom web parts or modifications?

- **Enterprise Support**: Contact your account manager
- **Developer Resources**: See [Developer Guide](../DEVELOPER_GUIDE.md)
- **Source Code**: Available on GitHub (MIT license)
- **Professional Services**: Custom development available

---

**The SPFx web parts are optional but provide a great SharePoint-native experience for users who prefer to work within SharePoint. Choose the approach that best fits your organization's needs!**

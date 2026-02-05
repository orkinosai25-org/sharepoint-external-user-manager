# External User Management UI - Implementation Guide

## Overview

This guide documents the implementation of the External User Management UI for SharePoint Framework (SPFx), which allows solicitors to visually manage external users through a SaaS backend API.

**Issue:** #9 - SPFx: External User Management UI  
**Status:** ✅ Implemented  
**Last Updated:** 2026-02-05

## Features

### 1. Add External User
- **Input:** Email address and permission level (Read or Edit)
- **Optional:** Company and Project metadata
- **Supports:** Single user and bulk user addition
- **Backend:** Calls `POST /api/external-users`

### 2. Remove User
- **Input:** Select one or more users from the list
- **Confirmation:** Yes/No dialog before removal
- **Backend:** Calls `DELETE /api/external-users`

### 3. List Current External Users
- **Display:** Shows all external users for selected library
- **Information:** Email, display name, permissions, company, project, invited date
- **Backend:** Calls `GET /api/external-users`

## Architecture

### Backend API Integration

All external user operations go through the **SaaS backend API** (not direct SharePoint calls):

```
SPFx UI → BackendApiService → Azure Functions API → Microsoft Graph API
```

#### Why Backend API?
- **Security:** Centralized authentication and authorization
- **Audit Logging:** All operations are logged in the backend
- **Consistency:** Single source of truth for permission management
- **Rate Limiting:** Backend handles throttling and retry logic

### Permission Model

The UI uses a **simplified permission model** for ease of use:

| UI Permission | Backend Permission | SharePoint Role |
|--------------|-------------------|----------------|
| Read | Read | Read |
| Edit | Contribute | Contribute |

**Note:** The backend supports additional permission levels (Edit, FullControl), but the UI intentionally limits choices to Read/Edit for solicitors.

## Components

### 1. ExternalUserManagerWebPart
**Location:** `src/webparts/externalUserManager/ExternalUserManagerWebPart.ts`

**Configuration Properties:**
- `description` - Webpart description text
- `backendApiUrl` - URL of the SaaS backend API
  - Default: `http://localhost:7071/api` (development)
  - Production: `https://your-function-app.azurewebsites.net/api`

**Example Configuration:**
```typescript
{
  description: "Manage external users for client libraries",
  backendApiUrl: "https://your-backend.azurewebsites.net/api"
}
```

### 2. ExternalUserManager Component
**Location:** `src/webparts/externalUserManager/components/ExternalUserManager.tsx`

Main component that displays:
- Library list with external user counts
- Command bar with actions (Add Library, Remove, Manage Users)
- Summary statistics

**Key Handlers:**
- `handleAddUser` - Add single external user via backend API
- `handleBulkAddUsers` - Add multiple users at once
- `handleRemoveUser` - Remove user access
- `handleGetUsers` - List users for a library

### 3. ManageUsersModal Component
**Location:** `src/webparts/externalUserManager/components/ManageUsersModal.tsx`

Modal dialog for managing users in a specific library:
- Lists all external users
- Add User form (single or bulk mode)
- Remove user confirmation
- Edit metadata (company, project)

**Features:**
- Simple Read/Edit permission dropdown
- Email validation for single and bulk modes
- Bulk results display with success/failure status
- User-friendly error messages

### 4. BackendApiService
**Location:** `src/webparts/externalUserManager/services/BackendApiService.ts`

Service class that handles all backend API communication:

**Methods:**
- `listExternalUsers(libraryUrl)` - GET /api/external-users
- `addExternalUser(libraryUrl, email, permission, company?, project?)` - POST /api/external-users
- `removeExternalUser(libraryUrl, email)` - DELETE /api/external-users
- `bulkAddExternalUsers(libraryUrl, emails[], permission, company?, project?)` - Batch POST requests

**Authentication:**
Uses SPFx AAD Token Provider to obtain access tokens for backend API.

**Error Handling:**
```typescript
try {
  // API call
} catch (error) {
  if (error.message.includes('Failed to fetch')) {
    throw new Error('Unable to connect to backend service...');
  }
  throw error; // Re-throw with original message
}
```

## Configuration

### 1. Backend API URL

Configure through webpart properties in SharePoint:

1. Add webpart to a page
2. Click "Edit web part" (pencil icon)
3. In the property pane, set "Backend API URL"
4. Example: `https://your-function-app.azurewebsites.net/api`

### 2. Azure AD Authentication

The backend API requires Azure AD authentication. Ensure:

1. **Backend API Registration:**
   - Register the backend as an Azure AD application
   - Note the Application (client) ID
   
2. **SPFx AAD Permissions:**
   Update `package-solution.json` to request API permissions:
   ```json
   {
     "webApiPermissionRequests": [
       {
         "resource": "Your Backend API",
         "scope": "user_impersonation"
       }
     ]
   }
   ```

3. **Admin Consent:**
   - SharePoint admin must grant consent in the API Access page
   - Navigate to: SharePoint Admin Center → API access

### 3. CORS Configuration

The backend API must allow CORS from SharePoint Online domains:

```typescript
// backend/src/middleware/cors.ts
const allowedOrigins = [
  'https://*.sharepoint.com',
  'https://localhost:4321'  // Development
];
```

## User Interface

### Main View

```
┌─────────────────────────────────────────────────────┐
│ External User Manager                               │
│ Manage external users and shared libraries...      │
│                                                     │
│ ℹ️ Manage external users through the SaaS         │
│   backend API. All operations are securely...     │
│                                                     │
│ [Add Library] [Remove] [Manage Users] [Refresh]   │
│                                                     │
│ ┌─────────────────────────────────────────────┐   │
│ │ ☐ Library Name          | Site URL | Users │   │
│ │ ☐ Client Projects       | /sites/  | 3     │   │
│ │ ☐ Partner Documents     | /sites/  | 5     │   │
│ └─────────────────────────────────────────────┘   │
│                                                     │
│ Total Libraries: 2 | Selected: 0 | Users: 8       │
└─────────────────────────────────────────────────────┘
```

### Manage Users Modal

```
┌─────────────────────────────────────────────────────┐
│ Manage External Users                            ✕  │
│ Library: Client Projects                            │
│                                                     │
│ [Add User] [Bulk Add] [Remove User] [Refresh]     │
│                                                     │
│ ┌─────────────────────────────────────────────┐   │
│ │ ☐ Name      | Email             | Permission│   │
│ │ ☐ John Ext  | john@ext.com      | Read      │   │
│ │ ☐ Jane Part | jane@partner.com  | Edit      │   │
│ └─────────────────────────────────────────────┘   │
│                                                     │
│ Total: 2 | Selected: 0                             │
│                                                     │
│ [Close]                                             │
└─────────────────────────────────────────────────────┘
```

### Add User Form

**Single User Mode:**
```
┌─────────────────────────────────────────────────────┐
│ Add External User                                   │
│                                                     │
│ Email Address *: [john@external.com            ]   │
│ Permission Level *: [Read ▼]                       │
│                                                     │
│ Company: [External Corp                        ]   │
│ Project: [Q1 Campaign                          ]   │
│                                                     │
│ [Add User] [Cancel]                                 │
└─────────────────────────────────────────────────────┘
```

**Bulk User Mode:**
```
┌─────────────────────────────────────────────────────┐
│ Bulk Add External Users                             │
│                                                     │
│ Email Addresses *:                                  │
│ ┌─────────────────────────────────────────────┐   │
│ │ user1@external.com                          │   │
│ │ user2@partner.com                           │   │
│ │ user3@vendor.com                            │   │
│ │                                             │   │
│ └─────────────────────────────────────────────┘   │
│                                                     │
│ Permission Level *: [Read ▼]                       │
│                                                     │
│ Company: [Partner Corp                         ]   │
│ Project: [Integration Project                  ]   │
│                                                     │
│ [Add Users] [Cancel]                                │
└─────────────────────────────────────────────────────┘
```

## Error Messages

All error messages are designed to be user-friendly and actionable:

### Connection Errors
```
❌ Unable to connect to the backend service. Please check your 
   network connection or contact your administrator.
```

### Authentication Errors
```
❌ Authentication failed. Please ensure you are signed in.
```

### Validation Errors
```
❌ Please enter a valid email address
❌ Email address is required
❌ Please enter at least one email address
```

### API Errors
```
❌ Failed to add user: User already has access to this library
❌ Failed to remove user: User not found
❌ Failed to load external users: Library not found
```

## Development

### Prerequisites
- Node.js 18.x LTS (specified in `.nvmrc`)
- SharePoint Online tenant
- Backend API deployed and running

### Build Commands
```bash
# Install dependencies
npm install

# Build solution
npm run build

# Create package for deployment
npm run package-solution

# Run locally with live reload
npm run serve
```

### Local Development
1. Start the backend API locally:
   ```bash
   cd backend
   npm start
   ```
   Backend runs on: `http://localhost:7071`

2. Configure webpart with local backend URL:
   ```
   backendApiUrl: "http://localhost:7071/api"
   ```

3. Start SPFx workbench:
   ```bash
   npm run serve
   ```
   Opens: `https://localhost:4321/temp/workbench.html`

### Testing with Backend API

1. **Test Add User:**
   - Open Manage Users modal
   - Click "Add User"
   - Enter email and select permission
   - Verify POST request to `/api/external-users`

2. **Test Remove User:**
   - Select a user from the list
   - Click "Remove User"
   - Verify DELETE request to `/api/external-users`

3. **Test List Users:**
   - Open Manage Users modal
   - Verify GET request to `/api/external-users?library={url}`

## Deployment

### 1. Package Solution
```bash
npm run clean
npm run build
npm run package-solution
```

This creates: `sharepoint/solution/sharepoint-external-user-manager.sppkg`

### 2. Deploy to App Catalog
1. Navigate to SharePoint Admin Center
2. Go to "More features" → "Apps" → "App Catalog"
3. Upload the `.sppkg` file
4. Check "Make this solution available to all sites"
5. Click "Deploy"

### 3. Grant API Permissions
1. In SharePoint Admin Center → "Advanced" → "API access"
2. Approve pending permission requests
3. Verify backend API permissions are granted

### 4. Add to Site
1. Navigate to your SharePoint site
2. Click "New" → "Page"
3. Add webpart: "External User Manager"
4. Configure backend API URL in properties
5. Publish page

## Monitoring

### Backend Audit Logs
All operations are logged in the backend with:
- User performing action
- Target email address
- Library URL
- Success/failure status
- Timestamp and correlation ID

### Application Insights
The backend uses Application Insights for monitoring:
- API request success rates
- Response times
- Error rates and stack traces
- User activity patterns

Query example:
```kusto
requests
| where name contains "external-users"
| summarize count() by resultCode, bin(timestamp, 1h)
```

## Troubleshooting

### Issue: "Unable to connect to backend service"

**Cause:** Backend API is not reachable or CORS is blocking the request

**Solution:**
1. Verify backend API URL is correct in webpart properties
2. Check backend API is running
3. Verify CORS settings allow SharePoint domain
4. Check browser console for CORS errors

### Issue: "Authentication failed"

**Cause:** Token provider cannot get access token for backend API

**Solution:**
1. Verify API permissions are granted in SharePoint Admin Center
2. Check Azure AD app registration for backend API
3. Ensure user has permissions to access the API
4. Re-authenticate user (sign out and sign in)

### Issue: "User already has access"

**Cause:** User already has permissions on the library

**Solution:**
1. Check if user already exists in the list
2. Remove user first if you need to change their permission level
3. Or update metadata instead of re-adding

### Issue: Build fails with Node version error

**Cause:** Using wrong Node.js version

**Solution:**
```bash
# Use Node 18 LTS
nvm use 18

# Or install it first
nvm install 18
nvm use 18
```

## Best Practices

### 1. Permission Management
- **Start with Read:** Default to Read permission, upgrade to Edit only when necessary
- **Regular Reviews:** Periodically review external users and remove unused access
- **Metadata:** Always add Company and Project for tracking

### 2. Bulk Operations
- **Batch Size:** Keep bulk user additions under 50 users at a time
- **Review Results:** Always check bulk operation results for failures
- **Retry Failures:** Failed additions can be retried individually

### 3. Error Handling
- **User Feedback:** Always show clear messages for success and failure
- **Retry Logic:** Backend handles retries for transient errors
- **Logging:** All errors are logged in backend for troubleshooting

### 4. Security
- **Token Expiry:** Tokens are automatically refreshed by SPFx
- **Least Privilege:** Grant only necessary permissions
- **Audit Trail:** All operations are logged for compliance

## API Reference

### BackendApiService Methods

#### listExternalUsers()
```typescript
async listExternalUsers(libraryUrl: string): Promise<IExternalUser[]>
```
**Parameters:**
- `libraryUrl` - Full URL of the SharePoint library

**Returns:** Array of external users

**Example:**
```typescript
const users = await backendApiService.listExternalUsers(
  'https://contoso.sharepoint.com/sites/client1/Shared%20Documents'
);
```

#### addExternalUser()
```typescript
async addExternalUser(
  libraryUrl: string,
  email: string,
  permission: 'Read' | 'Edit',
  company?: string,
  project?: string
): Promise<void>
```

**Parameters:**
- `libraryUrl` - Full URL of the SharePoint library
- `email` - Email address of external user
- `permission` - Permission level (Read or Edit)
- `company` - Optional company name
- `project` - Optional project name

**Example:**
```typescript
await backendApiService.addExternalUser(
  'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
  'john@external.com',
  'Read',
  'External Corp',
  'Q1 Campaign'
);
```

#### removeExternalUser()
```typescript
async removeExternalUser(
  libraryUrl: string,
  email: string
): Promise<void>
```

**Parameters:**
- `libraryUrl` - Full URL of the SharePoint library
- `email` - Email address of user to remove

**Example:**
```typescript
await backendApiService.removeExternalUser(
  'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
  'john@external.com'
);
```

#### bulkAddExternalUsers()
```typescript
async bulkAddExternalUsers(
  libraryUrl: string,
  emails: string[],
  permission: 'Read' | 'Edit',
  company?: string,
  project?: string
): Promise<BulkResult[]>
```

**Parameters:**
- `libraryUrl` - Full URL of the SharePoint library
- `emails` - Array of email addresses
- `permission` - Permission level for all users
- `company` - Optional company name
- `project` - Optional project name

**Returns:** Array of results with status for each email

**Example:**
```typescript
const results = await backendApiService.bulkAddExternalUsers(
  'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
  ['user1@ext.com', 'user2@ext.com', 'user3@ext.com'],
  'Read',
  'Partner Corp',
  'Integration'
);

// Check results
results.forEach(result => {
  if (result.status === 'success') {
    console.log(`✓ ${result.email}`);
  } else {
    console.log(`✗ ${result.email}: ${result.message}`);
  }
});
```

## Future Enhancements

### Planned Features
1. **Enhanced Search:** Search users by email, company, or project
2. **Bulk Remove:** Remove multiple users at once
3. **Permission History:** View permission change history
4. **Expiration Dates:** Set time-limited access with auto-removal
5. **Email Templates:** Customize invitation email messages
6. **Access Reviews:** Scheduled reminders to review external access
7. **Export:** Export user list to CSV or Excel

### Performance Optimizations
1. **Caching:** Cache user lists for faster loading
2. **Pagination:** Add pagination for libraries with many users
3. **Lazy Loading:** Load user details on demand
4. **Virtual Scrolling:** For large user lists

### Usability Improvements
1. **Tooltips:** Add contextual help for all fields
2. **Keyboard Shortcuts:** Quick actions via keyboard
3. **Mobile Responsive:** Optimize for mobile devices
4. **Dark Mode:** Support dark theme preference

## Support

### Documentation
- **Backend API:** See `EXTERNAL_USER_MANAGEMENT_IMPLEMENTATION.md`
- **Architecture:** See `ARCHITECTURE.md`
- **Deployment:** See `DEPLOYMENT_CHECKLIST.md`

### Contact
For issues or questions:
1. Check this guide and troubleshooting section
2. Review backend audit logs for detailed error information
3. Check Application Insights for API errors
4. Contact your SharePoint administrator

---

**Version:** 1.0.0  
**Last Updated:** 2026-02-05  
**Contributors:** GitHub Copilot Agent

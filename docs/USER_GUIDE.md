# User Guide - ClientSpace

Welcome to ClientSpace, your comprehensive solution for managing external users, client spaces, and document collaboration in Microsoft 365. This guide will help you get the most out of the platform.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Portal Features](#portal-features)
3. [Client Dashboard](#client-dashboard)
4. [External User Management](#external-user-management)
5. [Library Management](#library-management)
6. [List Management](#list-management)
7. [Subscription Management](#subscription-management)
8. [AI Chat Assistant](#ai-chat-assistant)
9. [Settings and Configuration](#settings-and-configuration)
10. [Best Practices](#best-practices)
11. [FAQ](#faq)

## Getting Started

### First Time Login

1. **Navigate to Portal**: Open your ClientSpace portal URL
2. **Sign In**: Click "Sign In" and use your Microsoft 365 credentials
3. **Complete Profile**: On first login, complete your user profile
4. **Dashboard Overview**: You'll see your dashboard with key metrics

### Portal Navigation

**Main Menu** (left sidebar):
- **Dashboard**: Overview of all client spaces and activity
- **Clients**: Manage client spaces and access
- **Users**: View and manage external users
- **Subscriptions**: View subscription status and billing
- **Settings**: Configure tenant settings
- **Support**: Access help resources and AI assistant

**Top Navigation**:
- **Search**: Quick search for clients, users, or documents
- **Notifications**: System notifications and alerts
- **Profile**: Access your profile and sign out

### User Roles

**Administrator**:
- Full access to all features
- Can manage users and settings
- View audit logs
- Configure tenant settings

**User**:
- View assigned client spaces
- Manage external users in assigned spaces
- Create and manage libraries
- Limited access to settings

## Portal Features

### Dashboard

The dashboard provides an at-a-glance view of your tenant:

**Key Metrics**:
- **Total Clients**: Number of client spaces
- **Active External Users**: Current external users with access
- **Libraries**: Total document libraries created
- **Recent Activity**: Latest user invitations and access changes

**Quick Actions**:
- Create new client space
- Invite external user
- View recent activity
- Access AI assistant

**Charts and Analytics**:
- External user growth over time
- Client space distribution
- Most active client spaces
- Access patterns and trends

### Navigation Tips

- **Quick Search**: Press `/` to activate search from anywhere
- **Keyboard Shortcuts**: 
  - `Ctrl+K` or `Cmd+K`: Open command palette
  - `Ctrl+/` or `Cmd+/`: View keyboard shortcuts
- **Breadcrumbs**: Use breadcrumb navigation at the top to navigate back

## Client Dashboard

The Client Dashboard provides a firm-level view of all your client spaces.

### Viewing Client Spaces

**Client List View**:
- Displays all client spaces in a searchable table
- Shows client name, site URL, external user count
- Quick action buttons for each client

**Table Columns**:
- **Client Name**: Name of the client organization
- **Site URL**: SharePoint site URL
- **External Users**: Number of users with access
- **Libraries**: Number of document libraries
- **Last Modified**: Last activity date
- **Status**: Active, Inactive, or Archived
- **Actions**: Quick action menu

### Creating a Client Space

1. **Click "New Client"**: From the dashboard or clients page
2. **Enter Details**:
   - Client name (e.g., "ABC Corporation")
   - Description (optional)
   - Primary contact email
3. **SharePoint Configuration**:
   - Site URL (auto-generated or custom)
   - Template selection (Team Site or Communication Site)
4. **Click "Create"**: The system provisions the SharePoint site
5. **Wait for Completion**: Usually takes 1-2 minutes

### Managing Client Spaces

**Open Client Site**:
- Click the site URL to open in SharePoint
- Opens in new tab

**View Details**:
- Click the client name for detailed view
- Shows external users, libraries, and activity

**Edit Client**:
- Click the edit icon
- Update client name, description, or settings

**Archive Client**:
- Click the archive icon
- Removes external user access
- Preserves data for compliance

**Delete Client**:
- Click the delete icon (requires confirmation)
- Permanently removes the client space
- ⚠️ This action cannot be undone

## External User Management

Manage who has access to your client spaces and what they can do.

### Viewing External Users

**User List**:
- View all external users across all client spaces
- Filter by client, status, or company
- Search by name or email

**User Details**:
- Name and email address
- Company affiliation
- Project associations
- Libraries with access
- Permission level (Read or Edit)
- Invitation date and status

### Inviting External Users

1. **Select Client Space**: Choose which client space to invite users to
2. **Click "Invite User"**
3. **Enter User Information**:
   - Email address (required)
   - Display name (optional, auto-populated if known)
   - Company name (for tracking)
   - Project name (optional)
4. **Select Libraries**: Choose which libraries to grant access to
5. **Set Permission Level**:
   - **Read**: View and download only
   - **Edit**: View, download, and upload
6. **Personal Message**: Add custom message to invitation (optional)
7. **Click "Send Invitation"**

The user will receive an email invitation to access SharePoint.

### Managing User Access

**Update Permissions**:
1. Navigate to user details
2. Click "Edit Permissions"
3. Add or remove library access
4. Change permission levels
5. Click "Save"

**Revoke Access**:
1. Navigate to user details
2. Click "Revoke Access"
3. Confirm action
4. User access is immediately removed

**Resend Invitation**:
1. Navigate to user with "Pending" status
2. Click "Resend Invitation"
3. User receives new invitation email

### Bulk Operations

**Invite Multiple Users**:
1. Click "Bulk Invite"
2. Upload CSV file with user details
3. Map columns to required fields
4. Review and confirm
5. Track progress in bulk operations page

**CSV Format**:
```csv
Email,DisplayName,Company,Project,Libraries,Permission
john@example.com,John Doe,ABC Corp,Project A,Documents;Contracts,Edit
jane@example.com,Jane Smith,XYZ Inc,Project B,Documents,Read
```

**Export User List**:
- Click "Export" on users page
- Choose format (CSV or Excel)
- Download file with all user data

### User Metadata

Track additional information about external users:

**Company**: Organization the user works for
**Project**: Specific project or matter
**Tags**: Custom tags for categorization
**Notes**: Internal notes (not visible to user)

## Library Management

Create and manage document libraries for organizing client documents.

### Creating Libraries

1. **Select Client Space**
2. **Click "New Library"**
3. **Enter Details**:
   - Library name (e.g., "Contracts")
   - Description
   - Template (optional)
4. **Configure Settings**:
   - Versioning: Enable/disable version history
   - Approval: Require approval for changes
   - Check-out: Require document check-out
5. **Click "Create"**

### Library Features

**Document Library**:
- Store and organize documents
- Version history
- Check-in/check-out
- Metadata columns
- Content approval

**Library Settings**:
- Permissions
- Versioning settings
- Advanced settings
- Delete library

### Managing Library Permissions

**View Permissions**:
1. Navigate to library details
2. Click "Permissions" tab
3. See all users with access and their levels

**Grant Access**:
1. Click "Grant Access"
2. Select users or groups
3. Choose permission level
4. Click "Share"

**Break Inheritance**:
- By default, libraries inherit site permissions
- Click "Stop Inheriting" to set unique permissions
- Manually manage who has access

## List Management

Create SharePoint lists for tracking information.

### Creating Lists

1. **Select Client Space**
2. **Click "New List"**
3. **Choose Template**:
   - Custom List (blank)
   - Tasks
   - Issues
   - Contacts
4. **Enter Details**:
   - List name
   - Description
5. **Configure Columns**: Add custom columns as needed
6. **Click "Create"**

### List Templates

**Tasks List**:
- Track project tasks
- Assign to users
- Set due dates
- Mark complete

**Issues List**:
- Track issues or problems
- Assign priority
- Set status
- Link to related items

**Contacts List**:
- Store contact information
- Link to external users
- Track interactions

### Managing List Items

**Add Item**:
1. Open list
2. Click "New Item"
3. Fill in fields
4. Click "Save"

**Edit Item**:
1. Click item to open
2. Click "Edit"
3. Update fields
4. Click "Save"

**Delete Item**:
1. Select item(s)
2. Click "Delete"
3. Confirm action

## Subscription Management

Manage your ClientSpace subscription and billing.

### Viewing Subscription

**Subscription Details**:
- Plan name (Free, Professional, Enterprise)
- Status (Active, Trial, Expired)
- Billing cycle (Monthly or Annual)
- Next billing date
- Current usage vs. plan limits

**Plan Limits**:
- Maximum client spaces
- Maximum external users
- Storage quota
- API rate limits
- Support level

### Upgrading Your Plan

1. **Navigate to Subscriptions**
2. **Click "Upgrade"**
3. **Compare Plans**: Review features and pricing
4. **Select Plan**: Choose Professional or Enterprise
5. **Enter Billing Details**:
   - Payment method (credit card via Stripe)
   - Billing address
6. **Review and Confirm**
7. **Complete Payment**

Your plan is upgraded immediately upon successful payment.

### Managing Billing

**Update Payment Method**:
1. Go to Subscriptions → Billing
2. Click "Update Payment Method"
3. Enter new card details
4. Click "Save"

**View Invoices**:
1. Go to Subscriptions → Invoices
2. View list of past invoices
3. Download PDF or receipt

**Cancel Subscription**:
1. Go to Subscriptions → Settings
2. Click "Cancel Subscription"
3. Provide feedback (optional)
4. Confirm cancellation

> **Note**: Your data is retained for 30 days after cancellation.

## AI Chat Assistant

ClientSpace includes an AI-powered chat assistant to help you use the platform.

### Accessing the Assistant

**Methods**:
- Click the chat icon (bottom right)
- Press `Ctrl+Shift+A` or `Cmd+Shift+A`
- Navigate to Support → AI Assistant

### Using the Assistant

**Ask Questions**:
- "How do I invite an external user?"
- "What's the difference between Read and Edit permissions?"
- "How do I create a new client space?"

**Get Help**:
- Troubleshooting issues
- Understanding features
- Best practices
- Navigation guidance

**Example Interactions**:

```
User: How do I give someone edit access to a library?
Assistant: To grant edit access to a library:
1. Navigate to the client space
2. Open the library
3. Click "Share" or "Permissions"
4. Enter the user's email
5. Select "Edit" permission level
6. Click "Share"

Would you like me to guide you through this process?
```

### Features

- **Context-Aware**: Understands your current page
- **Smart Suggestions**: Offers relevant help based on your actions
- **Multi-Language**: Supports multiple languages
- **Learning**: Improves over time based on interactions

## Settings and Configuration

Configure your tenant settings and preferences.

### Tenant Settings

**General Settings**:
- Tenant name
- Primary domain
- Administrator email
- Time zone
- Language preference

**SharePoint Settings**:
- Default site template
- Root site URL
- App catalog URL
- Default permissions

**Security Settings**:
- Require MFA for admins
- External sharing policy
- Session timeout
- IP restrictions (Enterprise only)

### User Management

**Add Users**:
1. Go to Settings → Users
2. Click "Add User"
3. Enter email address
4. Assign role (Admin or User)
5. Click "Invite"

**Manage Roles**:
- Admin: Full access
- User: Limited access
- Custom: Define custom permissions (Enterprise only)

### Integration Settings

**API Configuration**:
- View API endpoints
- Generate API keys
- Set rate limits
- Configure webhooks

**Stripe Integration**:
- Connect Stripe account
- Configure webhook endpoints
- View payment history

### Audit Logs

**View Logs**:
1. Go to Settings → Audit Logs
2. Filter by:
   - Date range
   - User
   - Action type
   - Resource

**Export Logs**:
- Click "Export"
- Choose format (CSV, JSON)
- Download for compliance

**Log Retention**:
- Free: 30 days
- Professional: 90 days
- Enterprise: 1 year (customizable)

## Best Practices

### Client Space Organization

✅ **Do**:
- Use clear, descriptive names for clients
- Create separate spaces for each client
- Archive inactive clients to reduce clutter
- Regularly review and clean up old data

❌ **Don't**:
- Mix multiple clients in one space
- Use abbreviations that aren't clear
- Leave test spaces in production
- Share administrator credentials

### External User Management

✅ **Do**:
- Always specify company and project for external users
- Use the minimum required permission level (prefer Read over Edit)
- Regularly review and revoke unused access
- Set reminders to review access quarterly
- Use bulk operations for efficiency

❌ **Don't**:
- Give everyone Edit permissions by default
- Forget to revoke access when projects end
- Share sensitive documents with unnecessary users
- Ignore pending invitations

### Library Organization

✅ **Do**:
- Create separate libraries for different document types
- Use consistent naming conventions
- Enable versioning for important documents
- Document library purposes in descriptions

❌ **Don't**:
- Create too many libraries (keep it simple)
- Use generic names like "Documents"
- Store unrelated documents together
- Forget to configure permissions

### Security Best Practices

✅ **Do**:
- Enable multi-factor authentication
- Use strong, unique passwords
- Review audit logs regularly
- Train users on security awareness
- Report suspicious activity immediately

❌ **Don't**:
- Share login credentials
- Use public Wi-Fi without VPN
- Click suspicious links in emails
- Ignore security warnings
- Disable security features

## FAQ

### General Questions

**Q: What is ClientSpace?**
A: ClientSpace is a SaaS solution for managing external user access to SharePoint Online, specifically designed for professional services firms managing client collaboration.

**Q: Do I need to install anything?**
A: The portal and API are cloud-hosted. You only need to install the SPFx web parts in your SharePoint tenant (one-time setup).

**Q: What browsers are supported?**
A: Modern versions of Chrome, Edge, Firefox, and Safari. Edge and Chrome provide the best experience.

**Q: Is my data secure?**
A: Yes. All data is encrypted at rest and in transit. We follow Microsoft security best practices and comply with industry standards.

### External User Management

**Q: What's the difference between Read and Edit permissions?**
A: Read allows viewing and downloading only. Edit allows viewing, downloading, uploading, and modifying documents.

**Q: Can external users see other client spaces?**
A: No. External users only have access to the specific libraries they're invited to. They cannot see other clients or spaces.

**Q: How long do invitations remain valid?**
A: Invitations expire after 30 days. Users can request a new invitation if theirs expires.

**Q: Can I invite multiple users at once?**
A: Yes, use the bulk invite feature with a CSV file to invite multiple users simultaneously.

### Billing and Subscriptions

**Q: What happens when my trial ends?**
A: You'll be prompted to upgrade to a paid plan. Your data is retained for 30 days, giving you time to upgrade.

**Q: Can I change plans?**
A: Yes, you can upgrade or downgrade at any time. Changes take effect immediately.

**Q: What payment methods are accepted?**
A: We accept all major credit cards via Stripe. Enterprise customers can request invoice billing.

**Q: Is there a refund policy?**
A: We offer a 30-day money-back guarantee for annual subscriptions. Monthly subscriptions are non-refundable.

### Technical Questions

**Q: Can I use the API programmatically?**
A: Yes, we provide a REST API for integration. See the [API Reference](./saas/api-spec.md) for details.

**Q: Do you have a mobile app?**
A: The web portal is fully responsive and works on mobile devices. The SPFx web parts work in the SharePoint mobile app.

**Q: Can I customize the branding?**
A: Enterprise customers can customize colors and logos. See [Branding Guide](./branding/README.md).

**Q: Do you support single sign-on (SSO)?**
A: Yes, via Azure AD. All authentication uses your Microsoft 365 credentials.

### Support Questions

**Q: How do I get support?**
A: Use the AI chat assistant, check documentation, or contact support via the portal. Enterprise customers have priority support.

**Q: What are your support hours?**
A: Standard support: Business hours (9am-5pm local time). Enterprise support: 24/7.

**Q: Do you offer training?**
A: Yes, we provide documentation, video tutorials, and live training sessions (Enterprise plan).

## Getting Help

### Resources

- **Documentation**: [docs/README.md](./README.md)
- **Installation Guide**: [INSTALLATION_GUIDE.md](./INSTALLATION_GUIDE.md)
- **API Reference**: [saas/api-spec.md](./saas/api-spec.md)
- **Developer Guide**: [DEVELOPER_GUIDE.md](../DEVELOPER_GUIDE.md)

### Support Channels

- **AI Assistant**: Available 24/7 in the portal
- **Email Support**: support@clientspace.com
- **Documentation**: Comprehensive guides and tutorials
- **Community Forum**: Share tips and ask questions

### Feedback

We welcome your feedback! To submit feedback:
1. Click the feedback icon in the portal
2. Describe your suggestion or issue
3. Click "Submit"

---

**Thank you for using ClientSpace!** We're here to help you manage external collaboration efficiently and securely.

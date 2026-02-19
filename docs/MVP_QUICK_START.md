# ClientSpace MVP Quick Start Guide

**Get up and running with ClientSpace in 5 minutes**

This quick start guide will help you get started with ClientSpace MVP as quickly as possible, from first login to managing your first external user.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [First Login (2 minutes)](#first-login-2-minutes)
3. [Create Your First Client Space (1 minute)](#create-your-first-client-space-1-minute)
4. [Invite Your First External User (2 minutes)](#invite-your-first-external-user-2-minutes)
5. [Next Steps](#next-steps)

---

## Prerequisites

Before you start, ensure you have:

- ✅ **Microsoft 365 account** with admin permissions
- ✅ **ClientSpace portal URL** (provided by your administrator or during signup)
- ✅ **Azure AD admin consent** completed (done during tenant onboarding)

> **New to ClientSpace?** If you haven't signed up yet, see the [Installation Guide](INSTALLATION_GUIDE.md) for complete onboarding instructions.

---

## First Login (2 minutes)

### Step 1: Access the Portal

1. Open your browser and navigate to your ClientSpace portal URL:
   ```
   https://your-tenant.clientspace.app
   ```

2. Click **Sign In** button

3. Sign in with your Microsoft 365 credentials

4. **Grant permissions** if prompted (first-time login only)

### Step 2: Complete Your Profile

On first login, you'll be prompted to complete your profile:

- **Display Name**: Your full name
- **Organization**: Your company/firm name
- **Role**: Select your role (e.g., Administrator, Partner, Associate)

Click **Save** to continue.

### Step 3: Explore the Dashboard

You'll land on the **Dashboard** which shows:

- **Overview metrics**: Client spaces, external users, libraries
- **Recent activity**: Latest invitations and changes
- **Quick actions**: Create client, invite user, access help

> 💡 **Tip**: Press `Ctrl+K` (or `Cmd+K` on Mac) to open the command palette from anywhere in the portal.

---

## Create Your First Client Space (1 minute)

Client spaces are where you manage documents and external users for each client.

### Steps:

1. From the **Dashboard**, click **New Client** (or navigate to **Clients** → **New Client**)

2. Fill in the client details:
   ```
   Client Name: ABC Corporation
   Description: Matter #2024-001 - Corporate Transaction
   Primary Contact: john.smith@abccorp.com
   ```

3. Configure SharePoint settings (or use defaults):
   - **Site URL**: Auto-generated as `ABC-Corporation` (or customize)
   - **Template**: Team Site (recommended for collaboration)

4. Click **Create Client Space**

5. Wait 1-2 minutes for provisioning to complete

✅ **Done!** Your client space is ready. You'll see it in the client list with:
- Client name
- SharePoint site URL (clickable)
- Status: Active
- Quick action buttons

---

## Invite Your First External User (2 minutes)

Now let's invite an external user to access the client space.

### Steps:

1. **Navigate to the client**:
   - From **Dashboard** or **Clients** page
   - Click on **ABC Corporation** to view details

2. **Click "Invite External User"**

3. **Enter user details**:
   ```
   Email Address: jane.doe@example.com
   Display Name: Jane Doe (auto-populated if known)
   Company: Example Consulting Ltd
   Project: Corporate Transaction 2024
   ```

4. **Select access**:
   - **Libraries**: Select "Documents" (created by default)
   - **Permission Level**: Choose **Read** (view/download) or **Edit** (view/download/upload)

5. **(Optional) Add a personal message**:
   ```
   Hi Jane, you've been granted access to our client documents for the corporate transaction. 
   Please let me know if you have any questions.
   ```

6. **Click "Send Invitation"**

✅ **Done!** The user will receive an email invitation to access the SharePoint site.

### What Happens Next?

- User receives email invitation
- User clicks link and signs in with their email
- User gains access to specified libraries with selected permissions
- You can track invitation status in **Users** section (Pending → Active)

---

## Next Steps

Now that you've completed the quick start, here's what to do next:

### Learn More Features

- 📚 **[User Guide](USER_GUIDE.md)**: Complete guide to all portal features
- 🔍 **[Search Feature Guide](../SEARCH_FEATURE_GUIDE.md)**: Using client space and global search
- 🔐 **[External User Management Guide](../EXTERNAL_USER_MANAGEMENT_UI_GUIDE.md)**: Advanced user management
- 📊 **[Library Management](USER_GUIDE.md#library-management)**: Organize documents effectively

### Explore Advanced Features

1. **Create Multiple Client Spaces**
   - Organize clients by matter, project, or department
   - Use naming conventions for easy identification

2. **Bulk Invite Users**
   - Upload CSV file with multiple users
   - Save time on large projects
   - See [User Guide - Bulk Operations](USER_GUIDE.md#bulk-operations)

3. **Use Search** (Professional/Enterprise plans)
   - Search across all client spaces
   - Search within specific client spaces
   - Filter by document type, date, user

4. **Manage Subscriptions**
   - View plan limits and usage
   - Upgrade for more features
   - Configure billing settings
   - See [User Guide - Subscription Management](USER_GUIDE.md#subscription-management)

5. **Configure Settings**
   - Set up team members with roles
   - Configure SharePoint defaults
   - Enable security features
   - See [User Guide - Settings](USER_GUIDE.md#settings-and-configuration)

### Get Help

- 🤖 **AI Chat Assistant**: Click the chat icon (bottom right) for instant help
- 📖 **Documentation**: Browse the [docs folder](README.md) for comprehensive guides
- 📧 **Email Support**: support@clientspace.com
- 💬 **Community Forum**: Coming soon

### Best Practices

Before you dive deeper, review these best practices:

1. **Use meaningful names** for clients and libraries
2. **Grant minimum required permissions** (prefer Read over Edit)
3. **Review external users quarterly** and revoke unused access
4. **Enable versioning** on document libraries for important files
5. **Use metadata** (company, project) to organize external users

---

## Common Quick Start Issues

### Issue: Can't Sign In

**Problem**: Error message when trying to sign in

**Solution**:
1. Verify you're using the correct portal URL
2. Check that you're using your Microsoft 365 account
3. Clear browser cache and cookies
4. Try a different browser
5. Contact your administrator to verify account setup

### Issue: Can't Create Client Space

**Problem**: Error when creating client space

**Solution**:
1. Check your subscription plan allows more client spaces
2. Verify SharePoint site URL is unique
3. Ensure you have admin role assigned
4. Check Azure AD admin consent is granted
5. Review [Troubleshooting Guide](INSTALLATION_GUIDE.md#troubleshooting)

### Issue: External User Didn't Receive Invitation

**Problem**: User reports no email received

**Solution**:
1. Check email address is correct (no typos)
2. Ask user to check spam/junk folder
3. Verify external sharing is enabled in SharePoint
4. Resend invitation from Users page
5. Check user's email domain isn't blocked by your organization

---

## Keyboard Shortcuts

Speed up your workflow with these keyboard shortcuts:

| Shortcut | Action |
|----------|--------|
| `Ctrl+K` or `Cmd+K` | Open command palette |
| `/` | Activate search |
| `Ctrl+/` or `Cmd+/` | View all shortcuts |
| `Ctrl+Shift+A` or `Cmd+Shift+A` | Open AI assistant |
| `Esc` | Close modals/dialogs |

---

## Video Walkthroughs

📹 **Coming Soon**: Video tutorials for each quick start step

---

## Feedback

We'd love to hear from you!

- **In-app feedback**: Click the feedback icon (⭐) in the portal
- **Email us**: feedback@clientspace.com
- **Report issues**: support@clientspace.com

---

## Related Documentation

- **[Installation Guide](INSTALLATION_GUIDE.md)**: Complete deployment and onboarding
- **[User Guide](USER_GUIDE.md)**: Comprehensive feature documentation
- **[API Reference](MVP_API_REFERENCE.md)**: For developers integrating with ClientSpace
- **[Deployment Runbook](MVP_DEPLOYMENT_RUNBOOK.md)**: For administrators deploying ClientSpace
- **[Support Runbook](MVP_SUPPORT_RUNBOOK.md)**: For support staff troubleshooting issues

---

**Welcome to ClientSpace!** 🎉

You're now ready to efficiently manage external collaboration in Microsoft 365.

If you have any questions, use the AI chat assistant or contact our support team.

---

*Last Updated: February 2026*  
*Version: MVP 1.0*

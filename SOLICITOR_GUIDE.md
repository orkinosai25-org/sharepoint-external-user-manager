# Solicitor Onboarding & Usage Guide

## Welcome to SharePoint External User Manager

This guide will help you manage your clients and their access to documents without needing any technical SharePoint knowledge. Think of this system as your central command center for managing all client relationships and document sharing in one place.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Understanding the Dashboard](#understanding-the-dashboard)
3. [Adding a New Client](#adding-a-new-client)
4. [Managing Client Access](#managing-client-access)
5. [Adding Document Spaces](#adding-document-spaces)
6. [Best Practices](#best-practices)
7. [Common Questions](#common-questions)

---

## Getting Started

### What is This System?

SharePoint External User Manager is a tool that helps law firms:
- **Organize client workspaces** - Each client gets their own secure space
- **Control who sees what** - Give clients access only to their own documents
- **Track everything** - See who has access to what, and when they were invited
- **Stay compliant** - Maintain audit trails of all access changes

### First Time Login

When you first open the system, you'll see two main sections:

1. **Client Dashboard** - A list of all your clients and their workspace status
2. **Document Library Manager** - Tools to manage documents and permissions for each client

No technical setup is required - just open the app and start using it!

---

## Understanding the Dashboard

### Client Dashboard Overview

The Client Dashboard shows all your client workspaces in a simple table format:

| What You See | What It Means |
|-------------|---------------|
| **Client Name** | The name of the client company or matter |
| **Site URL** | Where the client's documents are stored (you can click to open) |
| **Status** | Whether the workspace is ready to use |
| **Actions** | Quick buttons to manage each client |

#### Status Indicators

- **Active** ✓ - The client workspace is ready to use
- **Provisioning** ⏳ - The system is setting up the workspace (usually takes a few minutes)
- **Archived** 📦 - The matter is closed, but documents are preserved

### Quick Actions

For each client, you'll see action buttons:

- **Open Site** - Opens the client's document workspace in a new window
- **Manage Users** - Control who has access to this client's documents
- **View Details** - See complete information about the client workspace

---

## Adding a New Client

When you take on a new client or start a new matter, here's how to set up their workspace:

### Step-by-Step Process

#### Step 1: Click "Add Client"
Look for the **"Add Client"** button at the top of your Client Dashboard. It's usually in the command bar with a plus (+) icon.

#### Step 2: Fill in Client Details

You'll be asked for:

**Required Information:**
- **Client Name** - Use a clear, descriptive name (e.g., "Smith v. Jones - Litigation" or "ABC Corp - M&A Transaction")
- **Description** (optional but recommended) - Brief note about the matter

**Example:**
```
Client Name: Acme Corporation - Employment Matter
Description: Wrongful termination case, Jones v. Acme Corp
```

#### Step 3: Review and Submit

- Double-check the client name is spelled correctly (you can change it later if needed)
- Click **"Create Client"** or **"Save"**
- The system will start setting up the workspace

#### Step 4: Wait for Provisioning

- The status will show **"Provisioning"** for a few minutes
- You'll receive a notification when it's ready
- Once status changes to **"Active"**, you can start adding users and documents

### What Gets Created?

When you add a client, the system automatically:
- Creates a secure document workspace
- Sets up folders for different document types
- Applies your firm's security settings
- Generates an audit log for compliance

---

## Managing Client Access

### Overview of Access Management

For each client workspace, you can control exactly who can access documents and what they can do with them.

### Types of Access

There are two main permission levels:

1. **View Only (Read)** - Can see and download documents, but cannot edit or upload
2. **Edit Access** - Can view, download, upload, and edit documents

**Pro Tip:** Start with "View Only" access and upgrade to "Edit" only when necessary.

### Adding Users to a Client Workspace

#### Step 1: Select the Client
From your Client Dashboard, find the client and click **"Manage Users"**

#### Step 2: Add User
Click the **"Add User"** button (usually has a person icon with a +)

#### Step 3: Enter User Information

You'll need:
- **Email Address** - The external user's work email
- **Permission Level** - Choose "View Only" or "Edit Access"
- **Company** (optional) - The user's organization name
- **Project/Matter** (optional) - Specific project they're working on

**Example:**
```
Email: john.smith@clientcompany.com
Permission: View Only
Company: Client Company LLC
Project: 2026 Annual Review
```

#### Step 4: Send Invitation

- Click **"Add"** or **"Invite User"**
- The user will receive an email invitation
- They'll need to accept the invitation to access documents

### Removing User Access

When a matter concludes or access is no longer needed:

#### Step 1: Open User Management
Navigate to the client and click **"Manage Users"**

#### Step 2: Select Users to Remove
- Find the user(s) in the list
- Check the box next to their name
- You can select multiple users at once

#### Step 3: Remove Access
- Click **"Remove Selected"** or **"Remove User"**
- Confirm your choice in the dialog that appears
- Access is revoked immediately

**Important:** Removed users can no longer access any documents in that client workspace.

### Bulk User Management

When you need to add multiple users at once (e.g., for a large transaction):

#### Using Bulk Add Feature

1. Click **"Add Multiple Users"** or **"Bulk Add"**
2. Enter email addresses (one per line or comma-separated)
3. Choose a permission level for all users
4. Optionally set company and project for the group
5. Click **"Add All"** to send invitations

**Example for Transaction Team:**
```
lawyer1@acquirer.com
lawyer2@acquirer.com  
consultant@advisors.com
cfo@acquirer.com

Permission: View Only
Company: Acquirer Corp
Project: Merger Due Diligence
```

### Viewing Current Access

To see who currently has access:

1. Open the client workspace
2. Click **"Manage Users"** 
3. Review the user list showing:
   - User names and emails
   - Permission levels
   - Company and project assignments
   - Date they were invited
   - Last access date

---

## Adding Document Spaces

### Understanding Document Spaces

A "document space" or "library" is like a digital filing cabinet within a client workspace. You might create different spaces for:
- Correspondence
- Court filings
- Discovery documents
- Contracts
- Closing documents

### Creating a New Document Space

#### Step 1: Open External User Manager
From your main menu, select **"External User Manager"** or **"Document Libraries"**

#### Step 2: Create New Library

Click **"Add Library"** or **"Create Document Space"**

#### Step 3: Configure the Library

Provide:
- **Library Name** - Clear, descriptive name (e.g., "Discovery Documents", "Correspondence")
- **Description** - What types of documents go here
- **External Sharing** - Enable if external users need access

**Example:**
```
Library Name: Merger Documents
Description: Due diligence materials for ABC Corp acquisition
External Sharing: Enabled
```

#### Step 4: Set Up Folders (Optional)

After creating the library, you can organize it with folders:
- Click into the new library
- Create folders for different document types
- Apply permissions at the folder level if needed

### Managing Document Space Access

#### Linking Library to Clients

Once a document space is created:

1. Go to the library's settings
2. Select **"Manage Permissions"**
3. Add client workspaces that should have access
4. Set permission levels (View or Edit)

#### Organizing Multiple Libraries

For complex matters, you might have:
- **General Documents** - Shared among all parties
- **Privileged Communications** - Attorney work product only
- **Client Documents** - Uploaded by the client
- **Court Filings** - Organized by filing date

**Best Practice:** Use clear, consistent naming conventions across all clients.

---

## Best Practices

### 1. Naming Conventions

**For Clients:**
- Use consistent format: `[Client Name] - [Matter Type]`
- Include case numbers if applicable
- Examples:
  - "ABC Corp - M&A Transaction 2026"
  - "Smith v. Jones - Case #12345"
  - "Johnson Estate - Probate"

**For Document Spaces:**
- Be descriptive and specific
- Indicate the document type or phase
- Examples:
  - "Discovery - Phase 1"
  - "Contract Drafts"
  - "Final Executed Documents"

### 2. Access Management

**Start Restrictive:**
- Begin with "View Only" access
- Upgrade to "Edit" only when truly needed
- Review and remove access regularly

**Regular Access Reviews:**
- Monthly: Review external users for active matters
- Quarterly: Audit all client access
- When matter closes: Remove all external access

**Document Who Has Access:**
- Use the "Company" and "Project" fields consistently
- This helps during audits and access reviews
- Makes it easy to remove access by project

### 3. Document Organization

**Create Clear Folder Structures:**
```
Client Workspace
├── Correspondence
│   ├── Client Communications
│   └── Opposing Counsel
├── Court Documents
│   ├── Pleadings
│   └── Orders
├── Discovery
│   ├── Requests
│   ├── Responses
│   └── Productions
└── Work Product (attorneys only)
```

**Use Descriptive File Names:**
- Include dates: `2026-02-05_Complaint.pdf`
- Include document type: `Motion_Summary_Judgment.pdf`
- Avoid spaces: Use underscores or dashes

### 4. Security Practices

**Protect Privileged Information:**
- Create separate libraries for attorney work product
- Never add external users to privileged folders
- Mark documents clearly: "ATTORNEY-CLIENT PRIVILEGED"

**Monitor Access:**
- Check the audit logs regularly
- Note when external users access documents
- Investigate any unusual activity

**Timely Removal:**
- Remove access immediately when:
  - Matter concludes
  - Client relationship ends
  - External consultant completes their work
  - Employee leaves client organization

### 5. Communication with External Users

**Set Expectations:**
- Inform clients when they've been granted access
- Explain what they can and cannot do
- Provide a contact for technical issues

**Email Template for New Users:**
```
Subject: Access to Client Document Portal

Dear [Client Name],

You have been granted access to our secure document portal for 
[Matter Name]. You will receive an invitation email from Microsoft 
SharePoint - please click the link to accept access.

You will be able to [view/view and edit] documents related to this matter.

If you have any questions or issues accessing documents, please 
contact [Your Name] at [Your Email].

Best regards,
[Your Firm]
```

### 6. Maintenance Tasks

**Weekly:**
- Check for provisioning errors
- Review any access requests
- Upload new documents to appropriate spaces

**Monthly:**
- Review external user list
- Remove access for completed matters
- Update client workspace descriptions if needed

**Quarterly:**
- Full access audit
- Archive completed matters
- Review and update folder structures

---

## Common Questions

### Q: How long does it take to set up a new client workspace?

**A:** Usually 2-5 minutes. The system shows "Provisioning" status while it sets everything up. You'll see it change to "Active" when ready.

---

### Q: Can I change a client's name after creating the workspace?

**A:** Yes! Open the client details and click "Edit" to update the name, description, or other settings.

---

### Q: What happens if I accidentally remove someone's access?

**A:** You can immediately add them back using the same process. They'll receive a new invitation email. Previously uploaded documents remain unchanged.

---

### Q: Can external users see other clients' documents?

**A:** No. Each client workspace is completely isolated. External users only see documents in workspaces where they've been explicitly granted access.

---

### Q: How do I know if an external user has accessed documents?

**A:** In the "Manage Users" section, you can see the last access date for each user. For detailed audit logs, contact your IT administrator.

---

### Q: Can external users invite other people?

**A:** No. Only firm members can invite external users. This ensures you maintain complete control over access.

---

### Q: What if a client doesn't receive the invitation email?

**A:** Common solutions:
1. Check their spam/junk folder
2. Verify the email address is correct
3. Resend the invitation from the user management screen
4. Try using their alternate email address

---

### Q: How many users can I add to a client workspace?

**A:** There's no practical limit. You can add as many external users as needed for each matter.

---

### Q: Can I copy documents from one client workspace to another?

**A:** Yes, but do so carefully to avoid mixing client confidential information. Open both workspaces and manually copy files, ensuring no privileged information crosses client boundaries.

---

### Q: What happens to documents when I archive a client?

**A:** Documents are preserved and remain accessible to firm members. External users lose access. You can reactivate the client if needed.

---

### Q: Can clients upload documents?

**A:** Only if you give them "Edit Access" permission. Users with "View Only" can see and download, but cannot upload or modify documents.

---

### Q: Is this system secure enough for confidential legal documents?

**A:** Yes. The system uses Microsoft's enterprise-grade security:
- All data encrypted in transit and at rest
- Multi-factor authentication supported
- Complete audit trails
- Complies with legal industry standards

Always follow your firm's information security policies.

---

### Q: Can I use this on my phone or tablet?

**A:** Yes! The system works on any device with a web browser. The mobile interface adapts to smaller screens for easy access on the go.

---

## Getting Help

### Technical Support

If you encounter issues:
1. Check this guide for common solutions
2. Contact your firm's IT support team
3. Reference the error message when reporting issues

### Training

New to the system? Consider:
- Reviewing this guide thoroughly
- Practicing with a test client workspace
- Attending firm training sessions
- Asking a colleague for a walkthrough

### Feedback

Have suggestions for improvement?
- Submit feedback through your firm's internal channels
- Document any recurring issues
- Share tips with colleagues

---

## Summary Checklist

Use this quick checklist for common tasks:

### Adding a New Client
- [ ] Click "Add Client"
- [ ] Enter client name and description
- [ ] Wait for "Active" status
- [ ] Add document libraries as needed
- [ ] Invite external users

### Adding External User
- [ ] Open client workspace
- [ ] Click "Manage Users"
- [ ] Click "Add User"
- [ ] Enter email and permission level
- [ ] Add company and project info
- [ ] Send invitation

### Closing a Matter
- [ ] Remove all external user access
- [ ] Download final documents if needed
- [ ] Archive client workspace
- [ ] Update matter status
- [ ] Document closure date

---

*This guide is designed for solicitors and legal professionals. No SharePoint or technical knowledge required. For technical documentation, see [DEVELOPER_GUIDE.md](./DEVELOPER_GUIDE.md).*

**Last Updated:** February 2026  
**Version:** 1.0

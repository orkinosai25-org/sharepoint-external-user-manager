# ClientSpace MVP UX Screen Guide

**Complete user experience guide for all ClientSpace portal screens**

This document provides detailed guidance for every screen in the ClientSpace portal, including layouts, user flows, interactive elements, and best practices for each feature.

## Table of Contents

1. [Overview](#overview)
2. [Dashboard](#dashboard)
3. [Client Management](#client-management)
4. [External User Management](#external-user-management)
5. [Library Management](#library-management)
6. [Search](#search)
7. [Subscription & Billing](#subscription--billing)
8. [Settings](#settings)
9. [AI Chat Assistant](#ai-chat-assistant)
10. [Navigation & Common Elements](#navigation--common-elements)

---

## Overview

### Design Principles

ClientSpace follows these core UX principles:

1. **Clarity**: Clear labels, obvious actions, minimal jargon
2. **Efficiency**: Quick access to common tasks, keyboard shortcuts
3. **Consistency**: Uniform patterns across all screens
4. **Feedback**: Immediate visual feedback for all actions
5. **Accessibility**: WCAG 2.1 AA compliant, keyboard navigable

### Design System

- **Framework**: Blazor with Bootstrap 5
- **Icons**: Fluent UI System Icons
- **Typography**: Segoe UI (Windows), San Francisco (Mac), system default fallbacks
- **Colors**: SharePoint-aligned palette (see [Branding Guide](branding/README.md))

---

## Dashboard

### Screen Overview

The Dashboard is the first screen users see after login. It provides an at-a-glance view of tenant activity and quick access to common actions.

### Layout

```
┌────────────────────────────────────────────────────────────────┐
│  Header (Navigation Bar)                                       │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  Welcome, [User Name]!                                         │
│                                                                │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
│  │ 15 Clients  │ │ 120 Users   │ │ 45 Libraries│            │
│  │ +2 this wk  │ │ +8 this wk  │ │ +3 this wk  │            │
│  └─────────────┘ └─────────────┘ └─────────────┘            │
│                                                                │
│  Recent Activity                           Quick Actions      │
│  ┌────────────────────────────────┐        ┌───────────────┐ │
│  │ • User invited to ABC Corp...  │        │ + New Client  │ │
│  │ • Library created in XYZ...    │        │ + Invite User │ │
│  │ • Access revoked for Jane...   │        │ 🔍 Search     │ │
│  └────────────────────────────────┘        └───────────────┘ │
│                                                                │
│  Client Spaces                                                 │
│  ┌────────────────────────────────────────────────────────────┤
│  │ ABC Corp    │ 5 users │ 3 libs │ Active │ [Actions ▼] │  │
│  │ XYZ Inc     │ 3 users │ 2 libs │ Active │ [Actions ▼] │  │
│  │ ...                                                         │
│  └────────────────────────────────────────────────────────────┤
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### Components

#### 1. Welcome Message
- **Location**: Top of page, below header
- **Content**: "Welcome, [User Name]!" with personalized greeting
- **Behavior**: Shows time-based greeting (Good morning/afternoon/evening)

#### 2. Metric Cards
- **Count**: 3 cards (Clients, External Users, Libraries)
- **Content**: 
  - Large number showing total count
  - Small trend indicator (e.g., "+2 this week")
  - Icon representing the metric
- **Interaction**: Clickable, navigates to respective section
- **Visual**: Card with shadow, hover effect

#### 3. Recent Activity Panel
- **Location**: Left side, below metrics
- **Content**: 
  - Last 5 activities (user invitations, access revocations, library creations)
  - Timestamp for each activity
  - User avatar/initials
- **Interaction**: Click activity to view details
- **Visual**: List with icons, relative timestamps ("2 hours ago")

#### 4. Quick Actions Panel
- **Location**: Right side, below metrics
- **Content**:
  - "New Client" button
  - "Invite User" button
  - "Search" button
  - "View All Clients" link
- **Interaction**: Primary actions with clear CTAs
- **Visual**: Prominent buttons with icons

#### 5. Client Spaces Table
- **Location**: Bottom of page
- **Columns**:
  - Client name (clickable)
  - External user count
  - Library count
  - Status badge (Active/Inactive/Archived)
  - Actions dropdown
- **Features**:
  - Sortable columns
  - Pagination (10/20/50 per page)
  - Search/filter bar
- **Actions**:
  - View details
  - Edit client
  - Archive/Delete
  - Open SharePoint site

### User Flows

#### Primary Flow: View Overview
1. User logs in → Dashboard loads
2. User sees metrics at a glance
3. User scans recent activity
4. User can take quick actions or drill down

#### Secondary Flow: Quick Client Creation
1. User clicks "New Client" from Quick Actions
2. Modal/form opens
3. User fills in client details
4. User clicks "Create"
5. Dashboard refreshes with new client

### Keyboard Shortcuts
- `Ctrl+K` / `Cmd+K`: Open command palette
- `/`: Focus search
- `N`: New client (when command palette closed)
- `Arrow keys`: Navigate tables

### Accessibility
- All metrics have `aria-label` descriptors
- Table headers have proper `scope` attributes
- Focus indicators visible on all interactive elements
- Screen reader announcements for activity updates

---

## Client Management

### Clients List Screen

#### Layout

```
┌────────────────────────────────────────────────────────────────┐
│  Header: Clients                                               │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  ┌──────────────────┐  [Search clients...]  [+ New Client]   │
│  │ Filters ▼        │                                          │
│  │ Status: All      │                                          │
│  │ Sort: Name (A-Z) │                                          │
│  └──────────────────┘                                          │
│                                                                │
│  ┌────────────────────────────────────────────────────────────┤
│  │ Name       │ Site URL      │ Users│ Libs│ Status │Actions││
│  ├────────────────────────────────────────────────────────────┤
│  │ ABC Corp   │ /sites/ABC... │  5   │  3  │ Active │ ⋮     ││
│  │ XYZ Inc    │ /sites/XYZ... │  3   │  2  │ Active │ ⋮     ││
│  │ 123 Ltd    │ /sites/123... │  1   │  1  │ Inactive│ ⋮     ││
│  └────────────────────────────────────────────────────────────┤
│                                                                │
│  Showing 1-10 of 15       [< Prev] Page 1 of 2 [Next >]      │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

#### Components

1. **Filter Panel**
   - Status filter (All/Active/Inactive/Archived)
   - Sort options (Name, Date created, User count)
   - Search by name or URL

2. **New Client Button**
   - Primary action button
   - Opens creation modal/form

3. **Client Table**
   - Columns: Name, Site URL (truncated), User count, Library count, Status, Actions
   - Clickable rows (navigates to detail)
   - Actions dropdown (Edit, Archive, Delete, Open site)

4. **Pagination**
   - Page size selector (10/20/50)
   - Previous/Next buttons
   - Page indicator

### Client Detail Screen

#### Layout

```
┌────────────────────────────────────────────────────────────────┐
│  ← Back to Clients    ABC Corporation                         │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │ Client Information                           [Edit]      │  │
│  │                                                           │  │
│  │ Name: ABC Corporation                                    │  │
│  │ Site URL: https://contoso.sharepoint.com/sites/ABC...   │  │
│  │ Status: Active                                           │  │
│  │ Created: Jan 20, 2024                                    │  │
│  │ Primary Contact: john.smith@abccorp.com                  │  │
│  └─────────────────────────────────────────────────────────┘  │
│                                                                │
│  Tabs: [External Users] [Libraries] [Lists] [Activity]       │
│                                                                │
│  External Users (5)                         [+ Invite User]  │
│  ┌────────────────────────────────────────────────────────────┤
│  │ Name         │ Email            │ Status  │ Libraries     ││
│  │ Jane Doe     │ jane@example.com │ Active  │ Documents (R) ││
│  │ John Smith   │ john@example.com │ Pending │ Contracts (E) ││
│  └────────────────────────────────────────────────────────────┤
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

#### Components

1. **Header**
   - Back button with breadcrumb
   - Client name
   - Actions dropdown (Edit, Archive, Delete)

2. **Client Info Card**
   - Display-only fields (editable via Edit button)
   - Badge for status
   - Link to open SharePoint site

3. **Tab Navigation**
   - External Users
   - Libraries
   - Lists  
   - Activity (audit log)

4. **Tab Content**
   - Table/list of resources
   - Relevant actions (Invite, Create, etc.)

### Create/Edit Client Modal

#### Layout

```
┌─────────────────────────────────────────────┐
│  Create New Client Space              [×]   │
├─────────────────────────────────────────────┤
│                                             │
│  Client Name *                              │
│  [_____________________________________]    │
│                                             │
│  Description (optional)                     │
│  [_____________________________________]    │
│  [_____________________________________]    │
│                                             │
│  Primary Contact Email *                    │
│  [_____________________________________]    │
│                                             │
│  SharePoint Site URL *                      │
│  https://.../sites/[ABC-Corporation___]    │
│  ✓ Available                                │
│                                             │
│  Site Template                              │
│  ○ Team Site (recommended)                  │
│  ○ Communication Site                       │
│                                             │
│  [Cancel]              [Create Client]      │
│                                             │
└─────────────────────────────────────────────┘
```

#### Validation
- **Client Name**: Required, 3-100 chars, no special chars
- **Site URL**: Required, unique, auto-generated from name
- **Email**: Required, valid email format

#### Behavior
- Real-time validation with inline error messages
- Site URL auto-generates as user types name
- Check availability with debounce (500ms)
- Success: Modal closes, toast notification, redirects to client detail

---

## External User Management

### Users List Screen

#### Layout

```
┌────────────────────────────────────────────────────────────────┐
│  Header: External Users                                        │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  ┌──────────────────┐  [Search users...]   [+ Invite User]   │
│  │ Filters ▼        │                       [↑ Bulk Invite]   │
│  │ Status: All      │                                          │
│  │ Client: All      │                                          │
│  │ Company: All     │                                          │
│  └──────────────────┘                                          │
│                                                                │
│  ┌────────────────────────────────────────────────────────────┤
│  │ Name      │ Email        │ Company  │ Status │ Client     ││
│  ├────────────────────────────────────────────────────────────┤
│  │ Jane Doe  │ jane@ex...   │ Ex Corp  │ Active │ ABC Corp   ││
│  │ John Doe  │ john@ex...   │ Ex Corp  │ Pending│ XYZ Inc    ││
│  │ Bob Smith │ bob@other... │ Other Co │ Active │ ABC Corp   ││
│  └────────────────────────────────────────────────────────────┤
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### Invite User Modal

#### Layout

```
┌─────────────────────────────────────────────┐
│  Invite External User                 [×]   │
├─────────────────────────────────────────────┤
│                                             │
│  Email Address *                            │
│  [_____________________________________]    │
│                                             │
│  Display Name                               │
│  [_____________________________________]    │
│  (auto-populated if user found)             │
│                                             │
│  Company                                    │
│  [_____________________________________]    │
│                                             │
│  Project/Matter                             │
│  [_____________________________________]    │
│                                             │
│  Grant Access To:                           │
│  ☑ Documents (Library)                      │
│    Permission: [Read ▼]                     │
│  ☑ Contracts (Library)                      │
│    Permission: [Edit ▼]                     │
│  ☐ Archive (Library)                        │
│                                             │
│  Personal Message (optional)                │
│  [_____________________________________]    │
│  [_____________________________________]    │
│  [_____________________________________]    │
│                                             │
│  ☑ Send email invitation                    │
│                                             │
│  [Cancel]              [Send Invitation]    │
│                                             │
└─────────────────────────────────────────────┘
```

#### Validation
- **Email**: Required, valid format, not already invited
- **Libraries**: At least one must be selected
- **Permission**: Required for each selected library

#### Behavior
- Auto-complete for email (if user exists in directory)
- Library checkboxes dynamically loaded from client
- Permission dropdown for each selected library
- Preview invitation email (optional)
- Success: Modal closes, user added to list, email sent

### Bulk Invite Screen

#### Layout

```
┌────────────────────────────────────────────────────────────────┐
│  ← Back to Users    Bulk Invite External Users                │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  Step 1: Upload CSV File                                       │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Drag & drop CSV file here or [Browse Files]          │  │
│  │                                                         │  │
│  │  Download template: [sample-bulk-invite.csv]           │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                                │
│  Step 2: Map Columns                                           │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ CSV Column      → ClientSpace Field                    │  │
│  │ Email           → Email Address (required)             │  │
│  │ Name            → Display Name                         │  │
│  │ Company         → Company                              │  │
│  │ Project         → Project/Matter                       │  │
│  │ Libraries       → Libraries (comma-separated)          │  │
│  │ Permission      → Permission Level                     │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                                │
│  Step 3: Preview & Confirm                                     │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Ready to invite 25 users                               │  │
│  │                                                         │  │
│  │ ✓ 25 valid entries                                     │  │
│  │ ⚠ 2 warnings (missing company)                         │  │
│  │ ✗ 1 error (invalid email: bad@)                        │  │
│  │                                                         │  │
│  │ [View Details]                                         │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                                │
│  [Cancel]              [Invite All Users]                     │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

#### CSV Format
```csv
Email,DisplayName,Company,Project,Libraries,Permission
jane@example.com,Jane Doe,Example Corp,Project A,Documents;Contracts,Edit
john@other.com,John Smith,Other Inc,Project B,Documents,Read
```

#### Validation
- File size < 5MB
- Valid CSV format
- Required columns present
- Email format validation
- Duplicate detection

#### Behavior
- Real-time validation during upload
- Show summary of valid/invalid entries
- Allow user to fix errors before submitting
- Progress bar during processing
- Email report when complete

---

## Library Management

### Libraries List

#### Layout

```
┌────────────────────────────────────────────────────────────────┐
│  Client: ABC Corporation  →  Libraries                        │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  [Search libraries...]                      [+ New Library]   │
│                                                                │
│  ┌────────────────────────────────────────────────────────────┤
│  │ Name       │ Items │ Size    │ Ext Users │ Actions        ││
│  ├────────────────────────────────────────────────────────────┤
│  │ Documents  │ 45    │ 125 MB  │ 3         │ ⋮ Manage       ││
│  │ Contracts  │ 12    │ 45 MB   │ 2         │ ⋮ Permissions  ││
│  │ Archive    │ 103   │ 350 MB  │ 0         │ ⋮ Settings     ││
│  └────────────────────────────────────────────────────────────┤
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### Create Library Modal

#### Layout

```
┌─────────────────────────────────────────────┐
│  Create New Library                   [×]   │
├─────────────────────────────────────────────┤
│                                             │
│  Library Name *                             │
│  [_____________________________________]    │
│                                             │
│  Description (optional)                     │
│  [_____________________________________]    │
│  [_____________________________________]    │
│                                             │
│  Template                                   │
│  ○ Document Library (recommended)           │
│  ○ Custom List                              │
│  ○ Picture Library                          │
│                                             │
│  Settings                                   │
│  ☑ Enable versioning                        │
│  ☐ Require check-out                        │
│  ☐ Require content approval                 │
│                                             │
│  [Cancel]              [Create Library]     │
│                                             │
└─────────────────────────────────────────────┘
```

#### Validation
- **Name**: Required, 1-100 chars, no special chars except space and hyphen
- **Template**: Required

#### Behavior
- Name validation in real-time
- Settings options context help (tooltip)
- Success: Redirect to library in SharePoint (new tab) or stay in portal

---

## Search

### Global Search Screen

#### Layout

```
┌────────────────────────────────────────────────────────────────┐
│  Header: Search                                                │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  [🔍 Search across all clients...____________] [Search]       │
│                                                                │
│  ┌──────────────────┐                                          │
│  │ Filters          │   Results (127)                          │
│  │                  │                                          │
│  │ Type:            │   ┌──────────────────────────────────┐  │
│  │ ☑ Documents (85) │   │ 📄 Master Agreement.docx         │  │
│  │ ☑ Users (32)     │   │ ABC Corporation > Contracts      │  │
│  │ ☑ Clients (10)   │   │ Modified: Feb 15, 2024 by jane@  │  │
│  │                  │   │ ...terms and conditions...       │  │
│  │ Client:          │   └──────────────────────────────────┘  │
│  │ ☑ All            │                                          │
│  │ ☐ ABC Corp (52)  │   ┌──────────────────────────────────┐  │
│  │ ☐ XYZ Inc (43)   │   │ 📄 Service Agreement.pdf         │  │
│  │                  │   │ XYZ Inc > Documents              │  │
│  │ Modified:        │   │ Modified: Feb 10, 2024 by john@  │  │
│  │ ○ Any time       │   │ ...payment terms...              │  │
│  │ ○ Past week      │   └──────────────────────────────────┘  │
│  │ ○ Past month     │                                          │
│  │ ○ Past year      │   [Load More Results]                   │
│  │                  │                                          │
│  └──────────────────┘                                          │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

#### Components

1. **Search Bar**
   - Auto-complete suggestions
   - Search history (recent searches)
   - Clear button

2. **Filter Panel**
   - Type filters with counts
   - Client filters with counts
   - Date range filter
   - Apply/Clear buttons

3. **Results List**
   - Document icon/preview
   - Document title (clickable)
   - Breadcrumb (Client > Library)
   - Metadata (modified date, user)
   - Snippet with search term highlighted
   - Pagination (infinite scroll or Load More)

#### Behavior
- Real-time search as user types (debounced 300ms)
- Highlight search terms in results
- Click result to open in SharePoint (new tab)
- Keyboard navigation (arrow keys, Enter to open)
- Empty state: "No results found. Try different search terms."

### Client-Scoped Search

Same layout as global search, but:
- Pre-filtered to specific client
- Breadcrumb shows client name
- No client filter in sidebar

---

## Subscription & Billing

### Subscription Overview Screen

#### Layout

```
┌────────────────────────────────────────────────────────────────┐
│  Header: Subscription & Billing                                │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  Current Plan: Professional                                    │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │  Professional Plan - $99/month                          │  │
│  │                                                          │  │
│  │  Status: Active                                         │  │
│  │  Next billing date: March 1, 2024                       │  │
│  │  Amount: $99.00 USD                                     │  │
│  │                                                          │  │
│  │  [Manage Payment Method]  [View Invoices]  [Cancel]    │  │
│  └─────────────────────────────────────────────────────────┘  │
│                                                                │
│  Usage (Current Month)                                         │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │  Client Spaces: 15 / 50      ████████░░░░░░░░░░ 30%    │  │
│  │  External Users: 120 / 500   ████████████░░░░░░ 24%    │  │
│  │  Storage: 25GB / 100GB       ██████░░░░░░░░░░░░ 25%    │  │
│  │  API Calls: 15,420 / 50,000  ████░░░░░░░░░░░░░░ 31%    │  │
│  └─────────────────────────────────────────────────────────┘  │
│                                                                │
│  Available Plans                                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                    │
│  │ Free     │  │ Pro ✓    │  │ Enter.   │                    │
│  │ $0/mo    │  │ $99/mo   │  │ $299/mo  │                    │
│  │          │  │          │  │          │                    │
│  │ 3 clients│  │ 50 clients│ │ Unlimited│                    │
│  │ 10 users │  │ 500 users│  │ Unlimited│                    │
│  │ 10GB     │  │ 100GB    │  │ 1TB      │                    │
│  │          │  │          │  │          │                    │
│  │ [Select] │  │ Current  │  │ [Upgrade]│                    │
│  └──────────┘  └──────────┘  └──────────┘                    │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

#### Components

1. **Current Plan Card**
   - Plan name and price
   - Status badge (Active/Cancelled/Trial)
   - Next billing date
   - Actions (Manage, View invoices, Cancel)

2. **Usage Panel**
   - Progress bars for each metric
   - Current vs. limit with percentage
   - Color-coded (green < 70%, yellow 70-90%, red > 90%)

3. **Plans Comparison**
   - Cards for each plan
   - Current plan highlighted
   - Feature comparison
   - CTA button (Select, Upgrade, Contact Sales)

### Upgrade Flow

#### Layout

```
┌─────────────────────────────────────────────┐
│  Upgrade to Enterprise                [×]   │
├─────────────────────────────────────────────┤
│                                             │
│  Enterprise Plan - $299/month               │
│                                             │
│  Features included:                         │
│  ✓ Unlimited client spaces                  │
│  ✓ Unlimited external users                 │
│  ✓ 1TB storage                              │
│  ✓ Priority support                         │
│  ✓ Custom branding                          │
│  ✓ Advanced analytics                       │
│                                             │
│  Billing:                                   │
│  ○ Monthly ($299/month)                     │
│  ○ Annual ($2,990/year - Save $598!)       │
│                                             │
│  Payment will be processed immediately.     │
│  Your current plan will be prorated.        │
│                                             │
│  [Cancel]              [Proceed to Payment] │
│                                             │
└─────────────────────────────────────────────┘
```

#### Behavior
- Clicking "Proceed to Payment" redirects to Stripe Checkout
- After payment, redirect back to portal with success message
- Subscription updated immediately
- Email confirmation sent

---

## Settings

### Settings Screen

#### Layout

```
┌────────────────────────────────────────────────────────────────┐
│  Header: Settings                                              │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  Tabs: [General] [Users] [Security] [Integrations] [Audit]   │
│                                                                │
│  General Settings                                              │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │  Organization Name *                                    │  │
│  │  [Acme Law Firm__________________________]             │  │
│  │                                                          │  │
│  │  Primary Domain                                         │  │
│  │  [acmelaw.com________________________]                 │  │
│  │                                                          │  │
│  │  Time Zone                                              │  │
│  │  [(UTC) Dublin, Edinburgh, Lisbon, London__]           │  │
│  │                                                          │  │
│  │  Language                                               │  │
│  │  [English (UK)_________________________]               │  │
│  │                                                          │  │
│  │  Default SharePoint Template                            │  │
│  │  ○ Team Site                                            │  │
│  │  ○ Communication Site                                   │  │
│  │                                                          │  │
│  │  [Save Changes]                                         │  │
│  └─────────────────────────────────────────────────────────┘  │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### Users Tab

#### Layout

```
│  Users & Permissions                        [+ Add User]      │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │ Name          │ Email             │ Role    │ Status    ││  │
│  │ John Admin    │ admin@tenant.com  │ Admin   │ Active    ││  │
│  │ Jane User     │ jane@tenant.com   │ User    │ Active    ││  │
│  └─────────────────────────────────────────────────────────┘  │
│                                                                │
│  Roles:                                                        │
│  • Admin: Full access to all features                         │
│  • User: Can manage assigned clients only                     │
```

### Security Tab

#### Layout

```
│  Security Settings                                             │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │  Multi-Factor Authentication                            │  │
│  │  ☑ Require MFA for administrators                       │  │
│  │  ☐ Require MFA for all users                            │  │
│  │                                                          │  │
│  │  Session Timeout                                        │  │
│  │  [30 minutes____________▼]                             │  │
│  │                                                          │  │
│  │  External Sharing Policy                                │  │
│  │  ○ Allow external sharing (recommended)                 │  │
│  │  ○ Disable external sharing                             │  │
│  │                                                          │  │
│  │  IP Restrictions (Enterprise only)                      │  │
│  │  [Upgrade to enable__________________________]         │  │
│  │                                                          │  │
│  │  [Save Changes]                                         │  │
│  └─────────────────────────────────────────────────────────┘  │
```

---

## AI Chat Assistant

### Chat Widget

#### Layout

```
┌─────────────────────────────────┐
│  ClientSpace Assistant    [─][×]│
├─────────────────────────────────┤
│                                 │
│  👋 Hello! How can I help?      │
│                                 │
│  💬 User: How do I invite a     │
│           user to a client?     │
│                                 │
│  🤖 Assistant: To invite an     │
│     external user:              │
│     1. Go to the client page    │
│     2. Click "Invite User"      │
│     3. Enter their email        │
│     4. Select libraries         │
│     5. Click "Send Invitation"  │
│                                 │
│     [View detailed guide]       │
│                                 │
│  ─────────────────────────────  │
│  [Type your message...] [Send]  │
└─────────────────────────────────┘
```

#### Features
- Floating widget (bottom right)
- Minimize/maximize
- Context-aware (knows current page)
- Quick actions (links to relevant docs)
- Conversation history (session-based)
- Typing indicator
- Copy responses

#### Behavior
- Opens via click or `Ctrl+Shift+A` / `Cmd+Shift+A`
- Auto-suggest common questions
- Remembers conversation context
- Can perform actions (e.g., "Create client ABC Corp")

---

## Navigation & Common Elements

### Header Navigation

```
┌────────────────────────────────────────────────────────────────┐
│  [☰] ClientSpace    Dashboard  Clients  Users  Search          │
│                                                 [🔔] [👤] [⚙️]  │
└────────────────────────────────────────────────────────────────┘
```

#### Components
- **Menu toggle**: Opens sidebar on mobile
- **Logo/Brand**: Returns to dashboard
- **Main nav**: Dashboard, Clients, Users, Search
- **Notifications**: Bell icon with badge (unread count)
- **Profile**: Avatar/initials, dropdown menu
- **Settings**: Gear icon, quick settings access

### Profile Dropdown

```
┌──────────────────────┐
│  John Admin          │
│  admin@tenant.com    │
├──────────────────────┤
│  👤 My Profile       │
│  🏢 Organization     │
│  💳 Subscription     │
│  ⚙️  Settings        │
│  📚 Documentation    │
│  🆘 Support          │
├──────────────────────┤
│  🚪 Sign Out         │
└──────────────────────┘
```

### Toast Notifications

```
┌────────────────────────────────┐
│  ✅ Client created successfully│
│  [View]               [×]      │
└────────────────────────────────┘
```

**Types**:
- Success (green): ✅
- Error (red): ❌
- Warning (yellow): ⚠️
- Info (blue): ℹ️

**Behavior**:
- Auto-dismiss after 5 seconds
- User can dismiss manually
- Multiple toasts stack vertically
- Action button (optional)

### Loading States

**Full page loader**:
```
┌────────────────────────────────────────┐
│                                        │
│           ⏳ Loading...                │
│                                        │
└────────────────────────────────────────┘
```

**Skeleton loaders**:
```
┌────────────────────────────────────────┐
│  ▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░         │
│  ▓▓▓▓▓▓░░░░░░░░░░░░░░░░                │
│  ▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░             │
└────────────────────────────────────────┘
```

### Empty States

```
┌────────────────────────────────────────┐
│                                        │
│          📁                            │
│      No clients yet                    │
│                                        │
│  Get started by creating your first    │
│  client space.                         │
│                                        │
│      [+ Create Client]                 │
│                                        │
└────────────────────────────────────────┘
```

### Error States

```
┌────────────────────────────────────────┐
│                                        │
│          ❌                            │
│  Oops! Something went wrong            │
│                                        │
│  We couldn't load your clients.        │
│  Please try again.                     │
│                                        │
│      [Try Again]  [Contact Support]    │
│                                        │
└────────────────────────────────────────┘
```

---

## Responsive Design

### Breakpoints

- **Mobile**: < 768px
- **Tablet**: 768px - 1024px
- **Desktop**: > 1024px

### Mobile Adaptations

1. **Navigation**: Hamburger menu, collapsible sidebar
2. **Tables**: Card view instead of table
3. **Filters**: Bottom sheet instead of sidebar
4. **Actions**: Bottom action bar
5. **Forms**: Full-width, stacked inputs

---

## Accessibility Guidelines

### WCAG 2.1 AA Compliance

- **Color Contrast**: Minimum 4.5:1 for normal text, 3:1 for large text
- **Keyboard Navigation**: All interactive elements keyboard accessible
- **Screen Readers**: Proper ARIA labels and roles
- **Focus Indicators**: Visible focus outlines on all interactive elements
- **Alt Text**: All images and icons have descriptive alt text

### Testing

- Test with keyboard only (no mouse)
- Test with screen reader (NVDA, JAWS, VoiceOver)
- Test with browser zoom at 200%
- Test with high contrast mode
- Test with color blindness simulators

---

## Performance Guidelines

### Loading Time Targets

- **Initial page load**: < 2 seconds
- **Page transitions**: < 500ms
- **API responses**: < 1 second
- **Search results**: < 2 seconds

### Optimization Techniques

- Lazy load images and components
- Paginate large lists
- Debounce search inputs
- Cache API responses
- Use CDN for static assets
- Minimize JavaScript bundle size

---

## Additional Resources

- **[User Guide](USER_GUIDE.md)**: Complete feature documentation
- **[Quick Start Guide](MVP_QUICK_START.md)**: Getting started
- **[API Reference](MVP_API_REFERENCE.md)**: API documentation
- **[Branding Guide](branding/README.md)**: Design system and assets

---

*Last Updated: February 2026*  
*Version: MVP 1.0*  
*UX Version: 1.0*

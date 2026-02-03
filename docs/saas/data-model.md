# Data Model Documentation

## Overview

This document defines the complete data model for the SharePoint External User Manager SaaS platform, including database schemas, relationships, and data flow patterns.

## Multi-Tenant Data Architecture

### Strategy
- **Primary**: Azure SQL Database (one per tenant)
- **Shared Metadata**: Azure Cosmos DB (partitioned by tenantId)
- **Blob Storage**: Azure Storage Account for files/exports

### Tenant Isolation
Every tenant gets an isolated SQL database with the naming pattern:
```
sp_external_user_mgr_{sanitized_tenant_id}
```

## Cosmos DB Schema (Shared System Data)

### Container: Tenants

**Partition Key**: `/tenantId`

```json
{
  "id": "tenant-guid-123",
  "tenantId": "contoso.onmicrosoft.com",
  "displayName": "Contoso Ltd",
  "status": "active",
  "subscriptionTier": "pro",
  "subscriptionStatus": "active",
  "trialEndDate": "2024-06-30T23:59:59Z",
  "billingEmail": "billing@contoso.com",
  "adminEmail": "admin@contoso.com",
  "createdDate": "2024-01-15T10:00:00Z",
  "lastModifiedDate": "2024-01-20T14:30:00Z",
  "settings": {
    "apiBaseUrl": "https://api.spexternal.com",
    "webhookUrl": null,
    "features": {
      "auditExport": true,
      "bulkOperations": true,
      "advancedReporting": true,
      "customPolicies": false
    }
  },
  "azureAdAppId": "app-guid-456",
  "onboardingCompleted": true,
  "dataLocation": "eastus"
}
```

### Container: Subscriptions

**Partition Key**: `/tenantId`

```json
{
  "id": "sub-guid-789",
  "tenantId": "contoso.onmicrosoft.com",
  "tier": "pro",
  "status": "active",
  "startDate": "2024-01-15T00:00:00Z",
  "endDate": "2025-01-15T00:00:00Z",
  "autoRenew": true,
  "billingCycle": "annual",
  "pricing": {
    "amount": 999.00,
    "currency": "USD",
    "perSeat": false
  },
  "limits": {
    "maxExternalUsers": 500,
    "maxLibraries": 100,
    "apiCallsPerMonth": 100000,
    "auditRetentionDays": 365,
    "maxAdmins": 10
  },
  "usage": {
    "externalUsersCount": 127,
    "librariesCount": 23,
    "apiCallsThisMonth": 12450,
    "storageUsedMB": 250
  },
  "paymentMethod": "invoice",
  "marketplaceIntegration": {
    "enabled": false,
    "marketplaceSubscriptionId": null,
    "offerName": null
  },
  "createdDate": "2024-01-15T10:05:00Z",
  "lastModifiedDate": "2024-01-20T14:30:00Z"
}
```

### Container: GlobalAuditLogs

**Partition Key**: `/tenantId`

```json
{
  "id": "audit-guid-101",
  "tenantId": "contoso.onmicrosoft.com",
  "timestamp": "2024-01-20T14:35:22Z",
  "eventType": "user.invite",
  "actor": {
    "userId": "user-guid-202",
    "email": "admin@contoso.com",
    "displayName": "John Admin"
  },
  "target": {
    "resourceType": "externalUser",
    "resourceId": "extuser-guid-303",
    "email": "partner@external.com"
  },
  "action": "invite",
  "status": "success",
  "metadata": {
    "libraryId": "lib-guid-404",
    "libraryName": "Partner Documents",
    "permissions": "read",
    "ipAddress": "203.0.113.45",
    "userAgent": "Mozilla/5.0...",
    "correlationId": "corr-guid-505"
  },
  "changes": {
    "before": null,
    "after": {
      "status": "invited",
      "permissions": "read"
    }
  }
}
```

### Container: UsageMetrics

**Partition Key**: `/tenantId`

```json
{
  "id": "metrics-2024-01-20",
  "tenantId": "contoso.onmicrosoft.com",
  "date": "2024-01-20",
  "metrics": {
    "apiCalls": {
      "total": 450,
      "byEndpoint": {
        "/external-users": 200,
        "/policies": 50,
        "/audit": 150,
        "/tenants/me": 50
      }
    },
    "activeUsers": {
      "internalAdmins": 3,
      "externalUsers": 127
    },
    "operations": {
      "userInvites": 12,
      "userRemovals": 3,
      "policyUpdates": 2
    },
    "performance": {
      "avgResponseTimeMs": 145,
      "p95ResponseTimeMs": 320,
      "errorRate": 0.02
    }
  },
  "timestamp": "2024-01-20T23:59:59Z"
}
```

## Azure SQL Schema (Per-Tenant Data)

### Table: ExternalUsers

```sql
CREATE TABLE ExternalUsers (
    UserId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(255),
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    Company NVARCHAR(255),
    Project NVARCHAR(255),
    Department NVARCHAR(100),
    InvitedBy NVARCHAR(255) NOT NULL,
    InvitedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastAccessDate DATETIME2,
    Status NVARCHAR(50) NOT NULL, -- 'invited', 'active', 'expired', 'revoked'
    ExpirationDate DATETIME2,
    AzureAdGuestId UNIQUEIDENTIFIER,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT CK_Status CHECK (Status IN ('invited', 'active', 'expired', 'revoked')),
    INDEX IX_Email (Email),
    INDEX IX_Company (Company),
    INDEX IX_Project (Project),
    INDEX IX_Status (Status),
    INDEX IX_InvitedDate (InvitedDate)
);
```

### Table: UserLibraryAccess

```sql
CREATE TABLE UserLibraryAccess (
    AccessId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    LibraryId UNIQUEIDENTIFIER NOT NULL,
    PermissionLevel NVARCHAR(50) NOT NULL, -- 'read', 'contribute', 'fullcontrol'
    GrantedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    GrantedBy NVARCHAR(255) NOT NULL,
    ExpirationDate DATETIME2,
    Status NVARCHAR(50) NOT NULL, -- 'active', 'expired', 'revoked'
    SharePointPermissionId NVARCHAR(255),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (UserId) REFERENCES ExternalUsers(UserId),
    FOREIGN KEY (LibraryId) REFERENCES SharePointLibraries(LibraryId),
    CONSTRAINT CK_PermissionLevel CHECK (PermissionLevel IN ('read', 'contribute', 'fullcontrol')),
    CONSTRAINT CK_AccessStatus CHECK (Status IN ('active', 'expired', 'revoked')),
    INDEX IX_UserId (UserId),
    INDEX IX_LibraryId (LibraryId),
    INDEX IX_Status (Status)
);
```

### Table: SharePointLibraries

```sql
CREATE TABLE SharePointLibraries (
    LibraryId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SiteUrl NVARCHAR(500) NOT NULL,
    LibraryName NVARCHAR(255) NOT NULL,
    LibraryUrl NVARCHAR(500) NOT NULL,
    Description NVARCHAR(1000),
    Owner NVARCHAR(255) NOT NULL,
    SharePointSiteId UNIQUEIDENTIFIER,
    SharePointLibraryId UNIQUEIDENTIFIER,
    ExternalSharingEnabled BIT NOT NULL DEFAULT 1,
    RequireApproval BIT NOT NULL DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastSyncDate DATETIME2,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_SiteUrl (SiteUrl),
    INDEX IX_Owner (Owner),
    UNIQUE INDEX UX_LibraryUrl (LibraryUrl)
);
```

### Table: UserInvitations

```sql
CREATE TABLE UserInvitations (
    InvitationId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER,
    Email NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(255),
    LibraryId UNIQUEIDENTIFIER NOT NULL,
    PermissionLevel NVARCHAR(50) NOT NULL,
    InvitedBy NVARCHAR(255) NOT NULL,
    InvitedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Status NVARCHAR(50) NOT NULL, -- 'pending', 'accepted', 'expired', 'cancelled'
    InvitationMessage NVARCHAR(2000),
    ExpirationDate DATETIME2,
    AcceptedDate DATETIME2,
    SharePointInvitationId NVARCHAR(255),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (LibraryId) REFERENCES SharePointLibraries(LibraryId),
    CONSTRAINT CK_InvitationStatus CHECK (Status IN ('pending', 'accepted', 'expired', 'cancelled')),
    INDEX IX_Email (Email),
    INDEX IX_Status (Status),
    INDEX IX_InvitedDate (InvitedDate)
);
```

### Table: AuditLogs

```sql
CREATE TABLE AuditLogs (
    AuditId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    EventType NVARCHAR(100) NOT NULL,
    Actor NVARCHAR(255) NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    ResourceType NVARCHAR(100),
    ResourceId UNIQUEIDENTIFIER,
    Status NVARCHAR(50) NOT NULL,
    Details NVARCHAR(MAX), -- JSON
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    CorrelationId UNIQUEIDENTIFIER,
    
    INDEX IX_Timestamp (Timestamp),
    INDEX IX_EventType (EventType),
    INDEX IX_Actor (Actor),
    INDEX IX_CorrelationId (CorrelationId)
);
```

### Table: TenantPolicies

```sql
CREATE TABLE TenantPolicies (
    PolicyId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PolicyName NVARCHAR(100) NOT NULL,
    PolicyType NVARCHAR(50) NOT NULL, -- 'expiration', 'approval', 'restriction'
    IsEnabled BIT NOT NULL DEFAULT 1,
    Configuration NVARCHAR(MAX), -- JSON
    CreatedBy NVARCHAR(255) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(255),
    ModifiedDate DATETIME2,
    
    INDEX IX_PolicyType (PolicyType),
    UNIQUE INDEX UX_PolicyName (PolicyName)
);
```

### Table: TenantConfiguration

```sql
CREATE TABLE TenantConfiguration (
    ConfigId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ConfigKey NVARCHAR(100) NOT NULL,
    ConfigValue NVARCHAR(MAX),
    DataType NVARCHAR(50) NOT NULL, -- 'string', 'number', 'boolean', 'json'
    Description NVARCHAR(500),
    IsEditable BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    UNIQUE INDEX UX_ConfigKey (ConfigKey)
);
```

## Entity Relationships

```
┌─────────────────────┐
│  ExternalUsers      │
│  - UserId (PK)      │
│  - Email            │
│  - Company          │
│  - Project          │
└──────────┬──────────┘
           │
           │ 1:N
           │
           ▼
┌─────────────────────┐      N:1      ┌─────────────────────┐
│ UserLibraryAccess   │◄──────────────│ SharePointLibraries │
│ - AccessId (PK)     │               │ - LibraryId (PK)    │
│ - UserId (FK)       │               │ - LibraryName       │
│ - LibraryId (FK)    │               │ - SiteUrl           │
│ - PermissionLevel   │               └─────────────────────┘
└─────────────────────┘                         │
                                                │ 1:N
                                                │
                                                ▼
                                       ┌─────────────────────┐
                                       │ UserInvitations     │
                                       │ - InvitationId (PK) │
                                       │ - LibraryId (FK)    │
                                       │ - Email             │
                                       │ - Status            │
                                       └─────────────────────┘
```

## Data Flow Patterns

### 1. User Invitation Flow
```
1. API Request → POST /external-users/invite
2. Create UserInvitations record (status: pending)
3. Call Microsoft Graph API (send invitation)
4. Update UserInvitations (SharePointInvitationId)
5. Create AuditLog entry
6. Return invitation details
```

### 2. User Access Query Flow
```
1. API Request → GET /external-users
2. Query ExternalUsers table (tenant DB)
3. Join with UserLibraryAccess
4. Optionally query Graph API for live status
5. Cache results (5 min TTL)
6. Create AuditLog entry (if configured)
7. Return user list
```

### 3. Subscription Check Flow
```
1. Middleware intercepts API request
2. Extract tenantId from JWT
3. Query Cosmos DB Subscriptions container
4. Check subscription.status === 'active'
5. Check subscription.limits vs actual usage
6. Allow/Deny request
7. Log usage metrics
```

## Data Retention Policies

| Data Type | Retention Period | Action After Expiry |
|-----------|-----------------|---------------------|
| Active External Users | While access is active | Mark as deleted (soft delete) |
| Revoked External Users | 90 days | Archive to blob storage |
| Audit Logs (Operational) | Subscription-dependent | Move to cold storage |
| Audit Logs (Compliance) | 7 years | Move to cold storage |
| Invitation Records | 1 year | Archive to blob storage |
| Usage Metrics | 2 years | Aggregate and archive |
| Tenant Configuration | While tenant is active | N/A |

## Indexing Strategy

### High-Priority Indexes
```sql
-- Most frequent queries
CREATE INDEX IX_ExternalUsers_Status_InvitedDate 
    ON ExternalUsers(Status, InvitedDate DESC);

CREATE INDEX IX_AuditLogs_Timestamp_EventType 
    ON AuditLogs(Timestamp DESC, EventType);

-- Covering indexes for common queries
CREATE INDEX IX_UserLibraryAccess_UserId_Status 
    ON UserLibraryAccess(UserId, Status) 
    INCLUDE (LibraryId, PermissionLevel, GrantedDate);
```

## Data Migration Scripts

### Initial Schema Setup
```sql
-- See /backend/migrations/001_initial_schema.sql
-- Creates all tables with constraints
-- Inserts default configuration
```

### Tenant Provisioning
```sql
-- See /backend/migrations/provision_tenant_database.sql
-- Creates new database for tenant
-- Runs all migration scripts
-- Initializes default data
```

## GDPR Compliance

### Data Subject Rights
1. **Right to Access**: Export all user data via API
2. **Right to Erasure**: Hard delete user records and cascade
3. **Right to Portability**: JSON/CSV export of user data
4. **Right to Rectification**: Update user information via API

### Implementation
```typescript
// Example: Delete user and all related data
async function deleteUserData(userId: string): Promise<void> {
    await deleteUserLibraryAccess(userId);
    await deleteUserInvitations(userId);
    await deleteAuditLogsForUser(userId);
    await deleteExternalUser(userId);
    await logDataDeletionEvent(userId);
}
```

## Performance Considerations

### Query Optimization
- Use pagination for all list queries (max 100 items per page)
- Implement read replicas for high-traffic tenants
- Use connection pooling (max 100 connections per tenant DB)

### Caching Strategy
- Tenant configuration: 5 min TTL
- User lists: 2 min TTL (for dashboard views)
- Subscription status: 10 min TTL
- Policy settings: 5 min TTL

### Sharding (Future)
For tenants with > 10,000 external users:
- Partition ExternalUsers table by date range
- Use temporal tables for historical data
- Implement read replicas per region

## Backup & Recovery

### Automated Backups
- **SQL Database**: Point-in-time restore (7-35 days)
- **Cosmos DB**: Continuous backup (30 days)
- **Blob Storage**: Geo-redundant storage (GRS)

### Recovery Procedures
1. Identify affected tenant(s)
2. Restore tenant database to point-in-time
3. Validate data integrity
4. Notify tenant admin
5. Document incident in audit log

---

**Last Updated**: 2024-02-03
**Version**: 1.0

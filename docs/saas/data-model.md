# Multi-Tenant Data Model

## Overview

The SharePoint External User Manager uses a hybrid multi-tenant data architecture combining Azure SQL Database (for transactional data) and Azure Cosmos DB (for metadata and real-time data). The design prioritizes strong tenant isolation, performance, and scalability.

## Tenant Isolation Strategy

**Database-per-Tenant Model** (Azure SQL)

This approach provides the strongest isolation and makes compliance/data residency requirements easier to meet:

- **Pros:**
  - Complete data isolation per tenant
  - Easy to backup/restore individual tenants
  - Custom performance tuning per tenant
  - Simplified data residency compliance
  - Easy tenant offboarding (drop database)

- **Cons:**
  - Higher operational complexity
  - More databases to manage
  - Schema migrations require iteration across all databases

- **Mitigation:**
  - Use Elastic Pools to share resources
  - Automate migrations with scripts
  - Central master database for routing

## Master Database Schema (Azure SQL)

The master database contains global data and tenant registry:

```sql
-- Tenants table: Core tenant registry
CREATE TABLE Tenants (
    TenantId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TenantDomain NVARCHAR(255) NOT NULL UNIQUE,
    DisplayName NVARCHAR(255) NOT NULL,
    EntraIdTenantId NVARCHAR(255) NOT NULL UNIQUE,
    DatabaseName NVARCHAR(128) NOT NULL,
    SubscriptionTier NVARCHAR(50) NOT NULL, -- Free, Pro, Enterprise
    SubscriptionStatus NVARCHAR(50) NOT NULL, -- Active, Expired, Suspended, Cancelled
    SubscriptionStartDate DATETIME2 NOT NULL,
    SubscriptionEndDate DATETIME2 NULL,
    TrialEndDate DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(255) NOT NULL,
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(255) NULL,
    CONSTRAINT CK_SubscriptionTier CHECK (SubscriptionTier IN ('Free', 'Pro', 'Enterprise')),
    CONSTRAINT CK_SubscriptionStatus CHECK (SubscriptionStatus IN ('Active', 'Trial', 'Expired', 'Suspended', 'Cancelled'))
);

CREATE INDEX IX_Tenants_EntraIdTenantId ON Tenants(EntraIdTenantId);
CREATE INDEX IX_Tenants_SubscriptionStatus ON Tenants(SubscriptionStatus);
CREATE INDEX IX_Tenants_TenantDomain ON Tenants(TenantDomain);

-- Subscription plans and feature matrix
CREATE TABLE SubscriptionPlans (
    PlanId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanName NVARCHAR(50) NOT NULL UNIQUE, -- Free, Pro, Enterprise
    DisplayName NVARCHAR(255) NOT NULL,
    MaxUsers INT NOT NULL,
    MaxLibraries INT NOT NULL,
    PricePerMonth DECIMAL(10,2) NOT NULL,
    Features NVARCHAR(MAX) NOT NULL, -- JSON array of features
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Tenant administrators
CREATE TABLE TenantAdmins (
    AdminId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    UserPrincipalName NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL, -- TenantAdmin, LibraryOwner
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginDate DATETIME2 NULL,
    FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT CK_AdminRole CHECK (Role IN ('TenantAdmin', 'LibraryOwner', 'ReadOnly'))
);

CREATE INDEX IX_TenantAdmins_TenantId ON TenantAdmins(TenantId);
CREATE INDEX IX_TenantAdmins_UserPrincipalName ON TenantAdmins(UserPrincipalName);

-- Licensing and feature gates
CREATE TABLE LicenseUsage (
    UsageId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    MetricName NVARCHAR(100) NOT NULL, -- UserCount, LibraryCount, APICallsPerMonth
    CurrentValue INT NOT NULL,
    MaxValue INT NOT NULL,
    LastUpdated DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId)
);

CREATE INDEX IX_LicenseUsage_TenantId ON LicenseUsage(TenantId);

-- Global audit log (high-level system events)
CREATE TABLE GlobalAuditLog (
    AuditId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TenantId UNIQUEIDENTIFIER NULL,
    EventType NVARCHAR(100) NOT NULL, -- TenantRegistered, SubscriptionChanged, etc.
    EventData NVARCHAR(MAX) NULL, -- JSON
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UserPrincipalName NVARCHAR(255) NULL,
    IpAddress NVARCHAR(50) NULL,
    FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId)
);

CREATE INDEX IX_GlobalAuditLog_TenantId ON GlobalAuditLog(TenantId);
CREATE INDEX IX_GlobalAuditLog_Timestamp ON GlobalAuditLog(Timestamp);
CREATE INDEX IX_GlobalAuditLog_EventType ON GlobalAuditLog(EventType);
```

## Tenant Database Schema (Azure SQL)

Each tenant has its own database with identical schema:

```sql
-- External Libraries
CREATE TABLE Libraries (
    LibraryId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SharePointSiteId NVARCHAR(255) NOT NULL,
    SharePointLibraryId NVARCHAR(255) NOT NULL UNIQUE,
    LibraryName NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    SiteUrl NVARCHAR(500) NOT NULL,
    OwnerEmail NVARCHAR(255) NOT NULL,
    OwnerDisplayName NVARCHAR(255) NULL,
    ExternalSharingEnabled BIT NOT NULL DEFAULT 1,
    ExternalUserCount INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(255) NOT NULL,
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(255) NULL,
    LastSyncDate DATETIME2 NULL
);

CREATE INDEX IX_Libraries_SharePointLibraryId ON Libraries(SharePointLibraryId);
CREATE INDEX IX_Libraries_OwnerEmail ON Libraries(OwnerEmail);
CREATE INDEX IX_Libraries_IsActive ON Libraries(IsActive);

-- External Users
CREATE TABLE ExternalUsers (
    UserId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SharePointUserId NVARCHAR(255) NOT NULL UNIQUE,
    UserPrincipalName NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    DisplayName NVARCHAR(255) NULL,
    Company NVARCHAR(255) NULL,
    Project NVARCHAR(255) NULL,
    InvitedBy NVARCHAR(255) NOT NULL,
    InvitedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    AcceptedDate DATETIME2 NULL,
    LastAccessDate DATETIME2 NULL,
    Status NVARCHAR(50) NOT NULL, -- Invited, Active, Suspended, Removed
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT CK_UserStatus CHECK (Status IN ('Invited', 'Active', 'Suspended', 'Removed'))
);

CREATE INDEX IX_ExternalUsers_Email ON ExternalUsers(Email);
CREATE INDEX IX_ExternalUsers_Status ON ExternalUsers(Status);
CREATE INDEX IX_ExternalUsers_Company ON ExternalUsers(Company);
CREATE INDEX IX_ExternalUsers_Project ON ExternalUsers(Project);

-- User Permissions (many-to-many: Users to Libraries)
CREATE TABLE UserPermissions (
    PermissionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    LibraryId UNIQUEIDENTIFIER NOT NULL,
    PermissionLevel NVARCHAR(50) NOT NULL, -- Read, Contribute, Edit, FullControl
    GrantedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    GrantedBy NVARCHAR(255) NOT NULL,
    ExpirationDate DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES ExternalUsers(UserId),
    FOREIGN KEY (LibraryId) REFERENCES Libraries(LibraryId),
    CONSTRAINT CK_PermissionLevel CHECK (PermissionLevel IN ('Read', 'Contribute', 'Edit', 'FullControl')),
    CONSTRAINT UQ_UserLibraryPermission UNIQUE (UserId, LibraryId)
);

CREATE INDEX IX_UserPermissions_UserId ON UserPermissions(UserId);
CREATE INDEX IX_UserPermissions_LibraryId ON UserPermissions(LibraryId);
CREATE INDEX IX_UserPermissions_PermissionLevel ON UserPermissions(PermissionLevel);

-- Collaboration Policies
CREATE TABLE CollaborationPolicies (
    PolicyId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PolicyName NVARCHAR(255) NOT NULL,
    PolicyType NVARCHAR(100) NOT NULL, -- ExternalSharingPolicy, AccessReviewPolicy, etc.
    IsEnabled BIT NOT NULL DEFAULT 1,
    Configuration NVARCHAR(MAX) NOT NULL, -- JSON configuration
    AppliesTo NVARCHAR(50) NOT NULL, -- AllLibraries, SpecificLibraries
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(255) NOT NULL,
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy NVARCHAR(255) NULL,
    CONSTRAINT CK_PolicyAppliesTo CHECK (AppliesTo IN ('AllLibraries', 'SpecificLibraries'))
);

CREATE INDEX IX_CollaborationPolicies_PolicyType ON CollaborationPolicies(PolicyType);

-- Policy-to-Library mapping
CREATE TABLE PolicyLibraryMapping (
    MappingId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PolicyId UNIQUEIDENTIFIER NOT NULL,
    LibraryId UNIQUEIDENTIFIER NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PolicyId) REFERENCES CollaborationPolicies(PolicyId),
    FOREIGN KEY (LibraryId) REFERENCES Libraries(LibraryId),
    CONSTRAINT UQ_PolicyLibrary UNIQUE (PolicyId, LibraryId)
);

CREATE INDEX IX_PolicyLibraryMapping_PolicyId ON PolicyLibraryMapping(PolicyId);
CREATE INDEX IX_PolicyLibraryMapping_LibraryId ON PolicyLibraryMapping(LibraryId);

-- Tenant-level Audit Log (detailed operational events)
CREATE TABLE AuditLog (
    AuditId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EventType NVARCHAR(100) NOT NULL,
    EntityType NVARCHAR(100) NULL, -- Library, User, Permission, Policy
    EntityId UNIQUEIDENTIFIER NULL,
    Action NVARCHAR(50) NOT NULL, -- Create, Update, Delete, Grant, Revoke
    ActionBy NVARCHAR(255) NOT NULL,
    ActionDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    BeforeState NVARCHAR(MAX) NULL, -- JSON snapshot before change
    AfterState NVARCHAR(MAX) NULL, -- JSON snapshot after change
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    CorrelationId UNIQUEIDENTIFIER NULL
);

CREATE INDEX IX_AuditLog_EventType ON AuditLog(EventType);
CREATE INDEX IX_AuditLog_EntityType ON AuditLog(EntityType);
CREATE INDEX IX_AuditLog_ActionDate ON AuditLog(ActionDate);
CREATE INDEX IX_AuditLog_ActionBy ON AuditLog(ActionBy);
CREATE INDEX IX_AuditLog_CorrelationId ON AuditLog(CorrelationId);

-- Invitation History
CREATE TABLE InvitationHistory (
    InvitationId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    LibraryId UNIQUEIDENTIFIER NOT NULL,
    InvitationStatus NVARCHAR(50) NOT NULL, -- Sent, Accepted, Expired, Resent
    SentDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    AcceptedDate DATETIME2 NULL,
    ExpirationDate DATETIME2 NULL,
    InvitationMessage NVARCHAR(MAX) NULL,
    FOREIGN KEY (UserId) REFERENCES ExternalUsers(UserId),
    FOREIGN KEY (LibraryId) REFERENCES Libraries(LibraryId),
    CONSTRAINT CK_InvitationStatus CHECK (InvitationStatus IN ('Sent', 'Accepted', 'Expired', 'Resent', 'Cancelled'))
);

CREATE INDEX IX_InvitationHistory_UserId ON InvitationHistory(UserId);
CREATE INDEX IX_InvitationHistory_LibraryId ON InvitationHistory(LibraryId);
CREATE INDEX IX_InvitationHistory_InvitationStatus ON InvitationHistory(InvitationStatus);
```

## Cosmos DB Data Model (NoSQL)

Cosmos DB containers for shared metadata and real-time data:

### Container: TenantMetadata

Partition Key: `/tenantId`

```json
{
  "id": "tenant-abc-metadata",
  "tenantId": "abc-123-def",
  "type": "metadata",
  "settings": {
    "externalSharingEnabled": true,
    "allowAnonymousLinks": false,
    "defaultLinkPermission": "View",
    "externalUserExpirationDays": 90,
    "requireApprovalForExternalSharing": true,
    "accessReviewIntervalDays": 180
  },
  "features": {
    "bulkOperations": true,
    "advancedAudit": true,
    "customPolicies": true,
    "apiAccess": true
  },
  "customization": {
    "branding": {
      "logo": "https://storage.../logo.png",
      "primaryColor": "#0078d4",
      "displayName": "Contoso External Sharing"
    }
  },
  "_ts": 1705320000
}
```

### Container: AuditEvents

Partition Key: `/tenantId`

```json
{
  "id": "evt-789-xyz",
  "tenantId": "abc-123-def",
  "type": "auditEvent",
  "eventType": "UserInvited",
  "timestamp": "2024-01-15T10:30:00Z",
  "actor": {
    "userPrincipalName": "admin@contoso.com",
    "displayName": "John Admin",
    "ipAddress": "203.0.113.42"
  },
  "target": {
    "entityType": "ExternalUser",
    "entityId": "user-456",
    "email": "partner@external.com"
  },
  "details": {
    "libraryName": "Partner Documents",
    "permissionLevel": "Contribute",
    "expirationDate": "2024-04-15T10:30:00Z"
  },
  "correlationId": "req-abc-123",
  "_ts": 1705320000,
  "ttl": 7776000
}
```

### Container: UsageMetrics

Partition Key: `/tenantId`

```json
{
  "id": "metrics-2024-01-15",
  "tenantId": "abc-123-def",
  "type": "dailyMetrics",
  "date": "2024-01-15",
  "metrics": {
    "apiCalls": {
      "total": 1543,
      "byEndpoint": {
        "/api/v1/libraries": 450,
        "/api/v1/users": 780,
        "/api/v1/policies": 313
      }
    },
    "activeUsers": 12,
    "externalUsersCount": 45,
    "librariesCount": 8,
    "permissionsGranted": 23,
    "permissionsRevoked": 5
  },
  "performance": {
    "avgResponseTimeMs": 185,
    "p95ResponseTimeMs": 420,
    "errorRate": 0.012
  },
  "_ts": 1705320000,
  "ttl": 7776000
}
```

### Container: SessionCache

Partition Key: `/sessionId`

```json
{
  "id": "session-xyz-789",
  "sessionId": "session-xyz-789",
  "tenantId": "abc-123-def",
  "userPrincipalName": "admin@contoso.com",
  "authToken": "eyJ...",
  "roles": ["TenantAdmin"],
  "permissions": ["libraries:read", "users:write"],
  "createdAt": "2024-01-15T10:00:00Z",
  "expiresAt": "2024-01-15T18:00:00Z",
  "_ts": 1705320000,
  "ttl": 28800
}
```

## Data Retention Policies

### Audit Logs
- **Tenant-level audit logs (SQL):** 
  - Retention: 7 years (compliance requirement)
  - After 1 year: Move to read-only archive table
  - After 7 years: Export to blob storage and purge from database

- **Real-time audit events (Cosmos DB):**
  - Retention: 90 days using TTL
  - Hot data for recent queries and analytics
  - Automatically purged after 90 days

### Operational Data
- **External users:** Retain for 2 years after removal
- **Libraries:** Retain for 1 year after deletion
- **Invitation history:** Retain for 1 year

### Analytics and Metrics
- **Daily metrics:** 1 year retention
- **Monthly aggregates:** 5 years retention
- **Annual summaries:** Permanent retention

## Data Migration Strategy

### Initial Tenant Provisioning
```sql
-- Create new tenant database from template
CREATE DATABASE [tenant_{tenantId}] 
AS COPY OF [TemplateDatabase];

-- Run tenant-specific initialization
USE [tenant_{tenantId}];
EXEC sp_InitializeTenant @TenantId = '{tenantId}';
```

### Schema Migrations
```powershell
# Apply schema changes to all tenant databases
$tenants = Get-AzSqlDatabase -ResourceGroupName "rg-spexternal" | 
            Where-Object { $_.DatabaseName -like "tenant_*" }

foreach ($tenant in $tenants) {
    Invoke-Sqlcmd -ServerInstance $server -Database $tenant.DatabaseName `
                  -InputFile "migration-v1.2.sql"
}
```

## Performance Optimization

### Indexing Strategy
- **Primary Keys:** Clustered indexes on all GUID primary keys
- **Foreign Keys:** Non-clustered indexes for joins
- **Query Patterns:** Composite indexes for common WHERE clauses
- **Full-Text Search:** Indexes on description and name fields

### Partitioning (Cosmos DB)
- Partition by `tenantId` for tenant isolation
- Enables parallel queries within tenant
- Supports unlimited scale per tenant

### Caching
- Tenant metadata cached in Cosmos DB (hot path)
- Session state cached with TTL
- SQL query results cached at application level

## Backup and Disaster Recovery

### Azure SQL
- **Automated Backups:** Point-in-time restore (35 days)
- **Geo-Replication:** Active geo-replication to secondary region
- **Long-term Retention:** Annual backups for 10 years

### Cosmos DB
- **Continuous Backup:** 30 days retention
- **Geo-Redundancy:** Multi-region writes enabled
- **Point-in-Time Restore:** Available for all containers

## Compliance and Security

### Data Encryption
- **At Rest:** Transparent Data Encryption (TDE) for SQL
- **In Transit:** TLS 1.2+ for all connections
- **Key Management:** Azure Key Vault for encryption keys

### Data Residency
- Database-per-tenant enables region-specific deployment
- Support for EU Data Boundary requirements
- Configurable data location per tenant

### GDPR Compliance
- Right to erasure: Soft delete with hard delete after retention
- Data portability: Export APIs for tenant data
- Audit trail: Complete history of data access and modifications

## References

- [Multi-Tenant Data Architecture Patterns](https://learn.microsoft.com/azure/architecture/guide/multitenant/approaches/data-storage)
- [Azure SQL Database Elastic Pools](https://learn.microsoft.com/azure/azure-sql/database/elastic-pool-overview)
- [Cosmos DB Partitioning Best Practices](https://learn.microsoft.com/azure/cosmos-db/partitioning-overview)

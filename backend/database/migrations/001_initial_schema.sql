-- ============================================================================
-- SharePoint External User Manager - Initial Database Schema
-- Version: 001
-- Description: Creates all tables for multi-tenant SaaS backend
-- ============================================================================

-- Drop tables if exist (for clean migration)
IF OBJECT_ID('dbo.UserAction', 'U') IS NOT NULL DROP TABLE dbo.UserAction;
IF OBJECT_ID('dbo.AuditLog', 'U') IS NOT NULL DROP TABLE dbo.AuditLog;
IF OBJECT_ID('dbo.Policy', 'U') IS NOT NULL DROP TABLE dbo.Policy;
IF OBJECT_ID('dbo.Subscription', 'U') IS NOT NULL DROP TABLE dbo.Subscription;
IF OBJECT_ID('dbo.Tenant', 'U') IS NOT NULL DROP TABLE dbo.Tenant;
GO

-- ============================================================================
-- Table: Tenant
-- Description: Stores tenant registration and configuration
-- ============================================================================
CREATE TABLE [dbo].[Tenant] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [EntraIdTenantId] NVARCHAR(100) NOT NULL,
    [OrganizationName] NVARCHAR(255) NOT NULL,
    [PrimaryAdminEmail] NVARCHAR(255) NOT NULL,
    [OnboardedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active',
    [Settings] NVARCHAR(MAX) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Tenant] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Tenant_EntraIdTenantId] UNIQUE NONCLUSTERED ([EntraIdTenantId] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_Tenant_EntraIdTenantId] 
ON [dbo].[Tenant] ([EntraIdTenantId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Tenant_Status] 
ON [dbo].[Tenant] ([Status] ASC);
GO

-- ============================================================================
-- Table: Subscription
-- Description: Tracks subscription tier and licensing status per tenant
-- ============================================================================
CREATE TABLE [dbo].[Subscription] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [Tier] NVARCHAR(50) NOT NULL,
    [StartDate] DATETIME2 NOT NULL,
    [EndDate] DATETIME2 NULL,
    [TrialExpiry] DATETIME2 NULL,
    [GracePeriodEnd] DATETIME2 NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Trial',
    [MaxUsers] INT NULL,
    [Features] NVARCHAR(MAX) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Subscription] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Subscription_Tenant] FOREIGN KEY ([TenantId]) 
        REFERENCES [dbo].[Tenant] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_Subscription_TenantId] 
ON [dbo].[Subscription] ([TenantId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Subscription_Status] 
ON [dbo].[Subscription] ([Status] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Subscription_TrialExpiry] 
ON [dbo].[Subscription] ([TrialExpiry] ASC)
WHERE [TrialExpiry] IS NOT NULL;
GO

-- ============================================================================
-- Table: Policy
-- Description: Stores collaboration policies per tenant
-- ============================================================================
CREATE TABLE [dbo].[Policy] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [PolicyType] NVARCHAR(100) NOT NULL,
    [Enabled] BIT NOT NULL DEFAULT 1,
    [Configuration] NVARCHAR(MAX) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Policy] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Policy_Tenant] FOREIGN KEY ([TenantId]) 
        REFERENCES [dbo].[Tenant] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_Policy_TenantId] 
ON [dbo].[Policy] ([TenantId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Policy_TenantId_PolicyType] 
ON [dbo].[Policy] ([TenantId] ASC, [PolicyType] ASC);
GO

-- ============================================================================
-- Table: AuditLog
-- Description: Immutable audit trail for all system operations
-- ============================================================================
CREATE TABLE [dbo].[AuditLog] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UserId] NVARCHAR(100) NULL,
    [UserEmail] NVARCHAR(255) NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [ResourceType] NVARCHAR(50) NULL,
    [ResourceId] NVARCHAR(255) NULL,
    [Details] NVARCHAR(MAX) NULL,
    [IpAddress] NVARCHAR(50) NULL,
    [CorrelationId] NVARCHAR(100) NULL,
    [Status] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AuditLog_Tenant] FOREIGN KEY ([TenantId]) 
        REFERENCES [dbo].[Tenant] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLog_TenantId_Timestamp] 
ON [dbo].[AuditLog] ([TenantId] ASC, [Timestamp] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLog_CorrelationId] 
ON [dbo].[AuditLog] ([CorrelationId] ASC)
WHERE [CorrelationId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_AuditLog_Action] 
ON [dbo].[AuditLog] ([Action] ASC);
GO

-- ============================================================================
-- Table: UserAction
-- Description: Tracks external user management actions
-- ============================================================================
CREATE TABLE [dbo].[UserAction] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [ExternalUserEmail] NVARCHAR(255) NOT NULL,
    [ActionType] NVARCHAR(50) NOT NULL,
    [TargetLibrary] NVARCHAR(500) NULL,
    [PerformedBy] NVARCHAR(255) NOT NULL,
    [PerformedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Metadata] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_UserAction] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserAction_Tenant] FOREIGN KEY ([TenantId]) 
        REFERENCES [dbo].[Tenant] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_UserAction_TenantId_Email] 
ON [dbo].[UserAction] ([TenantId] ASC, [ExternalUserEmail] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_UserAction_TenantId_Date] 
ON [dbo].[UserAction] ([TenantId] ASC, [PerformedDate] DESC);
GO

PRINT 'Database schema created successfully!';
PRINT 'Tables created:';
PRINT '  - Tenant';
PRINT '  - Subscription';
PRINT '  - Policy';
PRINT '  - AuditLog';
PRINT '  - UserAction';
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Verify all tables and indexes';
PRINT '  2. Grant appropriate permissions to app service principal';
PRINT '  3. Test tenant onboarding flow';
GO

-- ============================================================================
-- SharePoint External User Manager - Tenant OAuth Token Storage
-- Version: 004
-- Description: Add table for storing tenant OAuth tokens for Microsoft Graph
-- ============================================================================

-- Create TenantAuth table for storing OAuth tokens
IF OBJECT_ID('dbo.TenantAuth', 'U') IS NOT NULL DROP TABLE dbo.TenantAuth;
GO

CREATE TABLE [dbo].[TenantAuth] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [AccessToken] NVARCHAR(MAX) NULL,
    [RefreshToken] NVARCHAR(MAX) NULL,
    [TokenExpiresAt] DATETIME2 NULL,
    [Scope] NVARCHAR(MAX) NULL,
    [ConsentGrantedBy] NVARCHAR(255) NULL,
    [ConsentGrantedAt] DATETIME2 NULL,
    [LastTokenRefresh] DATETIME2 NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_TenantAuth] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TenantAuth_Tenant] FOREIGN KEY ([TenantId]) 
        REFERENCES [dbo].[Tenant] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_TenantAuth_TenantId] UNIQUE NONCLUSTERED ([TenantId] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_TenantAuth_TenantId] 
ON [dbo].[TenantAuth] ([TenantId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_TenantAuth_TokenExpiry] 
ON [dbo].[TenantAuth] ([TokenExpiresAt] ASC)
WHERE [TokenExpiresAt] IS NOT NULL;
GO

PRINT 'Migration 004 completed: TenantAuth table created successfully';
GO

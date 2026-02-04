-- ============================================================================
-- SharePoint External User Manager - Client Table Migration
-- Version: 002
-- Description: Creates Client table for SaaS Core
-- ============================================================================

-- Drop table if exists (for clean migration)
IF OBJECT_ID('dbo.Client', 'U') IS NOT NULL DROP TABLE dbo.Client;
GO

-- ============================================================================
-- Table: Client
-- Description: Stores client records representing solicitor's customers
--              Each client is mapped 1:1 to a SharePoint site (Client Space)
-- ============================================================================
CREATE TABLE [dbo].[Client] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TenantId] INT NOT NULL,
    [ClientName] NVARCHAR(255) NOT NULL,
    [SiteUrl] NVARCHAR(500) NOT NULL,
    [SiteId] NVARCHAR(100) NOT NULL,
    [CreatedBy] NVARCHAR(255) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Provisioning',
    CONSTRAINT [PK_Client] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Client_Tenant] FOREIGN KEY ([TenantId]) 
        REFERENCES [dbo].[Tenant] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [CHK_Client_Status] CHECK ([Status] IN ('Provisioning', 'Active', 'Error'))
);
GO

-- Create indexes for performance
CREATE NONCLUSTERED INDEX [IX_Client_TenantId] 
ON [dbo].[Client] ([TenantId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Client_TenantId_CreatedAt] 
ON [dbo].[Client] ([TenantId] ASC, [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_Client_SiteId] 
ON [dbo].[Client] ([SiteId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Client_Status] 
ON [dbo].[Client] ([Status] ASC);
GO

PRINT 'Client table created successfully!';
PRINT '';
PRINT 'Table structure:';
PRINT '  - Id (PK)';
PRINT '  - TenantId (FK to Tenant)';
PRINT '  - ClientName';
PRINT '  - SiteUrl';
PRINT '  - SiteId';
PRINT '  - CreatedBy';
PRINT '  - CreatedAt';
PRINT '  - Status (Provisioning | Active | Error)';
PRINT '';
PRINT 'Indexes:';
PRINT '  - IX_Client_TenantId';
PRINT '  - IX_Client_TenantId_CreatedAt (for ordered listing)';
PRINT '  - IX_Client_SiteId';
PRINT '  - IX_Client_Status';
GO

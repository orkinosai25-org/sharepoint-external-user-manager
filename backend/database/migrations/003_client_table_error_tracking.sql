-- ============================================================================
-- SharePoint External User Manager - Client Table Update
-- Version: 003
-- Description: Adds ErrorMessage column and updates SiteUrl/SiteId to be nullable
-- ============================================================================

-- Add ErrorMessage column for tracking provisioning errors
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Client') AND name = 'ErrorMessage')
BEGIN
    ALTER TABLE [dbo].[Client]
    ADD [ErrorMessage] NVARCHAR(1000) NULL;
    
    PRINT 'Added ErrorMessage column to Client table';
END
GO

-- Make SiteUrl nullable since it's populated after provisioning
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Client') 
    AND name = 'SiteUrl' 
    AND is_nullable = 0
)
BEGIN
    ALTER TABLE [dbo].[Client]
    ALTER COLUMN [SiteUrl] NVARCHAR(500) NULL;
    
    PRINT 'Updated SiteUrl to be nullable';
END
GO

-- Make SiteId nullable since it's populated after provisioning
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Client') 
    AND name = 'SiteId' 
    AND is_nullable = 0
)
BEGIN
    ALTER TABLE [dbo].[Client]
    ALTER COLUMN [SiteId] NVARCHAR(100) NULL;
    
    PRINT 'Updated SiteId to be nullable';
END
GO

PRINT '';
PRINT 'Client table updated successfully!';
PRINT 'New columns:';
PRINT '  - ErrorMessage (nullable) - Stores provisioning error messages';
PRINT 'Updated columns:';
PRINT '  - SiteUrl (now nullable) - Populated after site provisioning';
PRINT '  - SiteId (now nullable) - Populated after site provisioning';
GO

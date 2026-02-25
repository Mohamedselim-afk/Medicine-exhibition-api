-- =============================================
-- Add Category table and Product columns (ExpiryDate, CategoryId)
-- Run this on existing database; adjust database name if needed
-- =============================================

-- USE YourDatabaseName;
-- GO

-- Create Categories table if not exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Categories] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsActive] BIT NOT NULL DEFAULT 1
    );
    CREATE INDEX [IX_Categories_Name] ON [dbo].[Categories]([Name]);
    CREATE INDEX [IX_Categories_IsActive] ON [dbo].[Categories]([IsActive]);
    PRINT 'Table Categories created.';
END
GO

-- Add ExpiryDate to Products if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'ExpiryDate')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [ExpiryDate] DATETIME2 NULL;
    PRINT 'Column Products.ExpiryDate added.';
END
GO

-- Add CategoryId to Products if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'CategoryId')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [CategoryId] INT NULL;
    ALTER TABLE [dbo].[Products] ADD CONSTRAINT [FK_Products_Categories]
        FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE SET NULL;
    CREATE INDEX [IX_Products_CategoryId] ON [dbo].[Products]([CategoryId]);
    PRINT 'Column Products.CategoryId and FK added.';
END
GO

PRINT 'Schema update completed.';
